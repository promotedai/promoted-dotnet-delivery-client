namespace Promoted.Lib
{
    public static class RequestProcessor
    {
        public static void FillNecessaryFields(Promoted.Delivery.Request req)
        {
            req.ClientInfo ??= new Common.ClientInfo();
            req.ClientInfo.ClientType = Common.ClientInfo.Types.ClientType.PlatformServer;
            req.ClientInfo.TrafficType = Common.ClientInfo.Types.TrafficType.Production;

            if (string.IsNullOrWhiteSpace(req.ClientRequestId))
            {
                req.ClientRequestId = Guid.NewGuid().ToString();
            }

            req.Timing ??= new Common.Timing();
            if (req.Timing.ClientLogTimestamp == 0)
            {
                req.Timing.ClientLogTimestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        public static void ConvertToShadowRequest(Promoted.Delivery.Request req)
        {
            req.ClientInfo ??= new Common.ClientInfo();
            req.ClientInfo.TrafficType = Common.ClientInfo.Types.TrafficType.Shadow;
        }

        public static bool IsInControl(Event.CohortMembership? experiment) {
            return experiment?.Arm == Event.CohortArm.Control;
        }
    }
}
