using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Adliance.Storyblok.Extensions;

public static class HtmlHelperExtensions
{
    public static HtmlString StoryblokEditorScript(this IHtmlHelper htmlHelper)
    {
        var settings = htmlHelper.ViewContext.HttpContext.RequestServices.GetRequiredService<IOptions<StoryblokOptions>>().Value;

        if (htmlHelper.ViewContext.HttpContext.Request.Query.IsInStoryblokEditor(settings))
        {
            var html = $"<script src=\"//app.storyblok.com/f/storyblok-v2-latest.js\"></script>"
                       + "<script>const storyblokInstance = new StoryblokBridge(); storyblokInstance.on([\"change\", \"published\"], () => { window.location.reload(); });</script>";
            return new HtmlString(html);
        }

        return new HtmlString("");
    }
}
