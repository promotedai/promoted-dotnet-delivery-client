# promoted-dotnet-delivery-client
.NET (targeting `netstandard2.0`) client SDK for the Promoted.ai Delivery API

## Features

- Demonstrates and implements the recommended practices and data types for calling Promoted.ai's Delivery API.
- Client-side position assignment and paging when not using results from Delivery API, for example when logging only or as part of an experiment control.

## Creating a DeliveryClient

The `Promoted.Lib.DeliveryClient` is currently intended to have one instance per pair (Delivery and Metrics) of endpoints. It is thread-safe, but is built on top of `System.Net.Http.HttpClient` so it implements `IDisposable`.

```c#
var options = new Promoted.Lib.DeliveryClientOptions(); // Optional.
var client = new Promoted.Lib.DeliveryClient(
    deliveryEndpoint, deliveryApiKey, deliveryTimeoutMillis,
    metricsEndpoint, metricsApiKey, metricsTimeoutMillis,
    options);
...
client.Dispose();
```

### DeliveryClient Constructor Parameters

| Name                           | Type                           | Description                                                                                                                                                                                                                                                                                                 |
| ------------------------------ | ------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `deliveryEndpoint`            | string                            | API endpoint for Promoted.ai's Delivery API.                                                                                                                                                                                                                                                                 |
| `deliveryApiKey`             | string                            | API key used in the `x-api-key` header for Promoted.ai's Delivery API.                                                                                                                                                                                                                                       |
| `deliveryTimeoutMillis`      | int                            | Timeout on the Delivery API call.                                                                                                                                                                                                                                                          |
| `metricsEndpoint`             | string                            | API endpoint for Promoted.ai's Metrics API.                                                                                                                                                                                                                                                                  |
| `metricsApiKey`              | string                            | API key used in the `x-api-key` header for Promoted.ai's Metrics API.                                                                                                                                                                                                                                        |
| `metricsTimeoutMillis`       | int                            | Timeout on the Metrics API call.                                                                                                                                                                                                                                                         |
| `options` | Promoted.Lib.DeliveryClientOptions?          | Optional. Specifies additional options, which are described below.                                                                                                                                  |

### DeliveryClientOptions Properties

| Name                           | Type                           | Description                                                                                                                       |
| ------------------------------ | ------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ShadowTrafficRate` | float between 0 and 1          | rate = [0,1] of traffic that gets directed to Delivery API as "shadow traffic". Only applies to cases where Delivery API is not called. Defaults to 0 (no shadow traffic). Throws an `ArgumentException` when trying to set a valid outside specified range.                                                                                                                                  |
| `Validate` | bool | Performs some validation that request fields are filled properly. These checks take time so this should be turned off once a request is satisfactory. |

### DeliveryClient Methods

| Method    | Input           | Output           | Description                                                                                           |
| --------- | --------------- | ---------------- | ----------------------------------------------------------------------------------------------------- |
| `Deliver` | Promoted.Delivery.Request, Promoted.Lib.DeliveryRequestOptions? | Task<Promoted.Delivery.Response> | Makes a request (subject to experimentation) to Delivery API for insertions, which are then returned. Second argument is optional. Specifies additional options, which are described below. When null, the default values below are used. |
| `Dispose` |  | void | Disposes of base HTTP clients. |

### DeliveryRequestOptions Properties

| Name                           | Type                           | Description                                                                                                                     |
| ------------------------------ | ------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `OnlyLogToMetrics` | bool | Defaults to false. Set to true to log the request as the CONTROL arm of an experiment, not call Delivery API, but rather deliver paged insertions from the request. |
| `Experiment` | Promoted.Event.CohortMembership? | Optional. A cohort to evaluation in experimentation. |
| `RetrievalInsertionOffset` | int | The offset the initial request insertion corresponds to in the list of ALL insertions. See [Pages of Request Insertions](#pages-of-request-insertions) for more details.

## Calling the Delivery API

Let's say the previous code looks like this:

```c#
void async GetProducts(ProductRequest req):
    var products = ...; // Logic to get products from DB, apply filtering, etc.
    SendSuccessToClient(products)
