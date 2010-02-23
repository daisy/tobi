using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Tobi.Common.UI.XAML;
using Tobi.Common.Validation;

namespace Tobi.Plugin.Validator.ContentDocument
{
    [Export(ValidationDataTemplateProperties.TypeIdentifier, typeof(ResourceDictionary)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class ContentDocumentValidationItemTemplate : ResourceDictionary
    {
        public ContentDocumentValidationItemTemplate()
        {
            InitializeComponent();
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
            if (item == ContentDocumentErrorType.InvalidChildElements)
                return "Document structure error";
            return "General document error";

        }
    }

    [ValueConversion(typeof(ContentDocumentValidationError), typeof(string))]
    public class ContentDocumentErrorDescriptionConverter : ValueConverterMarkupExtensionBase<ContentDocumentErrorDescriptionConverter>
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
            /*if (value == null) return "";
            if (!(value is ContentDocumentValidationError)) return "";
            ContentDocumentValidationError error = (ContentDocumentValidationError) value;

            if (error.ErrorType == ContentDocumentErrorType.InvalidChildElements)
                return InvalidChildElementsError(error);
            if (error.ErrorType == ContentDocumentErrorType.UndefinedElement)
                return */
        }
 
        
    }

    public class ContentDocumentErrorTemplateSelector :  DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null) return null;

            FrameworkElement elem = container as FrameworkElement;
            ContentDocumentValidationError error = item as ContentDocumentValidationError;

            if (error.ErrorType == ContentDocumentErrorType.MissingDtd)
                return (DataTemplate)elem.FindResource("MissingDtdTemplate");
            if (error.ErrorType == ContentDocumentErrorType.UndefinedElement)
                return (DataTemplate)elem.FindResource("UndefinedElementTemplate");
            if (error.ErrorType == ContentDocumentErrorType.InvalidChildElements)
                return (DataTemplate) elem.FindResource("InvalidChildElementsTemplate");
            return (DataTemplate) elem.FindResource("GeneralErrorTemplate");
        }
    }


}
