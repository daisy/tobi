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

        public static string GetDescriptionLabel(TreeNode treeNode)
        {
            QualifiedName qname = treeNode.GetXmlElementQName();
            //if (qname != null)
            //{
            //    str = "[" + qname.LocalName + "] ";
            //}

            StringBuilder strBuilder = null;
            int length = 0;
            TreeNode.StringChunk strChunkStart = treeNode.GetTextFlattened_(true);
            if (strChunkStart != null && !string.IsNullOrEmpty(strChunkStart.Str))
            {
                strBuilder = new StringBuilder(strChunkStart.GetLength());
                TreeNode.ConcatStringChunks(strChunkStart, -1, strBuilder);
                length = strBuilder.Length;
                if (length > 40)
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
                    strBuilder.Insert(40, addon);
                    length = 40 + addon.Length;
                }
            }

            if (qname != null && qname.LocalName.Equals("img", StringComparison.OrdinalIgnoreCase))
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
                        strBuilder.Append(strAttr.Substring(index));
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

            if (strBuilder == null)
            {
                return "";
            }

            return strBuilder.ToString(0, Math.Min(length, strBuilder.Length));
        }


        public void RaiseDescriptionChanged()
        {
            RaisePropertyChanged(() => Description);
            RaisePropertyChanged(() => DescriptionX);
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

        public string Description
        {
            get
            {
                return GetDescriptionLabel(TreeNode);
            }
        }

        public string DescriptionX
        {
            get
            {
                return (HasDescription ? "(described) " : "(no description) ") + Description;
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
