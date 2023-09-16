namespace lib_tests;

using Promoted.Lib;

public class DeliveryRequestOptionsTests
{
    [Fact]
    public void RetrievalInsertionOffsetDefault()
    {
        var options = new DeliveryRequestOptions();
        Assert.Equal(0, options.RetrievalInsertionOffset);
    }

    [Fact]
    public void RetrievalInsertionOffsetValid()
    {
        var options = new DeliveryRequestOptions();
        // Will throw and fail the test if not valid.
        options.RetrievalInsertionOffset = 0;
        options.RetrievalInsertionOffset = 100;
    }

    [Fact]
    public void RetrievalInsertionOffsetInvalid()
    {
        var options = new DeliveryRequestOptions();
        Assert.Throws<ArgumentException>(() => options.RetrievalInsertionOffset = -1);
    }
}
