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

        public static bool IsInControl(Event.CohortMembership? experiment)
        {
            return experiment?.Arm == Event.CohortArm.Control;
        }

        public static List<string> Validate(Promoted.Delivery.Request req)
        {
            List<string> problems = new List<string>();

            if (!string.IsNullOrWhiteSpace(req.RequestId))
            {
                problems.Add("Request.requestId should not be set");
            }

            if (req.UserInfo == null)
            {
                problems.Add("Request.userInfo should be set");
            }

            if (req.Insertion == null)
            {
                problems.Add("Request.insertion should be set");
            }
            else
            {
                foreach (Promoted.Delivery.Insertion insertion in req.Insertion)
                {
                    if (!string.IsNullOrWhiteSpace(insertion.InsertionId))
                    {
                        problems.Add("Insertion.insertionId should not be set");
                    }
                    if (string.IsNullOrWhiteSpace(insertion.ContentId))
                    {
                        problems.Add("Insertion.contentId should be set");
                    }
                }
            }

            return problems;
        }
    }
}
