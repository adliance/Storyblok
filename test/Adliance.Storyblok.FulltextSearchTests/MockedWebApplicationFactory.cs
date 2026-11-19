using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Adliance.Storyblok.FulltextSearch.Tests;

// ReSharper disable once ClassNeverInstantiated.Global
public class MockedWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override IHostBuilder CreateHostBuilder()
    {
        // disable config file watchers: tests create many hosts and would otherwise exhaust the inotify limit on Linux
        return Host.CreateDefaultBuilder()
            .ConfigureHostConfiguration(c => c.AddInMemoryCollection([new("hostBuilder:reloadConfigOnChange", "false")]))
            .ConfigureWebHostDefaults(_ => { });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSolutionRelativeContentRoot("");
        builder.UseStartup<MockedStartup>();
        base.ConfigureWebHost(builder);
    }
}
