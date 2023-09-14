using Google.Protobuf;
using System.Text;

namespace Promoted.Lib
{
    public class DeliveryClient
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly JsonFormatter formatter = new JsonFormatter(JsonFormatter.Settings.Default);
        private static readonly JsonParser parser = new JsonParser(JsonParser.Settings.Default);
        private readonly string deliveryEndpoint;

        public DeliveryClient(string deliveryEndpoint, string deliveryApiKey, int deliveryTimeoutMillis)
        {
            this.deliveryEndpoint = deliveryEndpoint;
            httpClient.DefaultRequestHeaders.Add("x-api-key", deliveryApiKey);
            httpClient.Timeout = TimeSpan.FromMilliseconds(deliveryTimeoutMillis);
        }

        public async Task<Promoted.Delivery.Response> Deliver(Promoted.Delivery.Request req)
        {
            try
            {
                // Protobuf -> JSON
                string json = formatter.Format(req);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(deliveryEndpoint, content);
                // This throws if the request was unsuccessful.
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                // JSON -> Protobuf
                return (Promoted.Delivery.Response)parser.Parse(responseBody, Promoted.Delivery.Response.Descriptor);
            }
            catch (TaskCanceledException ex)
            {
                // TODO(james): Proper timeout behavior.
                Console.WriteLine($"Request timed out: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // TODO(james): Proper fallback behavior.
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }
    }
}
