# tests for ArangoSnapshotStore

> For this test-suite we need to attach behaviour before the actual tests, so we need them to be virtual and override them.

Due to the nature of this store we need to mock the workings of an HttpClient.
This can be done by providing a "HttpMessageHandler", preferable in the form of an "DelegatingHandler".
That in turn means we have to store a Func until the HttpClient is called (_handlerFunctions).
Because we possibly need to support multiple calls per test we need to store multiple functions and call them in the right order.

For all this to work we need to bind the tests so tightly to the actual implementation, 
and provide such complex logic that these "unit"-tests become pretty brittle and useless.

This is an example of what is described above.

DelegatingHandlerStub only needs to retrieve and execute the desired Func after it was created.
At the time of creation we haven't had the chance to store our HttpClient-Response-Handler yet, so we need to defer that.

The tests need to be overriden, so that they store the necessary Response-Handler-Funcs before executing the tests.

```c#
private readonly List<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _handlerFunctions;
private int _currentFunction;

public ArangoSnapshotStoreTests()
{
    _handlerFunctions = new List<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>();
    _currentFunction = 0;

    var clientFactory = new Mock<IHttpClientFactory>();
    clientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                 .Returns(() => new HttpClient(new DelegatingHandlerStub(() => _handlerFunctions[_currentFunction++]))
                 {
                     BaseAddress = new Uri("http://localhost/arango-mock/")
                 });

    Store = new ArangoSnapshotStore(clientFactory.Object, ...);
}

/// <inheritdoc />
public override Task GetLatestSnapshotNumbers()
    => SetHttpObjectResult(base.GetLatestSnapshotNumbers,
                            new {code = 0, error = false, hasMore = false, result = new[] {new {MetaVersion = 1}}});

/// <inheritdoc />
public override Task StoreGenericSnapshot() => SetHttpResult(base.StoreGenericSnapshot);

/// <summary>
///     configure the <see cref="DelegatingHandlerStub"/> to return the given object as a JSON-Response
/// </summary>
private Task SetHttpObjectResult(Func<Task> testFunc, object result)
    => SetHttpResult(testFunc,
                        (request, token)
                            => Task.FromResult(
                                request.CreateResponse(
                                    HttpStatusCode.OK,
                                    result,
                                    new JsonMediaTypeFormatter(),
                                    "application/json")));

/// <summary>
///     configure the <see cref="DelegatingHandlerStub"/> to return a simple 200-OK
/// </summary>
private Task SetHttpResult(Func<Task> testFunc)
    => SetHttpResult(testFunc, (request, token) => Task.FromResult(request.CreateResponse(HttpStatusCode.OK)));

/// <summary>
///     configure the <see cref="DelegatingHandlerStub"/> to return the result of the function returned from <paramref name="handlerFunctions"/>.
///     all <paramref name="handlerFunctions"/> are stored until the configured HttpClient is used.
///     once the HttpClient is activated, the current item in <paramref name="handlerFunctions"/> is taken to produce the actual result.
/// </summary>
private Task SetHttpResult(Func<Task> testFunc, params Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>[] handlerFunctions)
{
    _handlerFunctions.AddRange(handlerFunctions);
    return testFunc();
}

public class DelegatingHandlerStub : DelegatingHandler
{
    private readonly Func<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _handlerProvider;

    public DelegatingHandlerStub(Func<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> handlerProvider)
    {
        _handlerProvider = handlerProvider;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => _handlerProvider()(request, cancellationToken);
}
```