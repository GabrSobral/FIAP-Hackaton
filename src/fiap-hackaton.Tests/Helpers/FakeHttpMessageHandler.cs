using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Moq;

namespace fiap_hackaton.Tests.Helpers;

/// <summary>
/// Fake HttpMessageHandler that dequeues responses in order.
/// The last response in the queue is repeated for any extra calls.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _queue;

    public FakeHttpMessageHandler(params HttpResponseMessage[] responses)
        => _queue = new Queue<HttpResponseMessage>(responses);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = _queue.Count > 1 ? _queue.Dequeue() : _queue.Peek();
        return Task.FromResult(response);
    }

    public static IHttpClientFactory Factory(params HttpResponseMessage[] responses)
    {
        var handler = new FakeHttpMessageHandler(responses);
        var client  = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        var mock    = new Mock<IHttpClientFactory>();
        mock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return mock.Object;
    }

    public static HttpResponseMessage Ok(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    public static HttpResponseMessage TooManyRequests()
    {
        var r = new HttpResponseMessage((HttpStatusCode)429);
        r.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);
        return r;
    }

    public static HttpResponseMessage InternalServerError()
        => new(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error")
        };
}
