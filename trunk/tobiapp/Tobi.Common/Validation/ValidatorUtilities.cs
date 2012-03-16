using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using AudioLib;
using Tobi.Common.UI.XAML;
using urakawa.core;

namespace Tobi.Common.Validation
{
    /// <summary>
    /// Static functions useful to validators (and perhaps others too, we'll see; for now they can live here)
    /// </summary>
    public class ValidatorUtilities
    {
        ////returns a 1-child-deep "flat" xml representation
        //public static string GetNodeXml(TreeNode node, bool flat)
        //{
        //    if (node == null) return "";
        //    if (flat)
        //        return GetNodeXml_Flat(node);
        //    else
        //        return GetNodeXml_Deep(node, 0);
        //}

        public static string GetTreeNodeTextExcerpt(TreeNode node)
        {
            TreeNode.StringChunk strChunkStart = node.GetTextFlattened_(true);
            if (strChunkStart == null || string.IsNullOrEmpty(strChunkStart.Str))
            {
                return "";
            }

            int strBuilderCapacity = 0;

            ////if this is a mixed content model node with previous sibling(s), add some ellipses before the text too
            //string strBuilder_str1 = (node.GetXmlElementQName() == null && node.PreviousSibling != null)
            //                            ? @"...\n" : null;
            //if (strBuilder_str1 != null)
            //{
            //    strBuilderCapacity += strBuilder_str1.Length;
            //}

            int maxLength = 100;
            int length = strChunkStart.GetLength();
            strBuilderCapacity += Math.Min(length, maxLength);
            string strBuilder_str2 = length > maxLength ? @"..." : null;
            if (strBuilder_str2 != null)
            {
                strBuilderCapacity += strBuilder_str2.Length;
            }

            StringBuilder strBuilder = new StringBuilder(strBuilderCapacity);
            TreeNode.ConcatStringChunks(strChunkStart, maxLength, strBuilder);
            if (strBuilder_str2 != null)
            {
                strBuilder.Append(strBuilder_str2);
            }
            //if (strBuilder_str1 != null)
            //{
            //    strBuilder.Insert(0, strBuilder_str1);
            //}
            DebugFix.Assert(strBuilder.Length == strBuilderCapacity);
            return strBuilder.ToString();
        }

        public static string GetNodeXml_Flat(TreeNode node)
        {
            if (node == null) return "";

            string nodeName = GetTreeNodeName(node);
            string text = GetTreeNodeTextExcerpt(node);
            string str1 = @"<";
            string str2 = @">";
            string str3 = @"/>";
            string str4 = @"</";

            int capacity = string.IsNullOrEmpty(nodeName) ? text.Length
                : (nodeName.Length +
                (text.Length == 0 ?
                str1.Length + str3.Length
                : str1.Length + str2.Length + text.Length + nodeName.Length + str4.Length + str2.Length
                )
                );
            StringBuilder strBuilder = new StringBuilder(capacity);

            if (string.IsNullOrEmpty(nodeName))
            {
                strBuilder.Append(text);
            }
            else
            {
                strBuilder.Append(str1);
                if (text.Length == 0)
                {
                    strBuilder.Append(nodeName);
                    strBuilder.Append(str3);
                }
                else
                {
                    strBuilder.Append(nodeName);
                    strBuilder.Append(str2);
                    strBuilder.Append(text);
                    strBuilder.Append(str4);
                    strBuilder.Append(nodeName);
                    strBuilder.Append(str2);
                }
            }

            DebugFix.Assert(strBuilder.Length == capacity);
            return strBuilder.ToString();
        }

        //private static string GetNodeXml_Deep(TreeNode node, int level)
        //{
        //    string xml = "";
        //    string nodeName = "";
        //    string indent = new string('\t', level);
        //    if (node.GetXmlElementQName() != null)
        //    {
        //        nodeName = node.GetXmlElementQName().LocalName;
        //        xml += string.Format("\n{0}<{1}>", indent, nodeName);
        //    }

