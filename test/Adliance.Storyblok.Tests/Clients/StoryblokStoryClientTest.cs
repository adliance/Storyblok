using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Adliance.Storyblok.Tests.Clients
{
    public class StoryblokStoryClientTest
    {
        private readonly MockedWebApplicationFactory<MockedStartup> _factory;
        private readonly StoryblokStoryClient _client;

        public StoryblokStoryClientTest()
        {
            _factory = new MockedWebApplicationFactory<MockedStartup>();
            _factory.CreateClient();
            _client = _factory.Services.GetRequiredService<StoryblokStoryClient>();
        }

        [Fact]
        public async Task Can_Load_Story_With_Resolved_Links()
        {
            var story = await _client.Story().WithSlug("/en/metabolism").ResolveLinks(ResolveLinksType.Url).Load<PageComponent>();
            var buttons = Find<ButtonComponent>(story);

            Assert.NotEmpty(buttons);
            Assert.All(buttons, x => Assert.NotNull(x.Link?.Story));
            Assert.All(buttons, x => Assert.NotEqual(x.Link?.CachedValue, x.Link?.Story?.FullSlug));
            Assert.All(buttons, x => Assert.Equal(x.Link?.Url, "/" + x.Link?.Story?.FullSlug));
        }
        
        [Fact]
        public async Task Can_Load_Story_With_Resolved_Stories()
        {
            var story = await _client.Story().WithSlug("/en/metabolism").ResolveLinks(ResolveLinksType.Story).Load<PageComponent>();
            var buttons = Find<ButtonComponent>(story);

            Assert.NotEmpty(buttons);
            Assert.All(buttons, x => Assert.NotNull(x.Link?.Story));
        }
        
        [Fact]
        public async Task Can_Load_Story_Without_Resolved_Links()
        {
            var story = await _client.Story().WithSlug("/en/metabolism").ResolveLinks(ResolveLinksType.None).Load<PageComponent>();
            var buttons = Find<ButtonComponent>(story);

            Assert.NotEmpty(buttons);
            Assert.All(buttons, x => Assert.Null(x.Link?.Story));
        }

        private IList<T> Find<T>(StoryblokStory<PageComponent>? story)
        {
            var result = new List<T>();

            foreach (var c1 in story?.Content?.Content ?? Enumerable.Empty<StoryblokComponent>())
            {
                if (c1 is SectionComponent s)
                {
                    foreach (var c2 in s.Content ?? Enumerable.Empty<StoryblokComponent>())
                    {
                        if (c2 is T b1)
                        {
                            result.Add(b1);
                        }
                        
                        if (c2 is Grid1x1Component g)
                        {
                            foreach (var c3 in g.Left ?? Enumerable.Empty<StoryblokComponent>())
                            {
                                if (c3 is T b2)
                                {
                                    result.Add(b2);
                                }
                            }
                            foreach (var c3 in g.Right ?? Enumerable.Empty<StoryblokComponent>())
                            {
                                if (c3 is T b2)
                                {
                                    result.Add(b2);
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}