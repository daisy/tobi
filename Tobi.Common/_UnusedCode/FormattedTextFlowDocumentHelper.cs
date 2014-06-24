using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;

namespace Tobi.Common._UnusedCode
{
    public static class FormattedTextFlowDocumentHelper
    {
        private static IEnumerable<TextElement> GetRunsAndParagraphs(FlowDocument doc)
        {
            for (TextPointer position = doc.ContentStart;
                position != null && position.CompareTo(doc.ContentEnd) <= 0;
                position = position.GetNextContextPosition(LogicalDirection.Forward))
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd
                    && (position.Parent is Run || position.Parent is Paragraph))
                {
                    yield return (TextElement)position.Parent;
                }
            }

            yield break;
        }

        private static string GetText(FlowDocument doc)
        {
            var sb = new StringBuilder();

            foreach (TextElement el in GetRunsAndParagraphs(doc))
            {
                var run = el as Run;
                sb.Append(run == null ? Environment.NewLine : run.Text);
            }

            return sb.ToString();
        }

        public static FormattedText GetFormattedText(FlowDocument doc)
        {
            if (doc == null)
            {
                throw new ArgumentNullException("doc");
            }

            var formattedText = new FormattedText(
                GetText(doc),
                CultureInfo.CurrentCulture,
                doc.FlowDirection,
                new Typeface(doc.FontFamily, doc.FontStyle, doc.FontWeight, doc.FontStretch),
                doc.FontSize,
                doc.Foreground);

            int offset = 0;

            foreach (TextElement el in GetRunsAndParagraphs(doc))
            {
                Run run = el as Run;

                if (run != null)
                {
                    int count = run.Text.Length;

                    formattedText.SetFontFamily(run.FontFamily, offset, count);
                    formattedText.SetFontStyle(run.FontStyle, offset, count);
                    formattedText.SetFontWeight(run.FontWeight, offset, count);
                    formattedText.SetFontSize(run.FontSize, offset, count);
                    formattedText.SetForegroundBrush(run.Foreground, offset, count);
                    formattedText.SetFontStretch(run.FontStretch, offset, count);
                    formattedText.SetTextDecorations(run.TextDecorations, offset, count);

                    offset += count;
                }
                else
                {
                    offset += Environment.NewLine.Length;
                }
            }

            return formattedText;
        }
    }
}