        //    foreach (TreeNode child in node.Children.ContentsAs_Enumerable)
        //    {
        //        xml += GetNodeXml_Deep(child, level++);
        //    }

        //    if (!string.IsNullOrEmpty(nodeName))
        //        xml += string.Format("\n{0}</{1}>", indent, nodeName);
        //    return xml;
        //}

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

    }
    ////The purpose of this code is to format a text fragment of a XUK tree to look like XML
    ////it is displayed as a flow document
    //[ValueConversion(typeof(TreeNode), typeof(FlowDocument))]
    //public class TreeNodeFlowDocumentConverter : ValueConverterMarkupExtensionBase<TreeNodeFlowDocumentConverter>
    //{
    //    public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        if (value == null) return "";
    //        if (!(value is TreeNode)) return "";

    //        TreeNode node = value as TreeNode;
    //        FlowDocument doc = new FlowDocument();
    //        doc.FontSize = 12;
    //        doc.FontFamily = new FontFamily("Courier New");
    //        doc.Background = SystemColors.WindowBrush;
    //        doc.Foreground = SystemColors.WindowTextBrush;
    //        WriteNodeXml_Flat(node, doc);

    //        return doc;
    //    }

    //    private void WriteNodeXml_Flat(TreeNode node, FlowDocument doc)
    //    {
    //        string nodeName = ValidatorUtilities.GetTreeNodeName(node);
    //        Paragraph paragraph = new Paragraph();
    //        bool emptyElement = false;
    //        if (node.Children.Count == 0 && node.GetTextMedia() == null)
    //        {
    //            emptyElement = true;
    //            paragraph.Inlines.Add(new Bold(new Run(string.Format("<{0}/>", nodeName))));
    //        }
    //        else
    //        {
    //            paragraph.Inlines.Add(new Bold(new Run(string.Format("<{0}>", nodeName))));
    //        }
    //        if (node.GetTextMedia() != null)
    //        {
    //            Debug.Assert(node.Children.Count == 0);

    //            string txt;
    //            if (node.GetTextMedia().Text.Length > 20)
    //            {
    //                txt = node.GetTextMedia().Text.Substring(0, 20) + "...";
    //            }
    //            else
    //            {
    //                txt = node.GetTextMedia().Text;
    //            }
    //            paragraph.Inlines.Add(new Run(txt));
    //        }

    //        foreach (TreeNode child in node.Children.ContentsAs_Enumerable)
    //        {
    //            string childNodeText = ValidatorUtilities.GetTreeNodeTextExcerpt(child);
    //            string childNodeName = ValidatorUtilities.GetTreeNodeName(child);
    //            paragraph.Inlines.Add(new LineBreak());
    //            paragraph.Inlines.Add(new Bold(new Run(string.Format("  <{0}>", childNodeName))));
    //            paragraph.Inlines.Add(new Run(childNodeText));
    //            paragraph.Inlines.Add(new Bold(new Run(string.Format("</{0}>", childNodeName))));
    //        }
    //        if (node.Children.Count > 0)
    //        {
    //            paragraph.Inlines.Add(new LineBreak());
    //        }
    //        //else
    //        //{
    //        //    if (node.GetTextMedia() != null)
    //        //        paragraph.Inlines.Add(new Run("..."));
    //        //}
    //        if (!emptyElement)
    //            paragraph.Inlines.Add(new Bold(new Run(string.Format("</{0}>", nodeName))));
    //        doc.Blocks.Add(paragraph);
    //    }
    //}


    [ValueConversion(typeof(TreeNode), typeof(string))]
    public class NodeToXmlConverter : ValueConverterMarkupExtensionBase<NodeToXmlConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            if (!(value is TreeNode)) return "";

            return ValidatorUtilities.GetNodeXml_Flat(value as TreeNode);
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
