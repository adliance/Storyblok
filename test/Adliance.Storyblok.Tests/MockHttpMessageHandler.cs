using System;
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
/// Therefore, if a request is made, we return a 404 response, unless <see cref="Responder"/> returns a response for the request.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    public List<string> Requests { get; } = [];

    /// <summary>
    /// Optional custom response for a request. If it returns <c>null</c>, a 404 response is returned.
    /// </summary>
    public Func<HttpRequestMessage, HttpResponseMessage?>? Responder { get; set; }

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
        // requests can also be sent from background tasks (e.g. loading the story index)
        lock (Requests)
        {
            Requests.Add(request.RequestUri?.ToString() ?? "");
        }

        var response = Responder?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.NotFound);
        response.RequestMessage = request;
        return response;
    }
}
