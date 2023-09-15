using Google.Protobuf;
using NLog;
using System.Collections.Concurrent;
using System.Text;

namespace Promoted.Lib
{
    // Disposable to try and ensure calls to metrics finish.
    public class DeliveryClient : IDisposable
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly JsonFormatter _formatter = new JsonFormatter(JsonFormatter.Settings.Default);
        private static readonly JsonParser _parser = new JsonParser(JsonParser.Settings.Default);
        private readonly IHttpClient _deliveryHttpClient;
        private readonly IHttpClient _metricsHttpClient;
        private readonly string _deliveryEndpoint;
        private readonly string _metricsEndpoint;
        private readonly ConcurrentBag<Task> _pendingTasks = new ConcurrentBag<Task>();
        private readonly DeliveryClientOptions _options = new DeliveryClientOptions();

        private class HttpClientWrapper : IHttpClient
        {
            private readonly HttpClient _httpClient;

            public HttpClientWrapper(HttpClient httpClient)
            {
                _httpClient = httpClient;
            }

            public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
            {
                return _httpClient.PostAsync(requestUri, content);
            }
        }

        public DeliveryClient(string deliveryEndpoint, string deliveryApiKey, int deliveryTimeoutMillis,
                              string metricsEndpoint, string metricsApiKey, int metricsTimeoutMillis,
                              DeliveryClientOptions? options = null)
        {
            HttpClient deliveryHttpClient = new HttpClient();
            deliveryHttpClient.DefaultRequestHeaders.Add("x-api-key", deliveryApiKey);
            deliveryHttpClient.Timeout = TimeSpan.FromMilliseconds(deliveryTimeoutMillis);
            _deliveryHttpClient = new HttpClientWrapper(deliveryHttpClient);
            this._deliveryEndpoint = deliveryEndpoint;

            HttpClient metricsHttpClient = new HttpClient();
            metricsHttpClient.DefaultRequestHeaders.Add("x-api-key", metricsApiKey);
            metricsHttpClient.Timeout = TimeSpan.FromMilliseconds(metricsTimeoutMillis);
            _metricsHttpClient = new HttpClientWrapper(metricsHttpClient);
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
        public DeliveryClient(IHttpClient deliveryHttpClient, string deliveryEndpoint,
                              IHttpClient metricsHttpClient, string metricsEndpoint,
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
            Task.WhenAll(_pendingTasks).Wait();
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
            Task continuation = _metricsHttpClient.PostAsync(_metricsEndpoint, content).ContinueWith(task =>
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
                // Remove the completed task from our list of pending tasks.
                _pendingTasks.TryTake(out _);

                content.Dispose();
            });
            // Store these to try and ensure they get completed.
            _pendingTasks.Add(continuation);
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
