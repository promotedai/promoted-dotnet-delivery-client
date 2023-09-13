namespace Promoted.Exe
{
    class ManualConsumer
    {
        static async Task Main(string[] args)
        {
            // Create the client.
            string url = "http://localhost:9090/deliver";
            string apiKey = "abc";
            var client = new Promoted.Lib.DeliveryClient(url, apiKey);

            // Use the client.
            var req = new Promoted.Delivery.Request();
            Console.WriteLine($"Request:\t{req}");
            Promoted.Delivery.Response resp = await client.Deliver(req);
            Console.WriteLine($"Response:\t{resp}");
        }
    }
}
