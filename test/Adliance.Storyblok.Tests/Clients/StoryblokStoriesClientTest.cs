using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Adliance.Storyblok.Tests.Clients
{
    public class StoryblokStoriesClientTest
    {
        private readonly MockedWebApplicationFactory<MockedStartup> _factory;

        public StoryblokStoriesClientTest()
        {
            _factory = new MockedWebApplicationFactory<MockedStartup>();
        }
        
        [Fact]
        public async Task Can_Load_Stories()
        {
            _factory.CreateClient();
            var client = _factory.Services.GetRequiredService<StoryblokStoriesClient>();
            
            var stories = await client.Stories().Load<StoryblokComponent>();
            Assert.NotNull(stories);
            Assert.True(stories.Count > 130);
        }

        [Fact]
        public async Task Can_Handle_Paged_Stories()
        {
            _factory.CreateClient();
            var client = _factory.Services.GetRequiredService<StoryblokStoriesClient>();
            client.PerPage = 10;
            
            var stories = await client.Stories().Load<StoryblokComponent>();
            Assert.NotNull(stories);
            Assert.True(stories.Count > 130);
        }
    }
}