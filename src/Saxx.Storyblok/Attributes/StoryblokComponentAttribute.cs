using System;

namespace Saxx.Storyblok.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class StoryblokComponentAttribute : Attribute
    {
        public string Name { get; }

        public StoryblokComponentAttribute(string name)
        {
            Name = name;
        }
    }
}