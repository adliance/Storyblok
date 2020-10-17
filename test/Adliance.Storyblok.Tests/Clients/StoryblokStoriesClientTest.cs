using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Adliance.Storyblok.Tests.Clients
{
    public class StoryblokStoriesClientTest
    {
        [Fact]
        public async Task Can_Load_Stories()
        {
            var client = TestUtils.GetStoriesClient();

            var stories = await client.Stories().Load<StoryblokComponent>();
            Assert.NotNull(stories);
            Assert.True(stories.Count > 130);
        }

        [Fact]
        public async Task Can_Handle_Paged_Stories()
        {
            var client = TestUtils.GetStoriesClient();
            client.PerPage = 10;
            
            var stories = await client.Stories().Load<StoryblokComponent>();
            Assert.NotNull(stories);
            Assert.True(stories.Count > 130);
        }
    }
}