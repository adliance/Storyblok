using System;
using Newtonsoft.Json;

namespace Saxx.Storyblok
{
    [JsonConverter(typeof(StoryblokComponentConverter))]
    public class StoryblokComponent 
    {
        [JsonProperty("_uid")] public Guid Uuid { get; set; }
        [JsonProperty("component")] public string Component { get; set; }
    }
}

