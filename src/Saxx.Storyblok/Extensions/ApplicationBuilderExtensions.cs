using Microsoft.AspNetCore.Builder;
using Saxx.Storyblok.Middleware;

namespace Saxx.Storyblok.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseStoryblok(this IApplicationBuilder app)
        {
            app.UseMiddleware<StoryblokMiddleware>();
            return app;
        }
    }
}
