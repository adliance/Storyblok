# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

C# (.NET 10) client for the Storyblok headless CMS, shipped as two NuGet packages:

- `src/Adliance.Storyblok` – strongly typed API clients plus ASP.NET Core middleware that renders stories directly into Razor views based on the request slug.
- `src/Adliance.Storyblok.FulltextSearch` – Lucene.Net-based fulltext index over Storyblok stories. **It references the published `Adliance.Storyblok` NuGet package (not the project)**, so changes in the core library are not visible to it until a new package version is published and the package reference is bumped.

All projects use `Nullable` enabled and `TreatWarningsAsErrors=true` – any compiler warning breaks the build.

## Commands

```bash
dotnet build Adliance.Storyblok.slnx
dotnet test test/Adliance.Storyblok.Tests/Adliance.Storyblok.Tests.csproj
dotnet test test/Adliance.Storyblok.FulltextSearchTests/Adliance.Storyblok.FulltextSearch.Tests.csproj

# single test / single class
dotnet test test/Adliance.Storyblok.Tests --filter "FullyQualifiedName~StoryblokStoryClientTest.Can_Load_Page_With_New_Much_Larger_Ids"
dotnet test test/Adliance.Storyblok.Tests --filter "FullyQualifiedName~StoryblokMiddlewareTest"
```

CI runs on Azure DevOps (`Adliance.Storyblok.yml`, `Adliance.Storyblok.FulltextSearch.yml`): build, test, pack with version `10.0.$(rev:r)`, and push to nuget.org on `master`. Each pipeline only triggers on changes under its own `src/` folder.

## Tests

- Tests are integration tests against a **real Storyblok test space** (public API key and asset key are hard-coded in `MockedStartup.cs`; they are intentionally non-confidential). They need network access and depend on content in that space (e.g. slugs like `page-sections-buttons`, the `redirects` datasource).
- Test parallelization is disabled (`MockedWebApplicationFactory.cs`), and several test classes call `Thread.DontBombardStoryblokApi()` (a 10 s sleep) to avoid Storyblok rate limits – test runs are slow.
- Test hosts are built via `MockedWebApplicationFactory<MockedStartup>` / `TestServer` with `MockedStartup` or `LocalisedMockedStartup` (the latter registers `RequestLocalizationOptions`).
- `MockedWebApplicationFactory(mockHttpClient: true)` swaps the default `HttpClient` handler for `MockHttpMessageHandler`, which records request URLs and always returns 404 – use it to assert that no (or which) Storyblok requests are made, e.g. for caching behavior.
- Component classes used by tests live in `test/*/Components.cs`.

## Architecture

**Component mapping.** Content types are POCOs deriving from `StoryblokComponent` and decorated with `[StoryblokComponent("name", "ViewName")]`. `StoryblokMappings` scans *all loaded assemblies* via reflection once and caches the name → (Type, View) map statically. `StoryblokComponentConverter` uses that map for polymorphic deserialization of nested `component` JSON objects; unknown components fall back to a bare `StoryblokComponent`. Other converters in `Converters/` handle Storyblok's loose typing (string/int/date/markdown/asset/enum). The full converter set is assembled in `StoryblokBaseClient.JsonOptions`.

**Clients** (`Clients/`, all scoped, registered by `AddStoryblok()`): `StoryblokStoryClient` (single story, fluent `Story().WithSlug().WithCulture()...Load()` via `StoryblokStoryQuery`), `StoryblokStoriesClient` (lists, `StoryblokStoriesQuery`), `StoryblokDatasourceClient`, `StoryblokAssetClient`. They share `StoryblokBaseClient`, which validates `StoryblokOptions` in the constructor and picks the preview vs. public API key.

- Editor mode: `StoryblokBaseClient.IsInEditor` is a **static** flag set from the current request's query string (`_storyblok_tk[...]` params, validated with a SHA1 hash of space id, preview key and timestamp; see `QueryCollectionExtensions.IsInStoryblokEditor`). In editor mode caching is bypassed, draft versions are loaded, and components keep their `Editable` attribute (otherwise it is nulled out).
- Caching: results (including 404s, under a `404_` prefixed key) are cached in `IMemoryCache` for `CacheDurationSeconds`. `StoryblokClearCacheMiddleware` (mapped to `SlugForClearingCache`, intended as Storyblok webhook target) clears the cache.
- Cultures: `SupportedCultures[0]` is the default culture. Non-default cultures are requested as `/stories/{culture}/{slug}`; a full culture like `en-US` falls back to `en` if only that is configured.

**Middleware pipeline** (`ApplicationBuilderExtensions.UseStoryblok()`), in order:
1. `StoryblokRedirectsMiddleware` – only if `RedirectsDatasourceName` is set; redirects come from a Storyblok datasource.
2. Inserts two request culture providers at the front: `_storyblok_lang` query param (editor language) and culture-prefixed paths (`/de/...`). Throws if `SupportedCultures` is set but `RequestLocalizationOptions` isn't registered.
3. `/sitemap.xml` → `StoryblokSitemapMiddleware` (`SitemapBuilder`, filterable via `SitemapFilter`).
4. `SlugForClearingCache` → `StoryblokClearCacheMiddleware`.
5. `UseRequestLocalization()`, then `StoryblokMiddleware`: for GET requests, maps `/` to `HandleRootWithSlug`, strips a culture prefix, skips `IgnoreSlugs` (exact, `prefix/*`, or `*suffix` patterns), loads the story and renders the mapped view with a `StoryblokStory<T>` model by executing a `ViewResult` manually. If no story / no view is found it falls through to `next`.

**Fulltext search.** Consumers subclass `FulltextSearchBase` (override `HandleStory`/title/content/role extraction) and register it with `AddStoryblokFulltextSearch<T>()`. `FulltextSearchBackgroundService` periodically rebuilds the per-culture Lucene index (via `LuceneService`) when stories were published after the index date.
