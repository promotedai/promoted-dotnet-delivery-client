using Google.Protobuf;
using System.Collections.Concurrent;
using System.Text;

namespace Promoted.Lib
{
    // Disposable to try and ensure calls to metrics finish.
    public class DeliveryClient : IDisposable
    {
        private static readonly HttpClient deliveryHttpClient = new HttpClient();
        private static readonly HttpClient metricsHttpClient = new HttpClient();

        private static readonly JsonFormatter formatter = new JsonFormatter(JsonFormatter.Settings.Default);
        private static readonly JsonParser parser = new JsonParser(JsonParser.Settings.Default);
        private readonly ConcurrentBag<Task> pendingTasks = new ConcurrentBag<Task>();
        private readonly string deliveryEndpoint;
        private readonly string metricsEndpoint;

        public DeliveryClient(string deliveryEndpoint, string deliveryApiKey, int deliveryTimeoutMillis,
                              string metricsEndpoint, string metricsApiKey, int metricsTimeoutMillis)
        {
            this.deliveryEndpoint = deliveryEndpoint;
            deliveryHttpClient.DefaultRequestHeaders.Add("x-api-key", deliveryApiKey);
            deliveryHttpClient.Timeout = TimeSpan.FromMilliseconds(deliveryTimeoutMillis);
            this.metricsEndpoint = metricsEndpoint;
            metricsHttpClient.DefaultRequestHeaders.Add("x-api-key", metricsApiKey);
            metricsHttpClient.Timeout = TimeSpan.FromMilliseconds(metricsTimeoutMillis);

            Console.WriteLine("Constructed delivery client:");
            Console.WriteLine($"- Delivery endpoint = {deliveryEndpoint}");
            Console.WriteLine($"- Metrics endpoint = {metricsEndpoint}");
        }

        public void Dispose()
        {
            Console.WriteLine("Waiting for pending tasks to complete...");
            Task.WhenAll(pendingTasks).Wait();
        }

        private async Task<string> CallDelivery(Promoted.Delivery.Request req)
        {
            // Protobuf -> JSON.
            string json = formatter.Format(req);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await deliveryHttpClient.PostAsync(deliveryEndpoint, content);
            try
            {
                // This throws if the request was unsuccessful.
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (TaskCanceledException ex)
            {
                // TODO(james): Proper timeout behavior.
                Console.WriteLine($"Delivery request timed out: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // TODO(james): Proper fallback behavior.
                Console.WriteLine($"Delivery request failed: {ex.Message}");
                return null;
            }
        }

        private void CallMetrics()
        {
            // TODO(james): Fix the event C# namespace in schema.
            var log_req = new Event.LogRequest();
            string json = formatter.Format(log_req);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            Task continuation = metricsHttpClient.PostAsync(metricsEndpoint, content).ContinueWith(task =>
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
                        Console.WriteLine($"Metrics request failed: {ex.Message}");
                    }
                }
                // Timeouts are indicated here.
                else if (task.IsFaulted || task.IsCanceled)
                {
                    Console.WriteLine($"Metrics request could not be completed: {task.Exception}");
                }
                // Remove the completed task from our list of pending tasks.
                pendingTasks.TryTake(out _);

                content.Dispose();
            });
            // Store these to try and ensure they get completed.
            pendingTasks.Add(continuation);
        }

        public async Task<Promoted.Delivery.Response> Deliver(Promoted.Delivery.Request req)
        {
            string resp = await CallDelivery(req);
            CallMetrics();
            // JSON -> Protobuf.
            return (Promoted.Delivery.Response)parser.Parse(resp, Promoted.Delivery.Response.Descriptor);
        }
    }
}
