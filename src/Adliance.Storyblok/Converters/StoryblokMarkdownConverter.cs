using System;
using System.Text.Json;

namespace Adliance.Storyblok.Converters
{
    public class StoryblokMarkdownConverter : System.Text.Json.Serialization.JsonConverter<Markdown>
    {
        public override Markdown Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();
            return new Markdown
            {
                Value = s
            };
        }

        public override void Write(Utf8JsonWriter writer, Markdown value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}