```

We might modify to something like this:

```c#
void async GetProducts(ProductRequest req):
    var products = ...; // Logic to get products from DB, apply filtering, etc.

    List<Promoted.Delivery.Insertion> insertions = new List<Promoted.Delivery.Insertion>();
    // Keep a dictionary for reordering.
    Dictionary<string, Product> productMap = new Dictionary<string, Product>();
    foreach (Product product in products)
    {
        var insertion = new Promoted.Delivery.Insertion();
        insertion.ContentId = product.ProductId;
        insertions.Add(insertion);
        productMap.Add(product.ProductId, product);
    }

    var req = new Promoted.Delivery.Request();
    req.UserInfo = new Promoted.Common.UserInfo();
    req.UserInfo.UserId = "abc";
    req.Paging = new Delivery.Paging();
    req.Paging.Offset = 0;
    req.Paging.Size = 100;
    req.Insertion.AddRange(insertions);

    Promoted.Delivery.Response resp = await client.Deliver(req);

    List<Product> rankedProducts = new List<Product>();
    foreach (var insertion in resp.Insertion)
    {
        rankedProducts.Add(productMap[insertion.ContentId]);
    }

    SendSuccessToClient(rankedProducts)
```

Note that `await client.Deliver(req)` can throw here.

## Data Types

### UserInfo

Basic information about the request user.

| Field Name | Type | Optional? | Description |
---------- | ---- | --------- | -----------
`UserId` | string | Yes | The authenticated user id. This is not a saved directly in long term logs. This gets pseudo anonymized.
`AnonUserId` | string | Yes | A different user id (presumably a UUID) disconnected from the authenticated user id (e.g. an "anonymous user id"), good for working with unauthenticated users or implementing right-to-be-forgotten.
`IsInternalUser` | bool | Yes | If this user is a test user or not, defaults to false.

### CohortMembership

Useful fields for experimentation during the delivery phase.

| Field Name | Type | Optional? | Description |
---------- | ---- | --------- | -----------
`Arm` | string | Yes | 'CONTROL' or one of the TREATMENT values ('TREATMENT', 'TREATMENT1', etc.).
`CohortId` | string | Yes | Name of the cohort (e.g. "LOCAL_HOLDOUT" etc.)

### Properties

Properties bag. Has the JSON structure:

```json
  "struct": {
    "product": {
      "id": "product3",
      "title": "Product 3",
      "url": "www.mymarket.com/p/3"
      // Other <key, value> pairs...
    }
  }
