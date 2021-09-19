using System;
using System.Collections.Generic;
using System.Linq;
using Adliance.Storyblok.Attributes;

namespace Adliance.Storyblok
{
    public static class StoryblokMappings
    {
        private static IDictionary<string, Mapping>? _mappingsCache; // make static, because it should be "valid" for as long as the app is running

        public static IDictionary<string, Mapping> Mappings
        {
            get
            {
                if (_mappingsCache == null)
                {
                    var components = from a in AppDomain.CurrentDomain.GetAssemblies()
                        from t in a.GetTypes()
                        let attributes = t.GetCustomAttributes(typeof(StoryblokComponentAttribute), true)
                        where attributes != null && attributes.Length > 0
                        select new {Type = t, Attribute = attributes.Cast<StoryblokComponentAttribute>().First()};

                    var mappingsCache = new Dictionary<string, Mapping>();
                    foreach (var c in components)
                    {
                        if (mappingsCache.ContainsKey(c.Attribute.Name))
                        {
                            continue;
                        }
                        
                        mappingsCache[c.Attribute.Name] = new Mapping
                        {
                            Type = c.Type,
                            ComponentName = c.Attribute.Name,
                            View = c.Attribute.View
                        };
                    }

                    _mappingsCache = mappingsCache; // this makes sure we don't have concurrent operations on the dictionary while it's filling
                    return mappingsCache;
                }

                return _mappingsCache;
            }
        }

        public class Mapping
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public string ComponentName { get; set; } = "";
            public Type Type { get; set; } = typeof(object);
            public string? View { get; set; }
        }
    }
}