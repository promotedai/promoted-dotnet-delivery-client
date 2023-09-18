namespace lib_tests;

using Promoted.Lib;

public class MetricsTests
{
    [Fact]
    public void Basic()
    {
        var req = new Promoted.Delivery.Request();
        req.PlatformId = 100;
        req.UserInfo = new Promoted.Common.UserInfo();
        req.ClientInfo = new Promoted.Common.ClientInfo();
        req.Timing = new Promoted.Common.Timing();
        var resp = new Promoted.Delivery.Response();
        bool didSdkDelivery = false;
        Promoted.Event.CohortMembership? experiment = null;
        Promoted.Event.LogRequest logReq = Metrics.MakeLogRequest(req, resp, didSdkDelivery, experiment);

        Assert.Equal((ulong)100, logReq.PlatformId);
        Assert.NotNull(logReq.UserInfo);
        Assert.NotNull(logReq.ClientInfo);
        Assert.NotNull(logReq.Timing);
    }

    [Fact]
    public void DeliveryLog()
    {
        var req = new Promoted.Delivery.Request();
        var resp = new Promoted.Delivery.Response();
        bool didSdkDelivery = true;
        Promoted.Event.CohortMembership? experiment = null;
        Promoted.Event.LogRequest logReq = Metrics.MakeLogRequest(req, resp, didSdkDelivery, experiment);

        Assert.Single(logReq.DeliveryLog);
        Promoted.Delivery.DeliveryLog deliveryLog = logReq.DeliveryLog[0];
        Assert.NotNull(deliveryLog.Request);
        Assert.NotNull(deliveryLog.Response);
        Assert.Equal(Promoted.Delivery.ExecutionServer.Sdk, deliveryLog.Execution.ExecutionServer);
        // At least 3 digits and 2 periods.
        Assert.True(deliveryLog.Execution.ServerVersion.Length >= 5);
    }

    [Fact]
    public void Experiment()
    {
        var req = new Promoted.Delivery.Request();
        var resp = new Promoted.Delivery.Response();
        bool didSdkDelivery = false;
        var experiment = new Promoted.Event.CohortMembership();
        Promoted.Event.LogRequest logReq = Metrics.MakeLogRequest(req, resp, didSdkDelivery, experiment);

        Assert.Single(logReq.CohortMembership);
    }
}
