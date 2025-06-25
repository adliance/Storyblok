using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Adliance.Storyblok.Tests.Clients;

public class StoryblokStoryClientTest
{
    private readonly StoryblokStoryClient _client;

    public StoryblokStoryClientTest()
    {
        MockedWebApplicationFactory<MockedStartup> factory = new();
        factory.CreateClient();
        _client = factory.Services.GetRequiredService<StoryblokStoryClient>();
    }

    [Fact]
    public async Task Can_Load_Story_With_Resolved_Links()
    {
        var story = await _client.Story().WithSlug("/en/page-sections-buttons").ResolveLinks(ResolveLinksType.Url).Load<PageComponent>();
        var section = story!.Content!.Content!.First() as SectionComponent;
        var grid = section!.Content!.First() as Grid1x1Component;
        var button = grid!.Right!.First() as ButtonComponent;
        Assert.NotNull(button);
        Assert.NotNull(button.Link?.Story);
        Assert.NotEqual(button.Link?.CachedValue, button.Link?.Story?.FullSlug);
        Assert.Equal(button.Link?.Url, "/" + button.Link?.Story?.FullSlug);
    }
    
    [Fact]
    public async Task Can_Load_Page_With_New_Much_Larger_Ids()
    {
        var story = await _client.Story().WithSlug("/page-with-section").Load<PageComponent>();
        Assert.NotNull(story);
        var section = story.Content!.Content!.First() as SectionComponent;
        Assert.NotNull(section);
        var button = section.Content!.First() as ButtonComponent;
        Assert.NotNull(button);
    }

    [Fact]
    public async Task Can_Load_Story_With_Resolved_Stories()
    {
        var story = await _client.Story().WithSlug("/en/page-sections-buttons").ResolveLinks(ResolveLinksType.Story).Load<PageComponent>();
        var section = story!.Content!.Content!.First() as SectionComponent;
        var grid = section!.Content!.First() as Grid1x1Component;
        var button = grid!.Right!.First() as ButtonComponent;
        Assert.NotNull(button);
        Assert.NotNull(button.Link!.Story);
    }

    [Fact]
    public async Task Can_Load_Story_Without_Resolved_Links()
    {
        var story = await _client.Story().WithSlug("/en/page-sections-buttons").ResolveLinks(ResolveLinksType.None).Load<PageComponent>();
        var section = story!.Content!.Content!.First() as SectionComponent;
        var grid = section!.Content!.First() as Grid1x1Component;
        var button = grid!.Right!.First() as ButtonComponent;
        Assert.NotNull(button);
        Assert.Null(button.Link!.Story);
    }

    [Fact]
    public async Task Can_Load_Story_Without_Resolved_Relations()
    {
        var story = await _client.Story().WithSlug("/page-relation").Load<PageComponent>();
        var reference = story?.Content?.Content?.FirstOrDefault() as ComponentReference;
        Assert.NotNull(reference);
        Assert.NotEqual(Guid.Empty, reference.ReferencedComponent!.First().Uuid);
        Assert.Empty(reference.ReferencedComponent!.First().Component);
    }

    [Fact]
    public async Task Can_Load_Story_With_Resolved_Relations()
    {
        var story = await _client.Story().WithSlug("/page-relation").ResolveRelations("component_reference.referenced_component").Load<PageComponent>();
        var reference = story?.Content?.Content?.FirstOrDefault() as ComponentReference;
        Assert.NotEqual(Guid.Empty, reference!.ReferencedComponent!.First().Uuid);
        Assert.NotEmpty(reference.ReferencedComponent!.First().Component);
    }

    [Fact]
    public async Task Can_Load_Table()
    {
        var story = await _client.Story().WithSlug("/page-table").ResolveLinks(ResolveLinksType.None).Load<PageComponent>();
        var table = story!.Content!.Content!.First() as TableComponent;
        Assert.NotNull(table);
        Assert.Equal(3, table.Table?.Header.Length);
        Assert.Equal("Header 2", table.Table?.Header[1].Value);
        Assert.Equal(2, table.Table?.Body.Length);
        Assert.Equal(3, table.Table?.Body[0].Columns.Length);
        Assert.Equal(3, table.Table?.Body[1].Columns.Length);
        Assert.Equal("", table.Table?.Body[0].Columns[1].Value);
        Assert.Equal("Inhalt D", table.Table?.Body[1].Columns[1].Value);
    }

    [Fact]
    public async Task Can_Load_Table_Translated()
    {
        var story = await _client.Story().WithSlug("/page-table").WithCulture(new CultureInfo("en")).ResolveLinks(ResolveLinksType.None).Load<PageComponent>();
        var table = story!.Content!.Content!.First() as TableComponent;
        Assert.NotNull(table);
        Assert.Equal(2, table.Table?.Header.Length);
        Assert.Equal("Content A", table.Table?.Header[0].Value);
        Assert.Equal("", table.Table?.Header[1].Value);
        Assert.Equal(1, table.Table?.Body.Length);
        Assert.Equal(2, table.Table?.Body[0].Columns.Length);
        Assert.Equal("Content B", table.Table?.Body[0].Columns[0].Value);
        Assert.Equal("Content C", table.Table?.Body[0].Columns[1].Value);
    }

    [Fact]
    public async Task Story_Contains_Translated_Slug()
    {
        var story = await _client.Story().WithSlug("/page-translated-slug").Load<PageComponent>();
        Assert.NotNull(story);
        Assert.NotEmpty(story.TranslatedSlugs);
    }

    [Fact]
    public async Task Story_Contains_Default_Full_Slug()
    {
        var story = await _client.Story().WithSlug("/en/page-translated-english-slug").Load<PageComponent>();
        Assert.NotNull(story);
        Assert.Equal("page-translated-slug", story.DefaultFullSlug);
    }
}
