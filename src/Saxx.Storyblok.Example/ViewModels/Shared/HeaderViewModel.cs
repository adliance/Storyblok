using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Saxx.Storyblok.Example.Components;

namespace Saxx.Storyblok.Example.ViewModels.Shared
{
    public class HeaderViewModel
    {
        public HeaderViewModel(StoryblokClient storyblokClient)
        {
            var pages = storyblokClient.Stories()
                .StartingWith("")
                .ExcludingFields("body", "title", "description", "keywords")
                .Having("menu_title", FilterOperation.NotIn, "")
                .Load<Page>().GetAwaiter().GetResult();

            var storiesForNavigation = new List<NavigationItem>();
            foreach (var minimalStory in pages)
            {
                if (!string.IsNullOrWhiteSpace(minimalStory.Content.MenuTitle))
                {
                    var story = storyblokClient.Story().WithCulture(CultureInfo.CurrentUICulture).WithSlug(minimalStory.FullSlug).Load().GetAwaiter().GetResult();

                    storiesForNavigation.Add(new NavigationItem
                    {
                        FullSlug = story.FullSlug,
                        Title = ((Page) story.Content).MenuTitle,
                        Order = ((Page) story.Content).MenuOrder
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