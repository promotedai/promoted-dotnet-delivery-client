namespace Promoted.Lib
{
    public static class RequestProcessor
    {
        private static DateTimeOffset _epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

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
                // UtcNow by itself returns the time since January 1, 0001.
                req.Timing.ClientLogTimestamp = (ulong)Math.Round((DateTimeOffset.UtcNow - _epoch).TotalMilliseconds);
            }
        }
    }
}
