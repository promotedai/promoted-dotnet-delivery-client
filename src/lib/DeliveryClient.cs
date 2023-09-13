using Google.Protobuf;
using System.Text;

namespace Promoted.Lib
{
    public class DeliveryClient
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly JsonFormatter formatter = new JsonFormatter(JsonFormatter.Settings.Default);
        private static readonly JsonParser parser = new JsonParser(JsonParser.Settings.Default);
        private readonly string url;

        public DeliveryClient(string url, string apiKey)
        {
            this.url = url;
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        }

        public async Task<Promoted.Delivery.Response> Deliver(Promoted.Delivery.Request req)
        {
            try
            {
                // Protobuf -> JSON
                string json = formatter.Format(req);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(url, content);
                // This throws if the request was unsuccessful.
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                // JSON -> Protobuf
                return (Promoted.Delivery.Response)parser.Parse(responseBody, Promoted.Delivery.Response.Descriptor);
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
