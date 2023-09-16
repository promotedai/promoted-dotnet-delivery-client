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

        private async Task<string> CallDelivery(Promoted.Delivery.Request req)
        {
            // Protobuf -> JSON.
            string json = _formatter.Format(req);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await _deliveryHttpClient.PostAsync(_deliveryEndpoint, content);
            try
            {
                // This throws if the request was unsuccessful.
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (TaskCanceledException ex)
            {
                // TODO(james): Proper timeout behavior.
                _logger.Error($"Delivery request timed out: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // TODO(james): Proper fallback behavior.
                _logger.Error($"Delivery request failed: {ex.Message}");
                return null;
            }
        }

        private void CallMetrics()
        {
            // TODO(james): Fix the event C# namespace in schema.
            var log_req = new Event.LogRequest();
            string json = _formatter.Format(log_req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _metricsHttpClient.PostAsync(_metricsEndpoint, content, task =>
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
                        _logger.Error($"Metrics request failed: {ex.Message}");
                    }
                }
                // Timeouts are indicated here.
                else if (task.IsFaulted || task.IsCanceled)
                {
                    if (task.Exception == null)
                    {
                        _logger.Error("Metrics request could not be completed (likely timed out)");
                    }
                    else
                    {
                        _logger.Error($"Metrics request could not be completed: {task.Exception}");
                    }
                }
                content.Dispose();
            });
        }

        public async Task<Promoted.Delivery.Response> Deliver(Promoted.Delivery.Request req)
        {
            // TODO(james): Add optional request validation to help with integration.

            RequestProcessor.FillNecessaryFields(req);

            // TODO(james): Implement SDK delivery.

            // TODO(james): Add logic to determine delivery method.
            string resp = await CallDelivery(req);

            // TODO(james): Actually implement CallMetrics().
            // TODO(james): Only call metrics when SDK delivery was done.
            CallMetrics();

            bool shouldSendShadowTraffic = _options.ShadowTrafficRate > 0 &&
                                           _options.ShadowTrafficRate < ThreadSafeRandom.NextFloat();
            // TODO(james): Implement shadow traffic.
            // TODO(james): Only send shadow traffic when we earlier decided to.

            // JSON -> Protobuf.
            return (Promoted.Delivery.Response)_parser.Parse(resp, Promoted.Delivery.Response.Descriptor);
        }
    }
}
