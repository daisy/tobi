using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Tobi.Common.MVVM;
using urakawa.core;
using urakawa.property.alt;
using urakawa.property.xml;
using urakawa.xuk;

namespace Tobi.Plugin.Descriptions
{
    public class DescribableTreeNode : PropertyChangedNotifyBase
    {
        public DescribableTreeNode(TreeNode node)
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

        public static ImageSource GetDescribableImage(TreeNode treeNode)
        {
            var te = treeNode.Tag as TextElement;
            if (te != null)
            {
                Panel panel = null;
                if (te is InlineUIContainer)
                {
                    panel = ((InlineUIContainer)te).Child as Panel;
                }
                else if (te is BlockUIContainer)
                {
                    panel = ((BlockUIContainer)te).Child as Panel;
                }

                Image img = null;
                if (panel != null)
                {
                    foreach (UIElement uiElement in panel.Children)
                    {
                        if (uiElement is Image)
                        {
                            img = (Image)uiElement;
                            break;
                        }
                    }
                }
                if (img != null)
                {
                    return img.Source;
                }
            }

            return null;
        }

        public static string GetDescriptionLabel(TreeNode treeNode, int limit)
        {
            //if (qname != null)
            //{
            //    str = "[" + qname.LocalName + "] ";
            //}

            StringBuilder strBuilder = null;
            int length = 0;
            TreeNode.StringChunkRange range = treeNode.GetTextFlattened_();
            if (range != null && range.First != null && !string.IsNullOrEmpty(range.First.Str))
            {
                strBuilder = new StringBuilder(range.GetLength());
                TreeNode.ConcatStringChunks(range, -1, strBuilder);
                length = strBuilder.Length;
                if (length > limit)
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
                    strBuilder.Insert(limit, addon);
                    length = limit + addon.Length;
                }
            }

            if (treeNode.HasXmlProperty)
            {
                string localName = treeNode.GetXmlElementLocalName();

                if (localName.Equals("img", StringComparison.OrdinalIgnoreCase)
                    || localName.Equals("video", StringComparison.OrdinalIgnoreCase)
                    )
                {
                    XmlAttribute xmlAttr = treeNode.GetXmlProperty().GetAttribute("src");
                    if (xmlAttr != null && !String.IsNullOrEmpty(xmlAttr.Value))
                    {

                        if (strBuilder == null)
                        {
                            strBuilder = new StringBuilder();
                        }

                        int l1 = strBuilder.Length;

                        strBuilder.Append("  --> [");
                        string strAttr = xmlAttr.Value.TrimEnd('/');
                        int index = strAttr.LastIndexOf('/');
                        if (index >= 0)
                        {
                            strBuilder.Append(strAttr.Substring(index + 1));
                        }
                        else
                        {
                            strBuilder.Append(strAttr);
                        }
                        strBuilder.Append("] ");

                        int l2 = strBuilder.Length;
                        int added = l2 - l1;
                        length += added;
                    }
                }
            }

            if (strBuilder == null)
            {
                return "";
            }

            return strBuilder.ToString(0, Math.Min(length, strBuilder.Length));
        }



        public void RaiseHasDescriptionChanged()
        {
            RaisePropertyChanged(() => HasDescription);
        }

        public bool HasDescription
        {
            get
            {
                var altProp = TreeNode.GetProperty<AlternateContentProperty>();
                return altProp != null && !altProp.IsEmpty;

                //return TreeNode.HasAlternateContentProperty;
            }
        }


        public ImageSource DescribableImage
        {
            get
            {
                return GetDescribableImage(TreeNode);
            }
        }

        public void InvalidateDescription()
        {
            m_Description = null;
            m_DescriptionX = null;

            RaisePropertyChanged(() => Description);
            RaisePropertyChanged(() => DescriptionX);
        }

        private string m_Description;
        public string Description
        {
            get
            {
                if (string.IsNullOrEmpty(m_Description))
                {
                    m_Description = GetDescriptionLabel(TreeNode, 100);
                }

                return m_Description;
            }
        }

        private string m_DescriptionX;
        public string DescriptionX
        {
            get
            {
                if (string.IsNullOrEmpty(m_DescriptionX))
                {
                    m_DescriptionX = (HasDescription ? "(described) " : "(no description) ") + Description;
                }

                return m_DescriptionX;
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
