using System;
using System.Text.Json;

namespace Adliance.Storyblok.Converters
{
    public class StoryblokNullableConverter<T> : System.Text.Json.Serialization.JsonConverter<T> where T:class
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return null;
            }
            
            //TODO: see how we can escape the call loop -> removing and adding the converter once launched isn't possible :(
            options.Converters.Remove(this);

            var result = JsonSerializer.Deserialize<T>(ref reader, options);
            
            options.Converters.Add(this);

            return result;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}