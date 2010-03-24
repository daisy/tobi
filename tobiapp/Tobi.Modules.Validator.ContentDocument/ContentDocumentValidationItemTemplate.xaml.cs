using System;
using System.Collections;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
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
            return GetElementName(value as TreeNode);
        }
        public static string GetElementName(TreeNode node)
        {
            string elementName = GetNearestElementName(node);
            if (string.IsNullOrEmpty(elementName)) return "";
            return elementName;
        }
        public static string GetNearestElementName(TreeNode node)
        {
            if (node.GetXmlElementQName() == null)
            {
                if (node.Parent != null)
                    return GetNearestElementName(node.Parent);
                else
                    return "";
            }
            
            return node.GetXmlElementQName().LocalName;
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
            string elementText = element.GetTextMediaFlattened(false);
            if (elementText == null) return "";
            if (elementText.Length > 100)
                elementText = elementText.Substring(0, 100);

            elementText += "...";
            
            //if this is a mixed content model node with previous sibling(s), add some ellipses before the text too
            if (element.GetXmlElementQName() == null && element.PreviousSibling != null)
                elementText = "...\n" + elementText;

            return elementText;
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

            return GetCleanRegex(value as string);
            
        }
        //todo: work on this regex string pretty print function
        public static string GetCleanRegex(string regex)
        {
            if (string.IsNullOrEmpty(regex)) return "";

            return regex.Replace("?:", "").Replace("#", "").Replace("((", "( (").Replace("))", ") )").Replace(")?(", ")? (");
        }
    }
    [ValueConversion(typeof(TreeNode), typeof(FlowDocument))]
    public class TreeNodeFlowDocumentConverter : ValueConverterMarkupExtensionBase<TreeNodeFlowDocumentConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            if (!(value is TreeNode)) return "";

            FlowDocument doc = new FlowDocument();
            Paragraph para = new Paragraph();
            para.Inlines.Add("Hello World!");
            doc.Blocks.Add(para);
            return doc;
        }
    }
}
