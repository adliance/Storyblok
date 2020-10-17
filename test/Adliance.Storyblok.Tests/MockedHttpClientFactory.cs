using System.Net.Http;

namespace Adliance.Storyblok.Tests
{
    public class MockedHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }
}