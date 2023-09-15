namespace lib_tests;

using Promoted.Lib;

public class DeliveryClientOptionsTests
{
    [Fact]
    public void ShadowTrafficRateDefault()
    {
        var options = new DeliveryClientOptions();
        Assert.Equal(0, options.ShadowTrafficRate);
    }

    [Fact]
    public void ShadowTrafficRateValid()
    {
        var options = new DeliveryClientOptions();
        // Will throw and fail the test if not valid.
        options.ShadowTrafficRate = 0;
        options.ShadowTrafficRate = 0.5F;
        options.ShadowTrafficRate = 1;
    }

    [Fact]
    public void ShadowTrafficRateInvalid()
    {
        var options = new DeliveryClientOptions();
        Assert.Throws<ArgumentException>(() => options.ShadowTrafficRate = -1);
        Assert.Throws<ArgumentException>(() => options.ShadowTrafficRate = 2);
    }
}
