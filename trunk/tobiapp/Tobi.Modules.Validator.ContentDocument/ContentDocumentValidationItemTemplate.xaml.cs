using System;
using System.Collections;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Tobi.Common.UI.XAML;
using Tobi.Common.Validation;
using urakawa.core;

namespace Tobi.Plugin.Validator.ContentDocument
{
    [Export(ValidationDataTemplateProperties.TypeIdentifier, typeof(ResourceDictionary)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class ContentDocumentValidationItemTemplate : ResourceDictionary
    {
        public ContentDocumentValidationItemTemplate()
        {
            InitializeComponent();
        }
        
        private void OnLinkClick(object sender, RoutedEventArgs e)
        {
            var obj = sender as Hyperlink;
            ((ValidationItem)obj.DataContext).TakeAction();
        }
    }

    [ValueConversion(typeof(ContentDocumentErrorType), typeof(string))]
    public class ContentDocumentErrorTypeConverter : ValueConverterMarkupExtensionBase<ContentDocumentErrorTypeConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {   
            if (value == null) return "";
            if (!(value is ContentDocumentErrorType)) return "";

            ContentDocumentErrorType item = (ContentDocumentErrorType)value;
            if (item == ContentDocumentErrorType.MissingDtd)
                return Tobi_Plugin_Validator_ContentDocument_Lang.MissingDTD;                                           // TODO LOCALIZE MissingDTD
            if (item == ContentDocumentErrorType.UndefinedElement)
                return Tobi_Plugin_Validator_ContentDocument_Lang.UndefinedElement;                                     // TODO LOCALIZE UndefinedElement
            if (item == ContentDocumentErrorType.InvalidElementSequence)
                return Tobi_Plugin_Validator_ContentDocument_Lang.DocStructureError;                              // TODO LOCALIZE DocStructureError
            return Tobi_Plugin_Validator_ContentDocument_Lang.GeneralDocumentError;                                    // TODO LOCALIZE GeneralDocumentError

        }
    }

    [ValueConversion(typeof(TreeNode), typeof(string))]
    public class ElementNameConverter : ValueConverterMarkupExtensionBase<ElementNameConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            if (!(value is TreeNode)) return "";
            return ContentDocumentValidator.GetTreeNodeName(value as TreeNode);
        }
        
    }

    [ValueConversion(typeof(TreeNode), typeof(string))]
    public class ElementTextExcerptConverter : ValueConverterMarkupExtensionBase<ElementTextExcerptConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            if (!(value is TreeNode)) return "";
            TreeNode element = value as TreeNode;

            return ContentDocumentValidator.GetTreeNodeTextExcerpt(element);
        }
    }

    
    [ValueConversion(typeof(TreeNode), typeof(Visibility))]
    public class NodeToVisibilityConverter : ValueConverterMarkupExtensionBase<NodeToVisibilityConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return Visibility.Hidden;
            else return Visibility.Visible;
        }
    }

    [ValueConversion(typeof(TreeNode), typeof(IEnumerable))]
    public class DirectElementChildrenConverter : ValueConverterMarkupExtensionBase<DirectElementChildrenConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return null;
            if (! (value is TreeNode)) return null;
            TreeNode node = value as TreeNode;
            return node.Children.ContentsAs_YieldEnumerable;
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    public class AllowedChildNodesConverter : ValueConverterMarkupExtensionBase<AllowedChildNodesConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            if (!(value is string)) return "";

            return ContentDocumentValidator.GetCleanRegex(value as string);
            
        }
        
    }
    [ValueConversion(typeof(TreeNode), typeof(FlowDocument))]
    public class TreeNodeFlowDocumentConverter : ValueConverterMarkupExtensionBase<TreeNodeFlowDocumentConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            if (!(value is TreeNode)) return "";

            string str = ContentDocumentValidator.GetNodeXml(value as TreeNode);

            TreeNode node = value as TreeNode;
            FlowDocument doc = new FlowDocument();
            doc.FontSize = 10;
            doc.FontFamily = new FontFamily("Courier");
            WriteNodeXml_Flat(node, doc);
            return doc;
        }

        private void WriteNodeXml_Flat(TreeNode node, FlowDocument doc)
        {
            string nodeName = ContentDocumentValidator.GetTreeNodeName(node);
            Paragraph openingNodePara = new Paragraph();
            openingNodePara.Inlines.Add(new Bold(new Run(string.Format("<{0}>", nodeName))));
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
                openingNodePara.Inlines.Add(new Run(txt));
            }

            doc.Blocks.Add(openingNodePara);

            foreach (TreeNode child in node.Children.ContentsAs_YieldEnumerable)
            {
                Paragraph childXmlPara = new Paragraph();
                string childNodeText = ContentDocumentValidator.GetTreeNodeTextExcerpt(child);
                string childNodeName = ContentDocumentValidator.GetTreeNodeName(child);
                childXmlPara.Inlines.Add(new Bold(new Run(string.Format("<{0}>", childNodeName))));
                childXmlPara.Inlines.Add(new Run(childNodeText));
                childXmlPara.Inlines.Add(new Bold(new Run(string.Format("</{0}>", childNodeName))));
                childXmlPara.TextIndent = 5;
                doc.Blocks.Add(childXmlPara);
            }
            Paragraph closingNodePara = new Paragraph();
            closingNodePara.Inlines.Add(new Bold(new Run(string.Format("</{0}>", nodeName))));
            doc.Blocks.Add(closingNodePara);
        }

        private void WriteNodeXml_Deep(TreeNode node, FlowDocument doc, int level)
        {
            string nodeName = "";
            Paragraph p = new Paragraph();
            if (node.GetXmlElementQName() != null)
            {
                nodeName = node.GetXmlElementQName().LocalName;
                p.Inlines.Add(new Bold(new Run(string.Format("<{0}>", nodeName))));
            }
            if (node.GetTextMedia() != null && !string.IsNullOrEmpty(node.GetTextMedia().Text))
            {
                p.Inlines.Add(new Run(node.GetTextMedia().Text));
            }
            p.TextIndent = level*5;
            doc.Blocks.Add(p);
            
            foreach (TreeNode child in node.Children.ContentsAs_YieldEnumerable)
            {
                WriteNodeXml_Deep(child, doc, level++);
            }

            if (!string.IsNullOrEmpty(nodeName))
            {
                Paragraph p2 = new Paragraph();
                p2.Inlines.Add(new Bold(new Run(string.Format("</{0}>", nodeName))));
                p2.TextIndent = level*5;
                doc.Blocks.Add(p2);
            }
        }
            
    }

}
