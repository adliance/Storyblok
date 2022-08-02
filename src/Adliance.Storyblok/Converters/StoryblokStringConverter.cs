using System;
using System.Globalization;
using System.Text.Json;

namespace Adliance.Storyblok.Converters
{
    public class StoryblokStringConverter : System.Text.Json.Serialization.JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return "";
            if (reader.TokenType == JsonTokenType.Number) return reader.GetInt32().ToString(CultureInfo.InvariantCulture);
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read(); // read start array
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return "";
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    var arrayStringValue = reader.GetString();
                    return arrayStringValue ?? "";
                }
                
                throw new Exception("Unable to deserialize array into string.");
            }

            var stringValue = reader.GetString();
            return stringValue ?? "";
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
    
    public class StoryblokNullableStringConverter : System.Text.Json.Serialization.JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            if (reader.TokenType == JsonTokenType.Number) return reader.GetInt32().ToString(CultureInfo.InvariantCulture);
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read(); // read start array
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    reader.Read(); // read end array
                    return null;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    var arrayStringValue = reader.GetString();
                    reader.Read(); // read end array
                    return arrayStringValue;
                }

                reader.Read(); // read start array
                throw new Exception("Unable to deserialize array into string.");
            }

            var stringValue = reader.GetString();
            return stringValue;
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}