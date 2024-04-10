using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Adliance.Storyblok.Converters;

public class StoryblokReferencedComponentConverter<T> : JsonConverter<StoryblokComponent[]?> where T : StoryblokReferencedComponentContainer
{
    public override StoryblokComponent[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var componentUid = reader.GetGuid();
            return
            [
                new StoryblokComponent
                {
                    Uuid = componentUid
                }
            ];
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var converter = (JsonConverter<StoryblokStory>)options.GetConverter(typeof(StoryblokStory));
            var story = converter.Read(ref reader, typeof(StoryblokStory), options);
            if (story == null) return null;
            var typedStory = new StoryblokStory<T>(story);
            return typedStory.Content?.ContainedComponents;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, StoryblokComponent[]? value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
