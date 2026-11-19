# Adliance.Storyblok

`Adliance.Storyblok` is a client to the [Storyblok](https://www.Storyblok.com) API written in C# that enables easy and strongly typed access to stories and components stored in the Storyblok headless CMS.

It also provides an ASP.NET Core middleware to directly render stories into views based on their Storyblok slug.

[![Build Status](https://dev.azure.com/adliance/Open%20Source%20Projects/_apis/build/status/Storyblok?branchName=master)](https://dev.azure.com/adliance/Open%20Source%20Projects/_build/latest?definitionId=97&branchName=master)
[![NuGet Version](https://img.shields.io/nuget/v/Adliance.Storyblok)](https://www.nuget.org/packages/Adliance.Storyblok/)

## Configuration

Register the services with `services.AddStoryblok(configuration.GetSection("Storyblok"))` and the middleware with `app.UseStoryblok()`. Example `appsettings.json`:

```json
{
  "Storyblok": {
    "ApiKeyPublic": "<public API key>",
    "ApiKeyPreview": "<preview API key>",
    "SupportedCultures": [ "de", "en" ],
    "CacheDurationSeconds": 900,
    "SlugForClearingCache": "/clear-storyblok-cache",
    "ClearCacheAllowedIpAddresses": [],
    "UseStoryIndexForNotFound": true
  }
}
```

- `SlugForClearingCache`: calling this URL clears all cached content. Configure it as [Storyblok webhook](https://www.storyblok.com/docs/concepts/webhooks) for publishing, unpublishing, deleting and moving stories.
- `ClearCacheAllowedIpAddresses`: optional IP addresses or CIDR networks (e.g. `3.121.25.244/24`) that may call `SlugForClearingCache`. Empty (default) allows all IP addresses. Behind a reverse proxy, configure the forwarded headers middleware so the actual client IP address is used. The list of the IP addresses in use by Storyblok can be found on their [website](https://www.storyblok.com/docs/concepts/webhooks#allow-storyblok-static-ip-addresses).
- `UseStoryIndexForNotFound`: uses the cached list of all published stories (the same list used for the sitemap) to answer requests for non-existing slugs without calling the Storyblok API. This keeps bots and crawlers requesting random URLs away from the Storyblok API and its rate limits. The index is never used in the Storyblok editor or with `IncludeDraftStories`. The index is always loaded in the background, so requests never wait for it: until it is available (on startup or after clearing the cache), the Storyblok API is used; once it is older than `CacheDurationSeconds`, it is still used while the new one is loaded. Requires the [Storyblok webhook](https://www.storyblok.com/docs/concepts/webhooks) to `SlugForClearingCache`, otherwise newly published stories are not found until the cache expires.

## Adliance.Storyblok.FulltextSearch

Extends the `Adliance.Storyblok` package with support for fulltext search.

[![NuGet Version](https://img.shields.io/nuget/v/Adliance.Storyblok.FulltextSearch)](https://www.nuget.org/packages/Adliance.Storyblok/)
