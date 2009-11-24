using System.Windows.Documents;
using System.Text;
using Tobi.Common.MVVM;
using urakawa.core;

namespace Tobi.Plugin.NavigationPane
{
    public class Page : PropertyChangedNotifyBase
    {
        private bool m_isMatch;
        private bool m_isSelected;
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
        public bool IsSelected
        {
            get { return m_isSelected; }
            set
            {
                if (m_isSelected == value) { return; }
                m_isSelected = value;
                RaisePropertyChanged(() => IsSelected);
            }
        }
        public string Name
        {
            get
            {
                string pageTxt = ((TreeNode)TextElement.Tag).GetTextMediaFlattened();
                if (!string.IsNullOrEmpty(pageTxt))
                {
                    return pageTxt;
                }
                //if (TextElement is Paragraph)
                //{
                //    return extractString((Paragraph)TextElement);
                //}
                return "??";
            }
        }
        public bool SearchMatch
        {
            get { return m_isMatch; }
            set
            {
                if (m_isMatch == value) { return; }
                m_isMatch = value;
                RaisePropertyChanged(() => SearchMatch);
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
