namespace lib_tests;

using Promoted.Lib;

public class SdkDeliveryTests
{
    private Promoted.Delivery.Insertion MakeInsertion(string contentId) {
        var insertion = new Promoted.Delivery.Insertion();
        insertion.ContentId = contentId;
        return insertion;
    }

    [Fact]
    public void RequestIdFilled()
    {
        var req = new Promoted.Delivery.Request();
        Promoted.Delivery.Response resp = SdkDelivery.Deliver(req);
        Assert.Equal(36, req.RequestId.Length);
    }

    [Fact]
    public void Basic()
    {
        var req = new Promoted.Delivery.Request();
        req.Paging = new Promoted.Delivery.Paging();
        req.Paging.Offset = 2;
        req.Paging.Size = 3;
        req.Insertion.Add(MakeInsertion("0"));
        req.Insertion.Add(MakeInsertion("1"));
        req.Insertion.Add(MakeInsertion("2"));
        req.Insertion.Add(MakeInsertion("3"));
        req.Insertion.Add(MakeInsertion("4"));
        req.Insertion.Add(MakeInsertion("5"));
        Promoted.Delivery.Response resp = SdkDelivery.Deliver(req);

        Assert.Equal(3, resp.Insertion.Count);
        Assert.Equal("2", resp.Insertion[0].ContentId);
        Assert.Equal("3", resp.Insertion[1].ContentId);
        Assert.Equal("4", resp.Insertion[2].ContentId);
    }

    [Fact]
    public void NullPaging()
    {
        var req = new Promoted.Delivery.Request();
        req.Insertion.Add(MakeInsertion("0"));
        req.Insertion.Add(MakeInsertion("1"));
        req.Insertion.Add(MakeInsertion("2"));
        req.Insertion.Add(MakeInsertion("3"));
        req.Insertion.Add(MakeInsertion("4"));
        req.Insertion.Add(MakeInsertion("5"));
        Promoted.Delivery.Response resp = SdkDelivery.Deliver(req);

        Assert.Equal(6, resp.Insertion.Count);
        Assert.Equal("0", resp.Insertion[0].ContentId);
        Assert.Equal("1", resp.Insertion[1].ContentId);
        Assert.Equal("2", resp.Insertion[2].ContentId);
        Assert.Equal("3", resp.Insertion[3].ContentId);
        Assert.Equal("4", resp.Insertion[4].ContentId);
        Assert.Equal("5", resp.Insertion[5].ContentId);
    }

    [Fact]
    public void InvalidPagingSize()
    {
        var req = new Promoted.Delivery.Request();
        req.Paging = new Promoted.Delivery.Paging();
        req.Paging.Offset = 2;
        req.Paging.Size = 0;
        req.Insertion.Add(MakeInsertion("0"));
        req.Insertion.Add(MakeInsertion("1"));
        req.Insertion.Add(MakeInsertion("2"));
        req.Insertion.Add(MakeInsertion("3"));
        req.Insertion.Add(MakeInsertion("4"));
        req.Insertion.Add(MakeInsertion("5"));
        Promoted.Delivery.Response resp = SdkDelivery.Deliver(req);

        Assert.Equal(4, resp.Insertion.Count);
        Assert.Equal("2", resp.Insertion[0].ContentId);
        Assert.Equal("3", resp.Insertion[1].ContentId);
        Assert.Equal("4", resp.Insertion[2].ContentId);
        Assert.Equal("5", resp.Insertion[3].ContentId);
    }

    [Fact]
    public void RetrievalInsertionCount()
    {
        var req = new Promoted.Delivery.Request();
        req.Paging = new Promoted.Delivery.Paging();
        req.Paging.Offset = 2;
        req.Paging.Size = 3;
        req.Insertion.Add(MakeInsertion("0"));
        req.Insertion.Add(MakeInsertion("1"));
        req.Insertion.Add(MakeInsertion("2"));
        req.Insertion.Add(MakeInsertion("3"));
        req.Insertion.Add(MakeInsertion("4"));
        req.Insertion.Add(MakeInsertion("5"));
        var options = new DeliveryRequestOptions();
        options.RetrievalInsertionOffset = 1;
        Promoted.Delivery.Response resp = SdkDelivery.Deliver(req, options);

        Assert.Equal(3, resp.Insertion.Count);
        Assert.Equal("1", resp.Insertion[0].ContentId);
        Assert.Equal("2", resp.Insertion[1].ContentId);
        Assert.Equal("3", resp.Insertion[2].ContentId);
    }

    [Fact]
    public void InvalidOffsetAndRetrievalInsertionOffset()
    {
        var req = new Promoted.Delivery.Request();
        req.Paging = new Promoted.Delivery.Paging();
        req.Paging.Offset = 5;
        var options = new DeliveryRequestOptions();
        options.RetrievalInsertionOffset = 6;
        Assert.Throws<ArgumentException>(() => SdkDelivery.Deliver(req, options));
    }
}
