using System;

namespace Adliance.Storyblok.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class StoryblokComponentAttribute(string name, string? view = null) : Attribute
{
    public string Name { get; } = name;

    /// <summary>
    /// If this component is detected as the "root" of a story, this is the view that will be returned by the middleware.
    /// </summary>
    public string? View { get; set; } = view;
}
