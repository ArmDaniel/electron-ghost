using System.Windows.Documents;
using System.Windows.Markup;
using Markdig;
using Markdig.Wpf;

namespace DestinyGhostAssistant.Utils
{
    public static class MarkdownConverter
    {
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UseSupportedExtensions()
            .Build();

        public static FlowDocument? ToFlowDocument(string? markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return null;
            }

            try
            {
                var document = Markdig.Wpf.Markdown.ToFlowDocument(markdown, Pipeline);
                return document;
            }
            catch
            {
                // If markdown parsing fails, return a simple document with the plain text
                var fallbackDoc = new FlowDocument();
                fallbackDoc.Blocks.Add(new Paragraph(new Run(markdown)));
                return fallbackDoc;
            }
        }
    }
}
