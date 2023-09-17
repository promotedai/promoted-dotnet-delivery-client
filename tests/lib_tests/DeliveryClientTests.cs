namespace lib_tests;

using Google.Protobuf;
using Moq;
using NLog;
using Promoted.Lib;
using System.Net;
using System.Text;

public class DeliveryClientTests
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private static readonly JsonFormatter _formatter = new JsonFormatter(JsonFormatter.Settings.Default);
    private static readonly JsonParser _parser = new JsonParser(JsonParser.Settings.Default);
    private Promoted.Delivery.Request _req = new Promoted.Delivery.Request();
    private Promoted.Delivery.Response _resp = new Promoted.Delivery.Response();
    private Mock<ICallbackHttpClient> _mockDeliveryHttpClient = new Mock<ICallbackHttpClient>();
    private Mock<ICallbackHttpClient> _mockMetricsHttpClient = new Mock<ICallbackHttpClient>();
    private string _deliveryEndpoint = "abc";
    private string _metricsEndpoint = "def";

    public DeliveryClientTests()
    {
        NLog.LogManager.Setup().LoadConfiguration(builder =>
        {
            builder.ForLogger().FilterMinLevel(LogLevel.Info).WriteToConsole();
        });
    }

    [Fact]
    public async Task NecessaryFieldsGetFilled()
    {
        string? delivery_req_content = null;
        _mockDeliveryHttpClient
            .Setup(client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
            .Callback<string, HttpContent>((_, content) =>
            {
                // We have to save this for later comparison because the original will be disposed of.
                delivery_req_content = content.ReadAsStringAsync().Result;
            });

        var client = new DeliveryClient(_mockDeliveryHttpClient.Object, _deliveryEndpoint,
                                        _mockMetricsHttpClient.Object, _metricsEndpoint);
        await client.Deliver(_req);
        client.Dispose();

        var req = (Promoted.Delivery.Request)_parser.Parse(delivery_req_content,
                                                           Promoted.Delivery.Request.Descriptor);
        // Just check one field.
        Assert.NotNull(req.ClientInfo);
    }

    [Fact]
    public async Task Control()
    {
        var client = new DeliveryClient(_mockDeliveryHttpClient.Object, _deliveryEndpoint,
                                        _mockMetricsHttpClient.Object, _metricsEndpoint);
        var options = new DeliveryRequestOptions();
        options.Experiment = new Event.CohortMembership();
        options.Experiment.Arm = Event.CohortArm.Control;
        await client.Deliver(_req, options);
        client.Dispose();

        // SDK delivery should happen instead.
        _mockDeliveryHttpClient.Verify(
            client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()),
            Times.Never);
        _mockMetricsHttpClient.Verify(
            client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(),
                                       It.IsAny<Action<Task<HttpResponseMessage>>>()),
            Times.Once);
    }

    [Fact]
    public async Task OnlyLogToMetrics()
    {
        var client = new DeliveryClient(_mockDeliveryHttpClient.Object, _deliveryEndpoint,
                                        _mockMetricsHttpClient.Object, _metricsEndpoint);
        var options = new DeliveryRequestOptions();
        options.OnlyLogToMetrics = true;
        await client.Deliver(_req, options);
        client.Dispose();

        // SDK delivery should happen instead.
        _mockDeliveryHttpClient.Verify(
            client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()),
            Times.Never);
        _mockMetricsHttpClient.Verify(
            client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(),
                                       It.IsAny<Action<Task<HttpResponseMessage>>>()),
            Times.Once);
    }

    [Fact]
    public async Task CallDeliveryFailure()
    {
        var client = new DeliveryClient(_mockDeliveryHttpClient.Object, _deliveryEndpoint,
                                        _mockMetricsHttpClient.Object, _metricsEndpoint);
        await client.Deliver(_req);
        client.Dispose();

        // Delivery happens, but fails because we didn't prepare the mock for a successful response.
        _mockDeliveryHttpClient.Verify(
            client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()),
            Times.Once);
        // And thus metrics happens too.
        _mockMetricsHttpClient.Verify(
            client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(),
                                       It.IsAny<Action<Task<HttpResponseMessage>>>()),
            Times.Once);
    }

    [Fact]
    public async Task CallDeliverySuccess()
    {
        _mockDeliveryHttpClient
            .Setup(client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_formatter.Format(_resp), Encoding.UTF8, "application/json")
            });

        var client = new DeliveryClient(_mockDeliveryHttpClient.Object, _deliveryEndpoint,
                                        _mockMetricsHttpClient.Object, _metricsEndpoint);
        await client.Deliver(_req);
        client.Dispose();

        _mockDeliveryHttpClient.Verify(
            client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()),
            Times.Once);
        _mockMetricsHttpClient.Verify(
            client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(),
                                       It.IsAny<Action<Task<HttpResponseMessage>>>()),
            Times.Never);
    }

    [Fact]
    public async Task Shadow()
    {
        var clientOptions = new DeliveryClientOptions();
        clientOptions.ShadowTrafficRate = 1;
        var client = new DeliveryClient(_mockDeliveryHttpClient.Object, _deliveryEndpoint,
                                        _mockMetricsHttpClient.Object, _metricsEndpoint, clientOptions);
        var requestOptions = new DeliveryRequestOptions();
        // Just do this to dodge delivery service.
        requestOptions.OnlyLogToMetrics = true;
        await client.Deliver(_req, requestOptions);
        client.Dispose();

        _mockDeliveryHttpClient.Verify(
            client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()),
            Times.Never);
        // Note this is the callback function for delivery. This isn't verifying a metrics call.
        _mockDeliveryHttpClient.Verify(
            client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(),
                                       It.IsAny<Action<Task<HttpResponseMessage>>>()),
            Times.Once);
    }
    
    [Fact]
    public async Task DontShadowBecauseWeTriedToCallDelivery()
    {
        var clientOptions = new DeliveryClientOptions();
        clientOptions.ShadowTrafficRate = 1;
        var client = new DeliveryClient(_mockDeliveryHttpClient.Object, _deliveryEndpoint,
                                        _mockMetricsHttpClient.Object, _metricsEndpoint, clientOptions);
        await client.Deliver(_req);
        client.Dispose();

        _mockDeliveryHttpClient.Verify(
            client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()),
            Times.Once);
        _mockDeliveryHttpClient.Verify(
            client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(),
                                       It.IsAny<Action<Task<HttpResponseMessage>>>()),
            Times.Never);
    }
}
