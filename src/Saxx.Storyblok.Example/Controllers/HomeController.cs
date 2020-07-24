using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Saxx.Storyblok.Clients;

namespace Saxx.Storyblok.Example.Controllers
{
    public class HomeController : Controller
    {
        private readonly StoryblokStoriesClient _storyblokClient;

        public HomeController(StoryblokStoriesClient storyblokClient)
        {
            _storyblokClient = storyblokClient;
        }

        [HttpGet("/stories")]
        public async Task<IActionResult> Stories()
        {
            var stories = await _storyblokClient.Stories().StartingWith("").Load();
            return View(stories);
        }
    }
}