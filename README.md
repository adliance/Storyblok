# Saxx.Storyblok

`Saxx.Storyblok` is a client to the [Storyblok](https://www.Storyblok.com) API written in C# that enables easy and strongly typed access to stories and components stored in the Storyblok headless CMS.

It also provides an ASP.NET Core middleware to directly render render stories into views based on their Storyblok slug.

[![Build Status](https://dev.azure.com/hannessachsenhofer/Saxx.Storyblok/_apis/build/status/saxx.Saxx.Storyblok?branchName=master)](https://dev.azure.com/hannessachsenhofer/Saxx.Storyblok/_build/latest?definitionId=1&branchName=master)
[![NuGet Badge](https://buildstats.info/nuget/Saxx.Storyblok)](https://www.nuget.org/packages/Saxx.Storyblok/)

## Features
- "Drop in" middleware to automatically render any story on Storyblok
- Use strongly typed representations of your stories and components in your C# code
- Automatic in-memory caching of fetched stories for faster response times
- Full support for internationalization based on Storybloks support for multiple languages
- Full support for the integrated preview in the Storyblok editor
- A set of "common components" (eg. markdown text field) that can be used optionally by using the `Saxx.Storyblok.Components` NuGet package

## Getting started
Add the Nuget package to your ASP.NET Core website:
```
dotnet add package Saxx.Storyblok
```

Then add the middleware to your `Startup.cs` by adding it to DI:

```csharp
services.AddStoryblok(options =>
{
    // here you can do some configuration
});
```
and the the pipeline:
```csharp
app.UseStoryblok();
```

A full `Startup.cs` could look like:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddStoryblok(options =>
    {
        options.ApiKeyPreview = "<a_storyblok_preview_key>";
        options.ApiKeyPublic = "<a_storyblok_public_key>";
        options.HandleRootWithSlug = "home";    
    });
    services.AddControllersWithViews();
}

public void Configure(IApplicationBuilder app)
{
    // these error pages are also stories on Storyblok
    app.UseExceptionHandler("/error/500");
    app.UseStatusCodePagesWithReExecute("/error/{0}");

    // the usual
    app.UseHsts();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    // add Storyblok before the MVC middleware, so we render the Storyblok story if it exists, or fall back to controller actions
    app.UseStoryblok();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapDefaultControllerRoute();
    });
}
```

You can also set the configuration options using the `appsettings.json` file:
```json
{
  "Storyblok": {
    "api_key_public": "<a_storyblok_public_key>",
    "api_key_preview": "<a_storyblok_preview_key>"
  }
}
```
Now if you create a story `this-is/my-story` in Storyblok and call the path `~/this-is/my-story` on your website, the story will automatically be fetched, rendered and returned as a view.

Please see the [example website](https://github.com/saxx/Saxx.Storyblok/tree/master/src/Saxx.Storyblok.Example) for a full live example of all features and configuration options.

## Add your components
Each component in Storyblok is represented by a C# class. Add the attribute `[StoryblokComponent]` to the class so that `Saxx.Storyblok` recognizes it.
The JSON response from Storyblok will be deserialized into this class, so use `[JsonProperty]` to specify the proper attribute names, for example:
```csharp
[StoryblokComponent("teaser")] // "teaser" is the name of the component in Storyblok
public class Teaser : StoryblokComponent
{
    [JsonProperty("headline")] public string Headline { get; set; } // the component has only one property, a simple text field called "headline" in Storyblok
}
```
This makes sure that all your components are strongly typed in your C# code.

Next, add a display template for your class to specify the HTML for your component.
For our example above, create a file `/Views/Shared/DisplayTemplates/Teaser.cshtml` which could look like as simple as:
```razor
@model Teaser

<h1>@Model.Headline</h1>
```

Now the middleware will automatically use this display template to render a specific component.

## Manually fetch stories
Of course, you don't have the use the middleware to fetch and render stories from Storyblok, you can use the C# API to download them manually:
```csharp
var myStory = await _storyblok.LoadStory(CultureInfo.CurrentUICulture, "/this-is/my-story");
// do whatever you like with your story, you have a strongly typed representation now
```
Don't forget to create C# classes to represent your components and decorate them with `[StoryblokComponent]`, so that the C# API knows how to deserialize the response from Storyblok.

For example, you could use the fetched Story as a ViewModel for your views and use `@Html.DisplayFor` to render it:
```razor
@Html.DisplayFor(x => Model.Content)
```

## Enable support for preview in Storyblok editor
Storyblok requires a little JavaScript to enable the integrated preview in its editor. Add this to you `_Layout.cshtml` to automatically add the required JavaScript to your website just before the closing `</body>` tag:
```razor
@Html.StoryblokEditorScript()
```
Please note that the the JavaScript will only be injected if you're actually in the Storyblok editor.

## More
Please see the [example website](https://github.com/saxx/Saxx.Storyblok/tree/master/src/Saxx.Storyblok.Example) for a full live example of all features and configuration options.
You can also check out a [live (hosted) version](Please see the [example website](https://github.com/saxx/Saxx.Storyblok/tree/master/src/Saxx.Storyblok.Example) for a full live example of all features and configuration options.
) of the example website.
