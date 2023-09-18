// In addition to implementing ICallbackHttpClient, this will:
// 1. Take ownership of callback continuations
// 2. Ensure that they do finish

using System.Collections.Concurrent;

namespace Promoted.Lib
{
    // Disposable to ensure all pending tasks finish.
    public class OwningCallbackHttpClient : ICallbackHttpClient, IDisposable
    {
        private readonly ConcurrentBag<Task> _pendingTasks = new ConcurrentBag<Task>();
        private readonly HttpClient _httpClient = new HttpClient();

        public OwningCallbackHttpClient(string apiKey, int timeoutMillis)
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.Timeout = TimeSpan.FromMilliseconds(timeoutMillis);
        }

        public void Dispose()
        {
            Task.WhenAll(_pendingTasks).Wait();
        }

        public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content)
        {
            return _httpClient.PostAsync(requestUri, content);
        }

        public void PostAsync(string requestUri, HttpContent content, Action<Task<HttpResponseMessage>> callback)
        {
            Task continuation = _httpClient.PostAsync(requestUri, content).ContinueWith(task =>
            {
                callback?.Invoke(task);
                // Remove the completed task from our list of pending tasks.
                _pendingTasks.TryTake(out _);
            });
            // Store these to ensure they get completed.
            _pendingTasks.Add(continuation);
        }
    }
}
