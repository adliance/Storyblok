using System.Globalization;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;

namespace Adliance.Storyblok;

public class StoryblokStoryQuery(StoryblokStoryClient client)
{
    private CultureInfo? _culture;
    private string _slug = "";
    private ResolveLinksType _resolveLinks = ResolveLinksType.Url;
    private bool _resolveAssets;
    private string _resolveRelations = "";

    public StoryblokStoryQuery WithSlug(string slug)
    {
        _slug = slug;
        return this;
    }

    public StoryblokStoryQuery WithCulture(CultureInfo culture)
    {
        _culture = culture;
        return this;
    }

    public StoryblokStoryQuery ResolveLinks(ResolveLinksType type)
    {
        _resolveLinks = type;
        return this;
    }

    public StoryblokStoryQuery ResolveAssets(bool resolveAssets = true)
    {
        _resolveAssets = resolveAssets;
        return this;
    }

    public StoryblokStoryQuery ResolveRelations(string relations)
    {
        _resolveRelations = relations;
        return this;
    }

    public async Task<StoryblokStory<T>?> Load<T>() where T : StoryblokComponent
    {
        return await client.LoadStory<T>(_culture, _slug, _resolveLinks, _resolveAssets, _resolveRelations);
    }

    // ReSharper disable once UnusedMember.Global
    public async Task<StoryblokStory?> Load()
    {
        return await client.LoadStory(_culture, _slug, _resolveLinks, _resolveAssets, _resolveRelations);
    }
}

public enum ResolveLinksType
{
    Url,
    Story,
    None
}

public enum ResolveAssetsType
{
    DontResolve,
    Resolve
}
