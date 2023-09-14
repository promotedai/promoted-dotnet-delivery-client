namespace Promoted.Exe
{
    class ManualConsumer
    {
        static string GetEnvironmentVariableOrThrow(string varName)
        {
            string? envVar = Environment.GetEnvironmentVariable(varName);
            if (envVar == null)
            {
                throw new InvalidOperationException($"Environment variable {varName} is not set.");
            }
            return envVar;
        }

        static async Task Main(string[] args)
        {
            Promoted.Lib.DeliveryClient client;
            try
            {
                // Create the client.
                string deliveryEndpoint = GetEnvironmentVariableOrThrow("DELIVERY_ENDPOINT");
                string deliveryApiKey = GetEnvironmentVariableOrThrow("DELIVERY_API_KEY");
                int deliveryTimeoutMillis = int.Parse(GetEnvironmentVariableOrThrow("DELIVERY_TIMEOUT_MILLIS"));
                string metricsEndpoint = GetEnvironmentVariableOrThrow("METRICS_ENDPOINT");
                string metricsApiKey = GetEnvironmentVariableOrThrow("METRICS_API_KEY");
                int metricsTimeoutMillis = int.Parse(GetEnvironmentVariableOrThrow("METRICS_TIMEOUT_MILLIS"));
                client = new Promoted.Lib.DeliveryClient(deliveryEndpoint, deliveryApiKey, deliveryTimeoutMillis,
                                                         metricsEndpoint, metricsApiKey, metricsTimeoutMillis);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create client: {ex.Message}");
                return;
            }

            // Use the client.
            var req = new Promoted.Delivery.Request();
            Console.WriteLine($"Request:\t{req}");
            Promoted.Delivery.Response resp = await client.Deliver(req);
            Console.WriteLine($"Response:\t{resp}");

            client.Dispose();
        }
    }
}
