using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;

namespace DestinyGhostAssistant.Utils
{
    public static class RichTextBoxHelper
    {
        public static readonly DependencyProperty BoundDocumentProperty =
            DependencyProperty.RegisterAttached(
                "BoundDocument",
                typeof(FlowDocument),
                typeof(RichTextBoxHelper),
                new PropertyMetadata(null, OnBoundDocumentChanged));

        public static FlowDocument? GetBoundDocument(DependencyObject obj)
        {
            return (FlowDocument?)obj.GetValue(BoundDocumentProperty);
        }

        public static void SetBoundDocument(DependencyObject obj, FlowDocument? value)
        {
            obj.SetValue(BoundDocumentProperty, value);
        }

        private static void OnBoundDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RichTextBox richTextBox)
            {
                var document = e.NewValue as FlowDocument;
                if (document != null)
                {
                    AttachHyperlinkHandlers(document);
                }
                richTextBox.Document = document ?? new FlowDocument();
            }
        }

        /// <summary>
        /// Walks all Hyperlinks in the FlowDocument and attaches a RequestNavigate
        /// handler so they open in the default browser.
        /// </summary>
        private static void AttachHyperlinkHandlers(FlowDocument document)
        {
            foreach (var block in document.Blocks)
            {
                ProcessBlock(block);
            }
        }

        private static void ProcessBlock(Block block)
        {
            if (block is Paragraph paragraph)
            {
                ProcessInlines(paragraph.Inlines);
            }
            else if (block is List list)
            {
                foreach (var listItem in list.ListItems)
                {
                    foreach (var b in listItem.Blocks)
                    {
                        ProcessBlock(b);
                    }
                }
            }
            else if (block is Section section)
            {
                foreach (var b in section.Blocks)
                {
                    ProcessBlock(b);
                }
            }
            else if (block is Table table)
            {
                foreach (var rowGroup in table.RowGroups)
                {
                    foreach (var row in rowGroup.Rows)
                    {
                        foreach (var cell in row.Cells)
                        {
                            foreach (var b in cell.Blocks)
                            {
                                ProcessBlock(b);
                            }
                        }
                    }
                }
            }
        }

        private static void ProcessInlines(InlineCollection inlines)
        {
            foreach (var inline in inlines)
            {
                if (inline is Hyperlink hyperlink)
                {
                    hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
                    hyperlink.Foreground = new SolidColorBrush(Color.FromRgb(96, 165, 250)); // Blue accent
                    hyperlink.Cursor = System.Windows.Input.Cursors.Hand;
                    // Process any nested inlines inside the hyperlink
                    ProcessInlines(hyperlink.Inlines);
                }
                else if (inline is Span span)
                {
                    ProcessInlines(span.Inlines);
                }
            }
        }

        private static void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open URL: {e.Uri} — {ex.Message}");
            }
            e.Handled = true;
        }
    }
}
