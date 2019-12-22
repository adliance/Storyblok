using System;
using System.Globalization;
using System.Text.Json;

namespace Saxx.Storyblok.Converters
{
    public class StoryblokIntConverter : System.Text.Json.Serialization.JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt32();
            }
            
            var s = reader.GetString();
            if (int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i))
            {
                return i;
            }

            return default;
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }

    public class StoryblokNullableIntConverter : System.Text.Json.Serialization.JsonConverter<int?>
    {
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt32();
            }
            
            var s = reader.GetString();
            if (int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i))
            {
                return i;
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}