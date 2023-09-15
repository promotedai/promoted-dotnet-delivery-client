namespace Promoted.Lib
{
    public static class RequestProcessor
    {
        public static void FillNecessaryFields(Promoted.Delivery.Request req)
        {
            if (req.ClientInfo == null)
            {
                req.ClientInfo = new Common.ClientInfo();
            }
            req.ClientInfo.ClientType = Common.ClientInfo.Types.ClientType.PlatformServer;
            req.ClientInfo.TrafficType = Common.ClientInfo.Types.TrafficType.Production;

            if (string.IsNullOrWhiteSpace(req.ClientRequestId))
            {
                req.ClientRequestId = Guid.NewGuid().ToString();
            }

            if (req.Timing == null)
            {
                req.Timing = new Common.Timing();
            }
            if (req.Timing.ClientLogTimestamp == 0)
            {
                req.Timing.ClientLogTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }
    }
}
