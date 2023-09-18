namespace lib_tests;

using Promoted.Lib;

public class RequestProcessorTests
{
    [Fact]
    public void FillNecessaryFields()
    {
        var req = new Promoted.Delivery.Request();
        RequestProcessor.FillNecessaryFields(req);
        Assert.Equal(Promoted.Common.ClientInfo.Types.ClientType.PlatformServer, req.ClientInfo.ClientType);
        Assert.Equal(Promoted.Common.ClientInfo.Types.TrafficType.Production, req.ClientInfo.TrafficType);
        Assert.False(string.IsNullOrWhiteSpace(req.ClientRequestId));
        Assert.Equal(36, req.ClientRequestId.Length);
        // Injecting a mockable time seems excessive. Just make sure this test passes for a long time.
        Assert.True(req.Timing.ClientLogTimestamp >=
                    (ulong)new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds());
        Assert.True(req.Timing.ClientLogTimestamp <=
                    (ulong)new DateTimeOffset(2073, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds());
    }

    [Fact]
    public void ConvertToShadowRequest()
    {
        var req = new Promoted.Delivery.Request();
        RequestProcessor.ConvertToShadowRequest(req);
        Assert.Equal(Promoted.Common.ClientInfo.Types.TrafficType.Shadow, req.ClientInfo.TrafficType);
    }

    [Fact]
    public void IsInControl()
    {
        Promoted.Event.CohortMembership? experiment = null;
        Assert.False(RequestProcessor.IsInControl(experiment));
        experiment = new Promoted.Event.CohortMembership();
        Assert.False(RequestProcessor.IsInControl(experiment));
        experiment.Arm = Promoted.Event.CohortArm.Treatment;
        Assert.False(RequestProcessor.IsInControl(experiment));

        experiment.Arm = Promoted.Event.CohortArm.Control;
        Assert.True(RequestProcessor.IsInControl(experiment));
    }

    [Fact]
    public void Validate()
    {
        // Just test all the leaf problems.
        var req = new Promoted.Delivery.Request();
        req.RequestId = "ShouldBeEmpty";
        req.UserInfo = new Promoted.Common.UserInfo();
        req.UserInfo.LogUserId = "";
        var badInsertionA = new Promoted.Delivery.Insertion();
        badInsertionA.InsertionId = "A";
        badInsertionA.ContentId = "";
        req.Insertion.Add(badInsertionA);
        var badInsertionB = new Promoted.Delivery.Insertion();
        badInsertionB.InsertionId = "B";
        badInsertionB.ContentId = "";
        req.Insertion.Add(badInsertionB);
        List<string> problems = RequestProcessor.Validate(req);
        Assert.Equal(5, problems.Count);
    }
}
