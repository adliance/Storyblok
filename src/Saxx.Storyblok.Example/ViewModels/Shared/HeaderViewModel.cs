using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Saxx.Storyblok.Clients;
using Saxx.Storyblok.Example.Components;

namespace Saxx.Storyblok.Example.ViewModels.Shared
{
    public class HeaderViewModel
    {
        public HeaderViewModel(StoryblokStoriesClient storiesClient, StoryblokStoryClient storyClient)
        {
            var pages = storiesClient.Stories()
                .StartingWith("")
                .ExcludingFields("body", "title", "description", "keywords")
                .Having("menu_title", FilterOperation.NotIn, "")
                .Load<PageComponent>().GetAwaiter().GetResult();

            var storiesForNavigation = new List<NavigationItem>();
            foreach (var minimalStory in pages)
            {
                if (!string.IsNullOrWhiteSpace(minimalStory.Content?.MenuTitle))
                {
                    var story = storyClient.Story().WithCulture(CultureInfo.CurrentUICulture).WithSlug(minimalStory.FullSlug).Load().GetAwaiter().GetResult();

                    storiesForNavigation.Add(new NavigationItem
                    {
                        FullSlug = story.FullSlug,
                        Title = ((PageComponent) story.Content).MenuTitle,
                        Order = ((PageComponent) story.Content).MenuOrder
                    });
                }
            }

            StoriesForNavigation = storiesForNavigation.OrderBy(x => x.Order);
        }

        public IEnumerable<NavigationItem> StoriesForNavigation { get; }

        public class NavigationItem
        {
            public string FullSlug { get; set; }
            public string Title { get; set; }
            public int Order { get; set; }
        }
    }
}