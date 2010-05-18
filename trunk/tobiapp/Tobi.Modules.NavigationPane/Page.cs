using System.Diagnostics;
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
        public Page(TreeNode treeNode)
        {
            TreeNode = treeNode;
        }

        public TreeNode TreeNode
        {
            get;
            private set;
        }

        //public string Id
        //{
        //    get
        //    {
        //        string uid = ((TreeNode)TextElement.Tag).GetXmlElementId();
        //        if (string.IsNullOrEmpty(uid)) return "";

        //        return uid;
        //    }
        //}
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

        public void RaiseNameChanged()
        {
            RaisePropertyChanged(() => Name);
        }

        public string Name
        {
            get
            {
                string pageTxt = TreeNode.GetTextFlattened(true);
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
