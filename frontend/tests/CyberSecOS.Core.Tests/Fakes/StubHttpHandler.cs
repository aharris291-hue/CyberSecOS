using System.Net;
using System.Text;

namespace CyberSecOS.Core.Tests.Fakes;

/// <summary>Records requests and replies with a canned response.</summary>
public sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _respond = respond;
    }

    public StubHttpHandler(HttpStatusCode status, string json)
        : this(_ => Json(status, json))
    {
    }

    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];

    public static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));
        return _respond(request);
    }

    public HttpClient CreateClient() => new(this) { BaseAddress = new Uri("http://test.local/") };
}
