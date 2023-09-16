namespace lib_tests;

using Promoted.Lib;

public class DeliveryRequestOptionsTests
{
    [Fact]
    public void InsertionStartIndexDefault()
    {
        var options = new DeliveryRequestOptions();
        Assert.Equal(0, options.InsertionStartIndex);
    }

    [Fact]
    public void InsertionStartIndexValid()
    {
        var options = new DeliveryRequestOptions();
        // Will throw and fail the test if not valid.
        options.InsertionStartIndex = 0;
        options.InsertionStartIndex = 100;
    }

    [Fact]
    public void InsertionStartIndexInvalid()
    {
        var options = new DeliveryRequestOptions();
        Assert.Throws<ArgumentException>(() => options.InsertionStartIndex = -1);
    }
}
