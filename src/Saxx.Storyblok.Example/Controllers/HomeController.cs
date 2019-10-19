using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Saxx.Storyblok.Example.Controllers
{
    public class HomeController : Controller
    {
        private readonly StoryblokClient _storyblokClient;

        public HomeController(StoryblokClient storyblokClient)
        {
            _storyblokClient = storyblokClient;
        }

        [HttpGet("/stories")]
        public async Task<IActionResult> Stories()
        {
            var stories = await _storyblokClient.LoadStories("");
            return View(stories);
        }
    }
}