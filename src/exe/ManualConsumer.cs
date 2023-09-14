namespace Promoted.Exe
{
    class ManualConsumer
    {
        static async Task Main(string[] args)
        {
            // Create the client.
            string deliveryEndpoint = "http://localhost:9090/deliver";
            string deliveryApiKey = "abc";
            int deliveryTimeoutMillis = 100;
            var client = new Promoted.Lib.DeliveryClient(deliveryEndpoint, deliveryApiKey, deliveryTimeoutMillis);

            // Use the client.
            var req = new Promoted.Delivery.Request();
            Console.WriteLine($"Request:\t{req}");
            Promoted.Delivery.Response resp = await client.Deliver(req);
            Console.WriteLine($"Response:\t{resp}");
        }
    }
}
