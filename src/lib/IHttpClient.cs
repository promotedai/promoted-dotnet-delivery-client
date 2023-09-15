// This is just for dependency injection into DeliveryClient.

namespace Promoted.Lib
{
    public interface IHttpClient
    {
        public Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content);
    }
}
