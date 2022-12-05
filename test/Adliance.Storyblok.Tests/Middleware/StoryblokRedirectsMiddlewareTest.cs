using System.Net;
using System.Threading.Tasks;
using Adliance.Storyblok.Tests.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Adliance.Storyblok.Tests.Middleware
{
    public class StoryblokRedirectsMiddlewareTest
    {
        private readonly MockedWebApplicationFactory<MockedStartup> _factory;

        public StoryblokRedirectsMiddlewareTest()
        {
            Thread.DontBombardStoryblokApi();
            _factory = new MockedWebApplicationFactory<MockedStartup>();
        }

        [Theory]
        [InlineData("/REDIRECT-from", "/redirect-to")]
        [InlineData("/redirect-from", "/redirect-to")]
        public async Task Responds_With_Redirect(string url, string expectedRedirect)
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
            Assert.Equal(response.Headers.Location?.ToString(), expectedRedirect);
        }

        [Theory]
        [InlineData("/REDIRECT-from")]
        [InlineData("/redirect-from")]
        public async Task Does_Not_Respond_With_Redirect_If_Datasource_Not_Configured(string url)
        {
            using var scope = _factory.Services.CreateScope();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<StoryblokOptions>>();
            options.Value.RedirectsDatasourceName = null;

            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Theory]
        [InlineData("/SOME-URL")]
        public async Task Does_Not_Respond_With_Redirect(string url)
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}