using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Tobi.Common.UI.XAML;
using urakawa.core;

namespace Tobi.Common.Validation
{
    /// <summary>
    /// Static functions useful to validators (and perhaps others too, we'll see; for now they can live here)
    /// </summary>
    public class ValidatorUtilities
    {
        //returns a 1-child-deep "flat" xml representation
        public static string GetNodeXml(TreeNode node, bool flat)
        {
            if (node == null) return "";
            if (flat)
                return GetNodeXml_Flat(node);
            else
                return GetNodeXml_Deep(node, 0);
        }

        private static string GetNodeXml_Flat(TreeNode node)
        {
            string xml = "";
            string nodeName = GetTreeNodeName(node);
            bool emptyElement = false;
            if (node.Children.Count == 0 && node.GetTextMedia() == null)
            {
                emptyElement = true;
                xml = string.Format("<{0}/>", nodeName);
            }
            else
            {
                xml = string.Format("<{0}>", nodeName);
            }

            if (node.GetTextMedia() != null)
            {
                if (node.GetTextMedia().Text.Length > 10)
                {
                    xml += node.GetTextMedia().Text.Substring(0, 10);
                    if (node.Children.Count == 0 && node.GetTextMedia() != null)
                    {
                        xml += ("...");
                    }
                }
                else
                {
                    xml += node.GetTextMedia().Text;
                }
            }

            foreach (TreeNode child in node.Children.ContentsAs_YieldEnumerable)
            {
                string childNodeText = GetTreeNodeTextExcerpt(child);
                string childNodeName = GetTreeNodeName(child);
                xml += string.Format("\n\t<{0}>{1}</{0}>", childNodeName, childNodeText);
            }

            if (!emptyElement)
            {
                if (node.Children.Count == 0)
                    xml += string.Format("</{0}>", nodeName);
                else
                    xml += string.Format("\n<{0}>", nodeName);
            }
            return xml;
        }

        private static string GetNodeXml_Deep(TreeNode node, int level)
        {
            string xml = "";
            string nodeName = "";
            string indent = new string('\t', level);
            if (node.GetXmlElementQName() != null)
            {
                nodeName = node.GetXmlElementQName().LocalName;
                xml += string.Format("\n{0}<{1}>", indent, nodeName);
            }

            foreach (TreeNode child in node.Children.ContentsAs_YieldEnumerable)
            {
                xml += GetNodeXml_Deep(child, level++);
            }

            if (!string.IsNullOrEmpty(nodeName))
                xml += string.Format("\n{0}</{1}>", indent, nodeName);
            return xml;
        }

        //static helper functions
        public static string GetTreeNodeName(TreeNode node)
        {
            string nodeName = GetNearestTreeNodeName(node);
            if (string.IsNullOrEmpty(nodeName)) return "";
            return nodeName;
        }
        public static string GetNearestTreeNodeName(TreeNode node)
        {
            if (node.GetXmlElementQName() == null)
            {
                if (node.Parent != null)
                    return GetNearestTreeNodeName(node.Parent);
                else
                    return "";
            }

            return node.GetXmlElementQName().LocalName;
        }

        public static string GetTreeNodeTextExcerpt(TreeNode node)
        {

            string nodeText = node.GetTextMediaFlattened(false);
            if (nodeText == null) return "";
            if (nodeText.Length > 100)
                nodeText = nodeText.Substring(0, 100);

            nodeText += "...";

            //if this is a mixed content model node with previous sibling(s), add some ellipses before the text too
            if (node.GetXmlElementQName() == null && node.PreviousSibling != null)
                nodeText = "...\n" + nodeText;

            return nodeText;
        }

    }
    //The purpose of this code is to format a text fragment of a XUK tree to look like XML
    //it is displayed as a flow document
    [ValueConversion(typeof(TreeNode), typeof(FlowDocument))]
    public class TreeNodeFlowDocumentConverter : ValueConverterMarkupExtensionBase<TreeNodeFlowDocumentConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            if (!(value is TreeNode)) return "";

            TreeNode node = value as TreeNode;
            FlowDocument doc = new FlowDocument();
            doc.FontSize = 12;
            doc.FontFamily = new FontFamily("Courier New");
            doc.Background = SystemColors.WindowBrush;
            doc.Foreground = SystemColors.WindowTextBrush;
            WriteNodeXml_Flat(node, doc);

            return doc;
        }

        private void WriteNodeXml_Flat(TreeNode node, FlowDocument doc)
        {
            string nodeName = ValidatorUtilities.GetTreeNodeName(node);
            Paragraph paragraph = new Paragraph();
            bool emptyElement = false;
            if (node.Children.Count == 0 && node.GetTextMedia() == null)
            {
                emptyElement = true;
                paragraph.Inlines.Add(new Bold(new Run(string.Format("<{0}/>", nodeName))));
            }
            else
            {
                paragraph.Inlines.Add(new Bold(new Run(string.Format("<{0}>", nodeName))));
            }
            if (node.GetTextMedia() != null)
            {
                string txt;
                if (node.GetTextMedia().Text.Length > 10)
                {
                    txt = node.GetTextMedia().Text.Substring(0, 10);
                }
                else
                {
                    txt = node.GetTextMedia().Text;
                }
                paragraph.Inlines.Add(new Run(txt));
            }

            foreach (TreeNode child in node.Children.ContentsAs_YieldEnumerable)
            {
                string childNodeText = ValidatorUtilities.GetTreeNodeTextExcerpt(child);
                string childNodeName = ValidatorUtilities.GetTreeNodeName(child);
                paragraph.Inlines.Add(new LineBreak());
                paragraph.Inlines.Add(new Bold(new Run(string.Format("  <{0}>", childNodeName))));
                paragraph.Inlines.Add(new Run(childNodeText));
                paragraph.Inlines.Add(new Bold(new Run(string.Format("</{0}>", childNodeName))));
            }
            if (node.Children.Count > 0)
            {
                paragraph.Inlines.Add(new LineBreak());
            }
            else
            {
                if (node.GetTextMedia() != null)
                    paragraph.Inlines.Add(new Run("..."));
            }
            if (!emptyElement)
                paragraph.Inlines.Add(new Bold(new Run(string.Format("</{0}>", nodeName))));
            doc.Blocks.Add(paragraph);
        }
    }

    [ValueConversion(typeof(TreeNode), typeof(string))]
    public class ElementNameConverter : ValueConverterMarkupExtensionBase<ElementNameConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            if (!(value is TreeNode)) return "";
            return ValidatorUtilities.GetTreeNodeName(value as TreeNode);
        }
    }
}
