using System.Linq;
using System.Threading.Tasks;
using Adliance.Storyblok.Clients;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Adliance.Storyblok.Tests.Converters
{
    public class SingleOptionTest
    {
        private readonly StoryblokStoryClient _client;

        public SingleOptionTest()
        {
            MockedWebApplicationFactory<MockedStartup> factory = new();
            factory.CreateClient();
            _client = factory.Services.GetRequiredService<StoryblokStoryClient>();
        }

        /// <summary>
        /// Tests a page that contains several fields with type "single option"
        /// and tests if the selected value is returned correctly
        /// </summary>
        [Fact]
        public async Task Can_Load_Component_With_SingleOption_Field()
        {
            var story = await _client.Story().WithSlug("/page-with-single-option").Load<PageComponent>();
            // we have two images on the page - the first image has a file assigned, the second one does not
            var firstDropdown = story?.Content?.Content?.First() as DropdownComponent;
            Assert.Equal("Value B", firstDropdown?.SingleOptionSelf);
            Assert.Equal("A", firstDropdown?.SingleOptionDatasource);
            var secondDropdown = story?.Content?.Content?.Skip(1).First() as DropdownComponent;
            Assert.Equal("", secondDropdown?.SingleOptionSelf);
            Assert.Equal("A", firstDropdown?.SingleOptionDatasource);
            var thirdDropdown = story?.Content?.Content?.Skip(2).First() as DropdownComponent;
            Assert.Equal("Value B", thirdDropdown?.SingleOptionSelf);
            Assert.Equal("A", firstDropdown?.SingleOptionDatasource);
        }
    }
}