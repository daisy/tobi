using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Tobi.Common.UI.XAML;
using Tobi.Common.Validation;
using urakawa.core;

namespace Tobi.Plugin.Validator.MissingAudio
{
    /// <summary>
    /// Interaction logic for MissingAudioValidationItemTemplate.xaml
    /// </summary>
    [Export(ValidationDataTemplateProperties.TypeIdentifier, typeof(ResourceDictionary)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class MissingAudioValidationItemTemplate : ResourceDictionary
    {
        public MissingAudioValidationItemTemplate()
        {
            InitializeComponent();
        }
    }



    //This was copied from ContentDocumentValidator
    //consider putting it in a common area
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
            doc.Background = new SolidColorBrush(Colors.White);
            doc.Foreground = new SolidColorBrush(Colors.Black);
            WriteNodeXml_Flat(node, doc);

            return doc;
        }

        private void WriteNodeXml_Flat(TreeNode node, FlowDocument doc)
        {
            string nodeName = ValidatorUtilities.GetTreeNodeName(node);
            Paragraph paragraph = new Paragraph();
            paragraph.Inlines.Add(new Bold(new Run(string.Format("<{0}>", nodeName))));
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

            //doc.Blocks.Add(paragraph);

            foreach (TreeNode child in node.Children.ContentsAs_YieldEnumerable)
            {
                //Paragraph childXmlPara = new Paragraph();
                string childNodeText = ValidatorUtilities.GetTreeNodeTextExcerpt(child);
                string childNodeName = ValidatorUtilities.GetTreeNodeName(child);
                paragraph.Inlines.Add(new LineBreak());
                //spaces = indent
                paragraph.Inlines.Add(new Bold(new Run(string.Format("  <{0}>", childNodeName))));
                paragraph.Inlines.Add(new Run(childNodeText));
                paragraph.Inlines.Add(new Bold(new Run(string.Format("</{0}>", childNodeName))));
                //doc.Blocks.Add(childXmlPara);
            }
            //Paragraph closingNodePara = new Paragraph();
            paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Bold(new Run(string.Format("</{0}>", nodeName))));
            doc.Blocks.Add(paragraph);
        }
    }


    //also copied from ContentDocumentValidator
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
