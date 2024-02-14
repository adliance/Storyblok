using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Adliance.Storyblok.Tests.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Adliance.Storyblok.Tests.Middleware
{
    public class StoryblokMiddlewareTest
    {
        private readonly TestServer _noExistingLocalisationApp;
        private readonly TestServer _existingLocalisationApp;

        public StoryblokMiddlewareTest()
        {
            Thread.DontBombardStoryblokApi();
            _noExistingLocalisationApp = new TestServer(new WebHostBuilder().UseStartup<MockedStartup>());
            _existingLocalisationApp = new TestServer(new WebHostBuilder().UseStartup<LocalisedMockedStartup>());
        }

        [Theory]
        [InlineData("/page-sections-buttons")]
        public async Task LocalisationSetsCultureBasedOnDefault(string url)
        {
            var context = await _existingLocalisationApp.SendAsync(c =>
            {
                c.Request.Method = HttpMethods.Get;
                c.Request.Path = url;
            });

            var rqf = context.Features.Get<IRequestCultureFeature>();
            Assert.Equal("de", rqf?.RequestCulture.UICulture.Name);
        }

        [Theory]
        [InlineData("/mi-NZ/page-sections-buttons")]
        public async Task LocalisationSetsCultureBasedOnSlug(string url)
        {
            var context = await _existingLocalisationApp.SendAsync(c =>
            {
                c.Request.Method = HttpMethods.Get;
                c.Request.Path = url;
            });

            var rqf = context.Features.Get<IRequestCultureFeature>();
            Assert.Equal("mi-NZ", rqf?.RequestCulture.UICulture.Name);
        }

        [Theory]
        [InlineData("/en/page-sections-buttons")]
        public async Task LocalisationSetsCultureBasedOnSlugHasPriority(string url)
        {
            var context = await _existingLocalisationApp.SendAsync(c =>
            {
                c.Request.Method = HttpMethods.Get;
                c.Request.Path = url;
                c.Request.QueryString = new QueryString("?ui-culture=de");
            });

            var rqf = context.Features.Get<IRequestCultureFeature>();
            Assert.Equal("en", rqf?.RequestCulture.UICulture.Name);
        }

        [Theory]
        [InlineData("/de/page-sections-buttons")]
        public async Task NoExistingLocalisationSetsCultureBasedOnSlug(string url)
        {
            var context = await _noExistingLocalisationApp.SendAsync(c =>
            {
                c.Request.Method = HttpMethods.Get;
                c.Request.Path = url;
            });

            var rqf = context.Features.Get<IRequestCultureFeature>();
            Assert.Equal("de", rqf?.RequestCulture.UICulture.Name);
        }

        [Theory]
        [InlineData("/de/page-sections-buttons")]
        public async Task NoExistingLocalisationSetsCultureBasedOnSlugHasPriority(string url)
        {
            var context = await _noExistingLocalisationApp.SendAsync(c =>
            {
                c.Request.Method = HttpMethods.Get;
                c.Request.Path = url;
                c.Request.QueryString = new QueryString("?ui-culture=en");
            });

            var rqf = context.Features.Get<IRequestCultureFeature>();
            Assert.Equal("de", rqf?.RequestCulture.UICulture.Name);
        }

        [Theory]
        [InlineData("/page-sections-buttons")]
        public async Task NoExistingLocalisationSetsCultureBasedOnQuery(string url)
        {
            var context = await _noExistingLocalisationApp.SendAsync(c =>
            {
                c.Request.Method = HttpMethods.Get;
                c.Request.Path = url;
                c.Request.QueryString = new QueryString("?ui-culture=de");
            });

            var rqf = context.Features.Get<IRequestCultureFeature>();
            Assert.Equal("de", rqf?.RequestCulture.UICulture.Name);
        }

    }
}