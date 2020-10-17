using System;
using Adliance.Storyblok.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adliance.Storyblok.Tests
{
    public class MockedStartup
    {
        private readonly IConfiguration _configuration;

        public MockedStartup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddStoryblok(o =>
            {
                o.ApiKeyPublic = Environment.GetEnvironmentVariable("Adliance_Storyblok_Tests__ApiKeyPublic");
                o.SupportedCultures = new[] {"de", "en"};
            });
            
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseStoryblok();
        }
    }
}