using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Saxx.Storyblok
{
    public class StoryblokComponentConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonToken = JToken.ReadFrom(reader);

            var componentName = (string) jsonToken["component"];
            var componentMappings = StoryblokMappings.Mappings;

            if (componentMappings.ContainsKey(componentName))
            {
                var type = componentMappings[componentName];
                var concreteComponent = Activator.CreateInstance(type);
                serializer.Populate(jsonToken.CreateReader(), concreteComponent);
                return concreteComponent;
            }

            var fallbackComponent = new StoryblokComponent();
            serializer.Populate(jsonToken.CreateReader(), fallbackComponent);
            return fallbackComponent;
        }
    }
}