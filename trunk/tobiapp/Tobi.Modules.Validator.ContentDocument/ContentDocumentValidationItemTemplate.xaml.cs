using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
            MessageBox.Show("This should highlight the error in the document");
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
                return "Missing DTD";
            if (item == ContentDocumentErrorType.UndefinedElement)
                return "Undefined element";
            if (item == ContentDocumentErrorType.InvalidElementSequence)
                return "Document structure error";
            return "General document error";

        }
    }

    [ValueConversion(typeof(TreeNode), typeof(string))]
    public class ElementNameStartTagConverter : ValueConverterMarkupExtensionBase<ElementNameStartTagConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            if (!(value is TreeNode)) return "";
            TreeNode element = value as TreeNode;
            if (element.GetXmlElementQName() == null) return "";
            string elementName = element.GetXmlElementQName().LocalName;
            if (string.IsNullOrEmpty(elementName)) return "";
            return string.Format("<{0}>", elementName);
        }       
    }

    [ValueConversion(typeof(TreeNode), typeof(string))]
    public class ElementNameEndTagConverter : ValueConverterMarkupExtensionBase<ElementNameEndTagConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return "";
            if (!(value is TreeNode)) return "";
            TreeNode element = value as TreeNode;
            if (element.GetXmlElementQName() == null) return "";
            string elementName = element.GetXmlElementQName().LocalName;
            if (string.IsNullOrEmpty(elementName)) return "";
            string test = string.Format("</{0}>", elementName);
            return test;
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
            string elementText = element.GetTextMediaFlattened();
            if (elementText.Length > 100)
                elementText = elementText.Substring(0, 100);

            elementText += "...";
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
}
