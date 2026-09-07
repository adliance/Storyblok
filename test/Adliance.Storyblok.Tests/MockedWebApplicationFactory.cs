using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Adliance.Storyblok.Tests;

// ReSharper disable once ClassNeverInstantiated.Global
public class MockedWebApplicationFactory<TStartup>(bool mockHttpClient = false) : WebApplicationFactory<TStartup> where TStartup : class
{
    protected override IWebHostBuilder CreateWebHostBuilder()
    {
        return WebHost.CreateDefaultBuilder();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSolutionRelativeContentRoot("");
        builder.UseStartup<MockedStartup>();
        if (mockHttpClient)
        {
            // ConfigureTestServices runs after Startup.ConfigureServices, so this wins.
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<MockHttpMessageHandler>();

                // The Storyblok clients use clientFactory.CreateClient(), i.e. the client
                // with the default (empty) name - so that's the one we swap the handler on.
                services.AddHttpClient(Options.DefaultName)
                    .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<MockHttpMessageHandler>());
            });
        }
        base.ConfigureWebHost(builder);
    }
}
