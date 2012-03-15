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
            QualifiedName qname = treeNode.GetXmlElementQName();
            if (qname != null)
            {
                strBuilder.Append("[");
                strBuilder.Append(qname.LocalName);
                strBuilder.Append("] ");

                length = strBuilder.Length;
            }

            TreeNode.StringChunk strChunkStart = treeNode.GetTextFlattened_(true);
            if (strChunkStart != null && !string.IsNullOrEmpty(strChunkStart.Str))
            {
                int l1 = length;
                TreeNode.ConcatStringChunks(strChunkStart, strBuilder);
                int l2 = strBuilder.Length;

                int added = l2 - l1;
                if (added > 40)
                {
                    //string str = strBuilder.ToString(0, 40);
                    //strBuilder.Clear();
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


        public void RaiseDescriptionChanged()
        {
            RaisePropertyChanged(() => Description);
        }

        public string Description
        {
            get
            {
                return GetMarkerDescription(TreeNode);
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
