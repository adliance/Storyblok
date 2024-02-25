using Markdig;

namespace Adliance.Storyblok;

public class Markdown
{
    public Markdown() { }

    public Markdown(string? value)
    {
        Value = value;
    }
    public string? Value { get; set; }

    // ReSharper disable once UnusedMember.Global
    public string Html
    {
        get
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            return Markdig.Markdown.ToHtml(Value ?? "", pipeline);
        }
    }
}
