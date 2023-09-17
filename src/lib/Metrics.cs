using System.Reflection;

namespace Promoted.Lib
{
    public static class Metrics
    {
        // TODO(james): Fix the event C# namespace in schema.
        public static Event.LogRequest MakeLogRequest(Promoted.Delivery.Request req, Promoted.Delivery.Response resp,
                                                      bool didSdkDelivery, Event.CohortMembership? experiment)
        {
            var logReq = new Event.LogRequest();

            logReq.PlatformId = req.PlatformId;
            logReq.UserInfo = req.UserInfo;
            logReq.ClientInfo = req.ClientInfo;
            logReq.Timing = req.Timing;

            // If we did SDK delivery then we need to add a delivery log.
            if (didSdkDelivery)
            {
                var deliveryLog = new Delivery.DeliveryLog();
                deliveryLog.Request = req;
                deliveryLog.Response = resp;
                deliveryLog.Execution = new Delivery.DeliveryExecution();
                deliveryLog.Execution.ExecutionServer = Delivery.ExecutionServer.Sdk;
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                deliveryLog.Execution.ServerVersion = $"{version.Major}.{version.Minor}.{version.Build}";
                logReq.DeliveryLog.Add(deliveryLog);
            }

            if (experiment != null)
            {
                logReq.CohortMembership.Add(experiment);
            }

            return logReq;
        }
    }
}
