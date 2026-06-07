using Markdig;

namespace MyMonitorHub.Web
{
    public static class MarkdownHelper
    {
        private static readonly MarkdownPipeline Pipeline =
            new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        public static string ToHtml(string? markdown)
        {
            if (string.IsNullOrEmpty(markdown)) return string.Empty;
            return Markdown.ToHtml(markdown, Pipeline);
        }
    }
}
