using Google.Protobuf;
using NLog;
using System.Text;

namespace Promoted.Lib
{
    // Disposable because of disposable members.
    public class DeliveryClient : IDisposable
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly JsonFormatter _formatter = new JsonFormatter(JsonFormatter.Settings.Default);
        private static readonly JsonParser _parser = new JsonParser(JsonParser.Settings.Default);
        private static readonly string _shadowDeliveryLogTag = "Shadow Delivery";
        private static readonly string _metricsLogTag = "Metrics";
        private readonly ICallbackHttpClient _deliveryHttpClient;
        private readonly ICallbackHttpClient _metricsHttpClient;
        private readonly string _deliveryEndpoint;
        private readonly string _metricsEndpoint;
        private readonly DeliveryClientOptions _options = new DeliveryClientOptions();

        public DeliveryClient(string deliveryEndpoint, string deliveryApiKey, int deliveryTimeoutMillis,
                              string metricsEndpoint, string metricsApiKey, int metricsTimeoutMillis,
                              DeliveryClientOptions? options = null)
        {
            _deliveryHttpClient = new OwningCallbackHttpClient(deliveryApiKey, deliveryTimeoutMillis);
            this._deliveryEndpoint = deliveryEndpoint;
            _metricsHttpClient = new OwningCallbackHttpClient(metricsApiKey, metricsTimeoutMillis);
            this._metricsEndpoint = metricsEndpoint;

            if (options != null)
            {
                _options = options;
            }

            _logger.Info("Constructed delivery client:");
            _logger.Info($"- Delivery endpoint = {deliveryEndpoint}");
            _logger.Info($"- Metrics endpoint = {metricsEndpoint}");
        }

        // Intended for test. Prefer the above constructor for the default HttpClient impl.
        public DeliveryClient(ICallbackHttpClient deliveryHttpClient, string deliveryEndpoint,
                              ICallbackHttpClient metricsHttpClient, string metricsEndpoint,
                              DeliveryClientOptions? options = null)
        {
            _deliveryHttpClient = deliveryHttpClient;
            this._deliveryEndpoint = deliveryEndpoint;
            _metricsHttpClient = metricsHttpClient;
            this._metricsEndpoint = metricsEndpoint;

            if (options != null)
            {
                _options = options;
            }
        }

        public void Dispose()
        {
            _logger.Info("Waiting for pending tasks to complete...");
            if (_deliveryHttpClient is OwningCallbackHttpClient concreteDeliveryClient)
            {
                concreteDeliveryClient.Dispose();
            }
            if (_metricsHttpClient is OwningCallbackHttpClient concreteMetricsClient)
            {
                concreteMetricsClient.Dispose();
            }
        }

        private async Task<Promoted.Delivery.Response?> CallDelivery(Promoted.Delivery.Request req)
        {
            // Protobuf -> JSON.
            string json = _formatter.Format(req);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                using HttpResponseMessage response = await _deliveryHttpClient.PostAsync(_deliveryEndpoint, content);
                // This throws if the request was unsuccessful.
                response.EnsureSuccessStatusCode();
                // JSON -> Protobuf.
                return (Promoted.Delivery.Response)_parser.Parse(await response.Content.ReadAsStringAsync(),
                                                                 Promoted.Delivery.Response.Descriptor);
            }
            catch (TaskCanceledException ex)
            {
                _logger.Error($"Delivery request timed out: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error($"Delivery request failed: {ex.Message}");
                return null;
            }
        }

        private void HandlePostAsyncCompletion(Task<HttpResponseMessage> task, HttpContent content, string logTag)
        {
            if (task.Status == TaskStatus.RanToCompletion)
            {
                using HttpResponseMessage response = task.Result;
                try
                {
                    response.EnsureSuccessStatusCode();
                }
                catch (Exception ex)
                {
                    _logger.Error($"{logTag} request failed: {ex.Message}");
                }
            }
            // Timeouts are indicated here.
            else if (task.IsFaulted || task.IsCanceled)
            {
                if (task.Exception == null)
                {
                    _logger.Error($"{logTag} request could not be completed (likely timed out)");
                }
                else
                {
                    _logger.Error($"{logTag} request could not be completed: {task.Exception}");
                }
            }
            content.Dispose();
        }

        // Shadow traffic is always non-blocking, at least for now.
        private void ShadowDelivery(Promoted.Delivery.Request req)
        {
            string json = _formatter.Format(req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _deliveryHttpClient.PostAsync(_deliveryEndpoint, content,
                                          task => HandlePostAsyncCompletion(task, content, _shadowDeliveryLogTag));
        }

        private void LogToMetrics(Promoted.Event.LogRequest logReq)
        {
            string json = _formatter.Format(logReq);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _metricsHttpClient.PostAsync(_metricsEndpoint, content,
                                         task => HandlePostAsyncCompletion(task, content, _metricsLogTag));
        }

        public async Task<Promoted.Delivery.Response> Deliver(Promoted.Delivery.Request req,
                                                              DeliveryRequestOptions? options = null)
        {
            options ??= new DeliveryRequestOptions();

            if (_options.Validate)
            {
                List<string> problems = RequestProcessor.Validate(req);
                foreach (string problem in problems)
                {
                    _logger.Error($"Delivery request validation error: {problem}");
                }
            }
            RequestProcessor.FillNecessaryFields(req);

            Promoted.Delivery.Response? resp = null;

            bool attemptedCallDelivery = false;
            bool didSdkDelivery = true;
            // Don't call delivery service if this request is part of the control group in an experiment
            // or if we just want to log to metrics.
            if (RequestProcessor.IsInControl(options.Experiment) || options.OnlyLogToMetrics)
            {
                resp = SdkDelivery.Deliver(req, options);
            }
            else
            {
                attemptedCallDelivery = true;
                resp = await CallDelivery(req);
                if (resp == null)
                {
                    resp = SdkDelivery.Deliver(req, options);
                }
                else
                {
                    didSdkDelivery = false;
                }
            }

            // Log to metrics if SDK delivery was done or if we have experiment info the delivery service doesn't.
            if (didSdkDelivery || options.Experiment != null)
            {
                Promoted.Event.LogRequest logReq =
                    Metrics.MakeLogRequest(req, resp, didSdkDelivery, options.Experiment);
                LogToMetrics(logReq);
            }

            // Even if we don't use the delivery service for re-ranking, we may still want to send traffic there.
            bool shouldShadowThisRequest = _options.ShadowTrafficRate > 0 &&
                                           ThreadSafeRandom.NextFloat() < _options.ShadowTrafficRate;
            if (!attemptedCallDelivery && shouldShadowThisRequest)
            {
                // Copy before modifying since the metrics call could still be using the request.
                Promoted.Delivery.Request clone = req.Clone();
                RequestProcessor.ConvertToShadowRequest(clone);
                ShadowDelivery(clone);
            }

            return resp;
        }
    }
}
