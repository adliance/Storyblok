using Markdig;

namespace Saxx.Storyblok
{
    public class Markdown
    {
        public string Value { get; set; }
        
        public string Html {
            get
            {
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                return Markdig.Markdown.ToHtml(Value ?? "", pipeline);
            }
        }
    }
}