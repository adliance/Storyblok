using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Saxx.Storyblok.Components;

namespace Saxx.Storyblok.Example.ViewModels.Shared
{
    public class HeaderViewModel
    {
        public HeaderViewModel(StoryblokClient storyblokClient)
        {
            var minimalStories = storyblokClient.LoadStories("", "body,title,description,keywords").GetAwaiter().GetResult();

            var storiesForNavigation = new List<NavigationItem>();
            foreach (var minimalStory in minimalStories)
            {
                if (minimalStory.Content is Page page)
                {
                    if (!string.IsNullOrWhiteSpace(page.MenuTitle))
                    {
                        var story = storyblokClient.LoadStory(CultureInfo.CurrentUICulture, minimalStory.FullSlug).GetAwaiter().GetResult();
                        
                        storiesForNavigation.Add(new NavigationItem
                        {
                            FullSlug = story.FullSlug,
                            Title = ((Page)story.Content).MenuTitle,
                            Order = ((Page)story.Content).MenuOrder ?? 0
                        });
                    }
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
