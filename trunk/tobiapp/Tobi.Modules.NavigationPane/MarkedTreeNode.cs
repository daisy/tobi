using System;
using System.Text;
using Tobi.Common.MVVM;
using urakawa.core;
using urakawa.xuk;

namespace Tobi.Plugin.NavigationPane
{
    public class MarkedTreeNode : PropertyChangedNotifyBase
    {
        public MarkedTreeNode(TreeNode node)
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

        public static string GetMarkerDescription(TreeNode treeNode)
        {
            StringBuilder strBuilder = new StringBuilder();
            int length = 0;

            if (treeNode.HasXmlProperty)
            {
                string localName = treeNode.GetXmlElementLocalName();

                strBuilder.Append("[");
                strBuilder.Append(localName);
                strBuilder.Append("] ");

                length = strBuilder.Length;
            }

            TreeNode.StringChunkRange range = treeNode.GetTextFlattened_();
            if (range != null && range.First != null && !string.IsNullOrEmpty(range.First.Str))
            {
                int l1 = length;
                TreeNode.ConcatStringChunks(range, -1, strBuilder);
                int l2 = strBuilder.Length;

                int added = l2 - l1;
                if (added > 40)
                {
                    //string str = strBuilder.ToString(0, 40);
                    //#if NET40
                    //                    stringBuilder.Clear();
                    //#else
                    //                    stringBuilder.Length = 0;
                    //#endif //NET40
                    //strBuilder.Append(str);
                    //strBuilder.Append("(...)");

                    string addon = "(...)";
                    strBuilder.Insert(length + 40, addon);
                    length += 40 + addon.Length;
                }
                else
                {
                    length += added;
                }
            }

            return strBuilder.ToString(0, Math.Min(length, strBuilder.Length));
        }


        public void InvalidateDescription()
        {
            m_Description = null;
            RaisePropertyChanged(() => Description);
        }

        private string m_Description;
        public string Description
        {
            get
            {
                if (!string.IsNullOrEmpty(m_Description))
                {
                    return m_Description;
                }

                m_Description = GetMarkerDescription(TreeNode);
                return m_Description;
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
