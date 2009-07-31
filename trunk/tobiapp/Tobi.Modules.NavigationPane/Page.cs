using System.Windows.Documents;
using System.Text;

namespace Tobi.Modules.NavigationPane
{
    public class Page
    {
        public Page(TextElement textElement)
        {
            TextElement = textElement;
        }

        public TextElement TextElement
        {
            get;
            private set;
        }
        public string Id
        {
            get
            {
                return TextElement.Name;
            }
        }
        public string Name
        {
            get
            {
                if (TextElement is Paragraph)
                {
                    return extractString((Paragraph)TextElement);
                }
                return "??";
            }
        }

        private static string extractString(Paragraph para)
        {
            StringBuilder str = new StringBuilder();
            foreach (Inline inline in para.Inlines)
            {
                if (inline is Run)
                {
                    str.Append(((Run)inline).Text);
                }
                else if (inline is Span)
                {
                    str.Append(extractString((Span)inline));
                }
            }
            return str.ToString();
        }

        private static string extractString(Span span)
        {
            StringBuilder str = new StringBuilder();
            foreach (Inline inline in span.Inlines)
            {
                if (inline is Run)
                {
                    str.Append(((Run)inline).Text);
                }
                else if (inline is Span)
                {
                    str.Append(extractString((Span)inline));
                }
            }
            return str.ToString();
        }
    }
}
