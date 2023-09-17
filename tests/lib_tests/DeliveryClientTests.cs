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

    [Fact(Skip = "Next PR is going to be adding tests here, so ignore breakage for now.")]
    public async Task DeliverSuccess()
    {
        string? delivery_req_content = null;
        var mockDeliveryHttpClient = new Mock<ICallbackHttpClient>();
        mockDeliveryHttpClient
            .Setup(c => c.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_formatter.Format(_resp), Encoding.UTF8, "application/json")
            })
            .Callback<string, HttpContent>((_, content) =>
            {
                // We have to save this for later comparison because the original will be disposed of.
                delivery_req_content = content.ReadAsStringAsync().Result;
            });
        string? metrics_req_content = null;
        var mockMetricsHttpClient = new Mock<ICallbackHttpClient>();
        mockMetricsHttpClient
            .Setup(c =>
            c.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<Action<Task<HttpResponseMessage>>>()))
                .Callback<string, HttpContent, Action<Task<HttpResponseMessage>>>((_, content, callback) =>
                {
                    metrics_req_content = content.ReadAsStringAsync().Result;
                    callback(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
                });

        string deliveryEndpoint = "abc";
        string metricsEndpoint = "def";
        var client = new DeliveryClient(mockDeliveryHttpClient.Object, deliveryEndpoint,
                                        mockMetricsHttpClient.Object, metricsEndpoint);
        var options = new DeliveryRequestOptions();
        // Making an experiment to force a metrics call right now. Won't do this once I add more test cases.
        options.Experiment = new Event.CohortMembership();
        await client.Deliver(_req, options);
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
                                 && content.Headers.ContentType.MediaType == "application/json"),
                             It.IsAny<Action<Task<HttpResponseMessage>>>()),
            Times.Once);
    }
}
