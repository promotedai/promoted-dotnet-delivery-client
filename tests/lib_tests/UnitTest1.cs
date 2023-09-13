namespace lib_tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var req = new Promoted.Delivery.Request();
        req.UserInfo = new Promoted.Common.UserInfo();
        req.UserInfo.UserId = "test";
        Console.WriteLine(req.UserInfo.UserId);
    }
}
