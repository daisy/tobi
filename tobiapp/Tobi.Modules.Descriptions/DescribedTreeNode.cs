using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Tobi.Common.MVVM;
using urakawa.core;
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
            string str = "";
            QualifiedName qname = treeNode.GetXmlElementQName();
            //if (qname != null)
            //{
            //    str = "[" + qname.LocalName + "] ";
            //}
            string text = treeNode.GetTextFlattened(true);
            if (!string.IsNullOrEmpty(text))
            {
                if (text.Length > 40)
                {
                    text = text.Substring(0, 40) + "(...)";
                }
                str = str + text;
            }

            if (qname != null && qname.LocalName.ToLower() == "img")
            {
                XmlAttribute xmlAttr = treeNode.GetXmlProperty().GetAttribute("src");
                if (xmlAttr != null && !String.IsNullOrEmpty(xmlAttr.Value))
                {
                    str = str + " [";
                    string strAttr = xmlAttr.Value.TrimEnd('/');
                    int index = strAttr.LastIndexOf('/');
                    if (index >= 0)
                    {
                        str = str + strAttr.Substring(index);
                    }
                    else
                    {
                        str = str + strAttr;
                    }
                    str = str + "] ";
                }
            }

            return str;
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
                return TreeNode.HasAlternateContentProperty;
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
