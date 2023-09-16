// Interface for an HTTP client that can run a given callback after an async call finishes.
// This is an interface for dependency injection into DeliveryClient.

namespace Promoted.Lib
{
    public interface ICallbackHttpClient
    {
        public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content);
        public void PostAsync(string requestUri, HttpContent content, Action<Task<HttpResponseMessage>> callback);
    }
}
