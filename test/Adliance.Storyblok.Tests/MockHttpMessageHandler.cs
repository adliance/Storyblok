using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Adliance.Storyblok.Tests;

/// <summary>
/// Records every outgoing request instead of hitting the Storyblok API.
/// We hook in at the message handler, because that's the only method that all
/// HttpClient methods (GetAsync, GetByteArrayAsync, ...) actually funnel through.
/// This is mostly intended to test that no requests are made to the Storyblok API.
/// Therefore, if a request is made, we return a 404 response.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    public List<string> Requests { get; } = [];

    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Record(request);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Record(request));
    }

    private HttpResponseMessage Record(HttpRequestMessage request)
    {
        Requests.Add(request.RequestUri?.ToString() ?? "");

        return new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request };
    }
}
