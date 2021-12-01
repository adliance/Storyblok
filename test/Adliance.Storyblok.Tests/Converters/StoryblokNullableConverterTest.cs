using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Adliance.Storyblok.Converters;
using Xunit;

namespace Adliance.Storyblok.Tests.Converters
{
    public class StoryblokNullableConverterTest
    {
        [Fact]
        public void Can_Deserialize_Empty_String()
        {
            var json = "{\"Element\": \"\"}";

            var result = JsonSerializer.Deserialize<Container>(json);
            
            Assert.NotNull(result);
            Assert.Null(result?.Element);
        }
        
        [Fact]
        public void Can_Deserialize_Concrete_Class()
        {
            var json = "{\"Element\": {\"Value\": 999}}";

            var result = JsonSerializer.Deserialize<Container>(json);
            
            Assert.NotNull(result);
            Assert.NotNull(result?.Element);
            Assert.Equal(999, result?.Element?.Value);
        }
        
        public class Container
        {
            [JsonConverter(typeof(StoryblokNullableConverter<Element>))] public Element? Element { get; set; }
        }

        public class Element
        {
            public int Value { get; set; }
        }
    }
}