```

### Promoted.Delivery.Insertion

Content being served at a certain position.

| Field Name | Type | Optional? | Description |
---------- | ---- | --------- | -----------
`UserInfo` | Promoted.Common.UserInfo | Yes | The user info structure.
`InsertionId` | string | Yes | Generated by the SDK (_do not set_).
`ContentId` | string | No | Identifier for the content to be ranked, must be set.
`RetrievalRank` | int | Yes | Optional original ranking of this content item.
`RetrievalScore` | float | Yes | Optional original quality score of this content item.
`Properties` | Promoted.Common.Properties | Yes | Any additional custom properties to associate. For v1 integrations, it is fine not to fill in all the properties.

### Promoted.Common.Size

User's screen dimensions.

| Field Name | Type | Optional? | Description |
---------- | ---- | --------- | -----------
`Width` | int | No | Screen width.
`Height` | int | No | Screen height.

### Promoted.Common.Screen

State of the screen including scaling.

| Field Name | Type | Optional? | Description |
---------- | ---- | --------- | -----------
`Size` | Promoted.Common.Size | Yes | Screen size.
`Scale` | float | Yes | Current screen scaling factor.

### Promoted.Common.ClientHints

Alternative to user-agent strings. See https://raw.githubusercontent.com/snowplow/iglu-central/master/schemas/org.ietf/http_client_hints/jsonschema/1-0-0.

| Field Name | Type | Optional? | Description |
---------- | ---- | --------- | -----------
`IsMobile` | bool | Yes | Mobile flag.
`Brand` | Promoted.Common.ClientBrandHint (repeated) | Yes |
`Architecture` | string | Yes |
`Model` | string | Yes |
`Platform` | string | Yes |
`Platform_version` | string | Yes |
`UaFullVersion` | string | Yes |

### Promoted.Common.ClientBrandHint

See https://raw.githubusercontent.com/snowplow/iglu-central/master/schemas/org.ietf/http_client_hints/jsonschema/1-0-0.

| Field Name | Type | Optional? | Description |
---------- | ---- | --------- | -----------
`Brand` | string | Yes |
`Version` | string | Yes |

### Promoted.Common.Location

Information about the user's location.

| Field Name | Type | Optional? | Description |
---------- | ---- | --------- | -----------
`Latitude` | float | No | Location latitude.
`Longitude` | float | No | Location longitude.
`AccuracyInMeters` | int | Yes | Location accuracy if available.

### Promoted.Common.Browser

Information about the user's browser.

| Field Name | Type | Optional? | Description |
---------- | ---- | --------- | -----------
`UserAgent` | string | Yes | Browser user agent string.
`ViewportSize` | Promoted.Common.Size | Yes | Size of the browser viewport.
`ClientHints` | Promoted.Common.ClientHints | Yes | HTTP client hints structure.

### Promoted.Common.Device

Information about the user's device.

| Field Name | Type | Optional? | Description |
---------- | ---- | --------- | -----------
`DeviceType` | one of (`UNKNOWN_DEVICE_TYPE`, `DESKTOP`, `MOBILE`, `TABLET`) | Yes | Type of device.
`Brand` | string | Yes | "Apple, "google", Samsung", etc.
`Manufacturer` | string | Yes | "Apple", "HTC", Motorola", "HUAWEI", etc.
`Identifier` | string | Yes | Android: android.os.Build.MODEL; iOS: iPhoneXX,YY, etc.
`Screen` | Promoted.CommonScreen | Yes | Screen dimensions.
`IpAddress` | string | Yes | Originating IP address.
`Location` | Promoted.CommonLocation | Yes | Location information.
`Browser` | Promoted.CommonBrowser | Yes | Browser information.

### Promoted.Common.Paging

Describes a page of insertions.

| Field Name | Type | Optional? | Description |
---------- | ---- | --------- | -----------
`Size` | int | Yes | Size of the page being requested.
`Offset` | int | Yes | Page offset.

### Promoted.Delivery.Request

A request for content insertions.

| Field Name | Type | Optional? | Description |
---------- | ---- | --------- | -----------
`Insertion` | Promoted.Delivery.Insertion (repeated) | No | The set of insertions to consider.
`UserInfo` | Promoted.Common.UserInfo | Yes | The user info structure.
`RequestId` | string | Yes | Generated by the SDK when needed (_do not set_).
`UseCase` | string | Yes | One of the use case enum values or strings, i.e. 'FEED', 'SEARCH', etc.
`Properties` | Promoted.Common.Properties | Yes | Any additional custom properties to associate.
`Paging` | Promoted.Common.Paging | Yes | Paging parameters.
`Device` | Promoted.Common.Device | Yes | Device information (as available).


### Promoted.Delivery.Response

Output of `Deliver()`. Includes the ranked insertions for you to display.

| Field Name | Type | Optional? | Description |
---------- | ---- | --------- | -----------
`Insertion` | Promoted.Delivery.Insertion (repeated) | No | The ranked set of insertions (when `Deliver()` was called, i.e. we weren't either only-log or part of an experiment) or the input insertions (when the other conditions don't hold).
`RequestId` | string | No | The ID from the corresponding request. Generated as part of processing. May be useful for logging and debugging.
`PagingInfo` | Promoted.Delivery.PagingInfo | Yes | Information about the paging context for the corresponding request. Only populated if delivery service was successfully called.

## Pages of Request Insertions

Clients can send a subset of all request insertions to Promoted in Delivery API's `Request.Insertion` array. The `RetrievalInsertionOffset` option specifies the start index of the array `Request.Insertion` in the list of ALL request insertions.

`Request.Paging.Offset` should be set to the zero-based position in ALL request insertions (_not_ the relative position in the `Request.Insertion` array).

Examples

- If there are 10 items and all 10 items are in `Request.Insertion`, then `RetrievalInsertionOffset=0`.
- If there are 10,000 items and the first 500 items are on `Request.Insertion`, then `RetrievalInsertionOffset=0`.
- If there are 10,000 items and we want to send items [500,1000) on `Request.Insertion`, then `RetrievalInsertionOffset=500`.
- If there are 10,000 items and we want to send the last page [9500,10000) on `Request.Insertion`, then `RetrievalInsertionOffset=9500`.

`RetrievalInsertionOffset` is required to be less than `Paging.Offset` or else a `ValueArgumentExceptionError` will result.

Additional details: https://docs.promoted.ai/docs/ranking-requests#how-to-send-more-insertions-than-the-top-few-hundred

### Position

- Do not set the insertion `Position` field in client code. The SDK and Delivery API will set it when `Deliver()` is called.

## Logging only

You can use `Deliver()` but enable the `OnlyLogToMetrics` option.
