namespace lib_tests;

using Google.Protobuf;
using Moq;
using Promoted.Lib;
using System.Net;
using System.Text;

public class DeliveryClientTests
{
    private static readonly JsonFormatter _formatter = new JsonFormatter(JsonFormatter.Settings.Default);
    private static readonly JsonParser _parser = new JsonParser(JsonParser.Settings.Default);
    private Promoted.Delivery.Request _req = new Promoted.Delivery.Request();
    private Promoted.Delivery.Response _resp = new Promoted.Delivery.Response();
    private Event.LogRequest _log_req = new Event.LogRequest();

    [Fact]
    public async Task DeliverSuccess()
    {
        string? delivery_req_content = null;
        var mockDeliveryHttpClient = new Mock<IHttpClient>();
        mockDeliveryHttpClient
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_formatter.Format(_resp), Encoding.UTF8, "application/json")
            })
            .Callback<string, HttpContent>((_, context) =>
            {
                // We have to save this for later comparison because the original will be disposed of.
                delivery_req_content = context.ReadAsStringAsync().Result;
            });
        string? metrics_req_content = null;
        var mockMetricsHttpClient = new Mock<IHttpClient>();
        mockMetricsHttpClient
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Callback<string, HttpContent>((_, context) =>
            {
                metrics_req_content = context.ReadAsStringAsync().Result;
            });

        string deliveryEndpoint = "abc";
        string metricsEndpoint = "def";
        var client = new DeliveryClient(mockDeliveryHttpClient.Object, deliveryEndpoint,
                                        mockMetricsHttpClient.Object, metricsEndpoint);
        await client.Deliver(_req);
        client.Dispose();

        mockDeliveryHttpClient.Verify(
            c => c.PostAsync(deliveryEndpoint,
                             It.Is<HttpContent>(content =>
                                 delivery_req_content == _formatter.Format(_req)
                                 && content.Headers.ContentType.CharSet == "utf-8"
                                 && content.Headers.ContentType.MediaType == "application/json")),
            Times.Once);
        mockMetricsHttpClient.Verify(
            c => c.PostAsync(metricsEndpoint,
                             It.Is<HttpContent>(content =>
                                 metrics_req_content == _formatter.Format(_log_req)
                                 && content.Headers.ContentType.CharSet == "utf-8"
                                 && content.Headers.ContentType.MediaType == "application/json")),
            Times.Once);
    }
}
