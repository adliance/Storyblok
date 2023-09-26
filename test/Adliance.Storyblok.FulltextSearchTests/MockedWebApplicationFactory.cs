using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Adliance.Storyblok.FulltextSearch.Tests
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class MockedWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            return WebHost.CreateDefaultBuilder();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSolutionRelativeContentRoot("");
            builder.UseStartup<MockedStartup>();
            base.ConfigureWebHost(builder);
        }
    }
}