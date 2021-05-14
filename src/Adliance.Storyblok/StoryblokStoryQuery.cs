using System.Globalization;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;

namespace Adliance.Storyblok
{
    public class StoryblokStoryQuery
    {
        private readonly StoryblokStoryClient _client;

        private CultureInfo? _culture;
        private string _slug = "";
        private ResolveLinksType _resolveLinks = ResolveLinksType.Url;
        private bool _resolveAssets;

        public StoryblokStoryQuery(StoryblokStoryClient client)
        {
            _client = client;
        }

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

        public async Task<StoryblokStory<T>?> Load<T>() where T : StoryblokComponent
        {
            return await _client.LoadStory<T>(_culture, _slug, _resolveLinks, _resolveAssets);
        }

        // ReSharper disable once UnusedMember.Global
        public async Task<StoryblokStory?> Load()
        {
            return await _client.LoadStory(_culture, _slug, _resolveLinks, _resolveAssets);
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
}