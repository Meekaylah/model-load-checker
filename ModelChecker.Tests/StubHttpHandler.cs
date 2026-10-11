using System.Net;

namespace ModelChecker.Tests;

// A pretend internet for tests. HttpClient sends every request through a "handler";
// by swapping in this one, no real network call is made and the test decides
// what comes back.
public class StubHttpHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _body;

    public StubHttpHandler(HttpStatusCode statusCode, string body = "")
    {
        _statusCode = statusCode;
        _body = body;
    }

    // Remembers the last address asked for, so a test can check it.
    public string? LastRequestPath { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestPath = request.RequestUri?.AbsolutePath;

        HttpResponseMessage response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_body)
        };

        return Task.FromResult(response);
    }
}
