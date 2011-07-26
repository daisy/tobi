using Tobi.Common.MVVM;
using urakawa.core;
using urakawa.xuk;

namespace Tobi.Plugin.Descriptions
{
    public class DescribedTreeNode : PropertyChangedNotifyBase
    {
        public DescribedTreeNode(TreeNode node)
        {
            TreeNode = node;
        }

        public TreeNode TreeNode
        {
            get;
            private set;
        }

        private bool m_isSelected;
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

        public static string GetDescriptionLabel(TreeNode treeNode)
        {
            string str = "";
            QualifiedName qname = treeNode.GetXmlElementQName();
            if (qname != null)
            {
                str = "[" + qname.LocalName + "] ";
            }
            string text = treeNode.GetTextFlattened(true);
            if (!string.IsNullOrEmpty(text))
            {
                if (text.Length > 40)
                {
                    text = text.Substring(0, 40) + "(...)";
                }
                str = str + text;
            }
            return str;
        }


        public void RaiseDescriptionChanged()
        {
            RaisePropertyChanged(() => Description);
        }

        public string Description
        {
            get
            {
                return GetDescriptionLabel(TreeNode);
            }
        }

        private bool m_isMatch;
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
    }
}
