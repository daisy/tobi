using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Win32;
using urakawa.daisy;
using urakawa.data;
using urakawa.property.alt;

namespace Tobi.Plugin.Descriptions
{
    public partial class DescriptionsView
    {


        private void OnClick_ButtonOpenImage(object sender, RoutedEventArgs e)
        {
            m_Logger.Log("DescriptionImage.OpenFileDialog", Category.Debug, Priority.Medium);

            var dlg = new OpenFileDialog
            {

                FileName = "",
                DefaultExt = DataProviderFactory.IMAGE_JPG_EXTENSION,
                Filter = @"JPEG, PNG, BMP, GIF, SVG (*" + DataProviderFactory.IMAGE_JPEG_EXTENSION + ", *" + DataProviderFactory.IMAGE_JPG_EXTENSION + ", *" + DataProviderFactory.IMAGE_PNG_EXTENSION + ", *" + DataProviderFactory.IMAGE_BMP_EXTENSION + ", *" + DataProviderFactory.IMAGE_GIF_EXTENSION + ", *" + DataProviderFactory.IMAGE_SVG_EXTENSION + ", *" + DataProviderFactory.IMAGE_SVGZ_EXTENSION + ")|*" + DataProviderFactory.IMAGE_JPEG_EXTENSION + ";*" + DataProviderFactory.IMAGE_JPG_EXTENSION + ";*" + DataProviderFactory.IMAGE_PNG_EXTENSION + ";*" + DataProviderFactory.IMAGE_BMP_EXTENSION + ";*" + DataProviderFactory.IMAGE_GIF_EXTENSION + ";*" + DataProviderFactory.IMAGE_SVG_EXTENSION + ";*" + DataProviderFactory.IMAGE_SVGZ_EXTENSION + "",
                CheckFileExists = false,
                CheckPathExists = false,
                AddExtension = true,
                DereferenceLinks = true,
                Title = "Tobi: " + "Open image"
            };

            bool? result = false;

            m_ShellView.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

            if (result == false)
            {
                return;
            }

            string fullPath = "";
            fullPath = dlg.FileName;

            if (string.IsNullOrEmpty(fullPath)) return;

            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            m_ViewModel.SetDescriptionImage(altContent, fullPath);

            forceRefreshUI_Image();
        }

        private void OnClick_ButtonOpenImage_Specific(string diagramElementName)
        {
            bool descWasAdded = false;
            AlternateContent altContent = m_ViewModel.GetAltContent(diagramElementName);
            if (altContent == null)
            {
                string uid = m_ViewModel.GetNewXmlID(diagramElementName.Replace(':', '_'));
                altContent = addNewDescription(uid, diagramElementName);
                descWasAdded = true;
            }

            DebugFix.Assert(altContent != null);
            DescriptionsListView.SelectedItem = altContent;
            DebugFix.Assert(DescriptionsListView.SelectedItem == altContent);

            OnClick_ButtonOpenImage(null, null);

            if (descWasAdded && (altContent.Image == null || altContent.Image.ImageMediaData == null))
            {
                m_ViewModel.RemoveDescription(altContent);
            }
        }

        private void OnClick_ButtonOpenImage_Tactile(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonOpenImage_Specific(DiagramContentModelHelper.D_Tactile);
        }
        private void OnClick_ButtonOpenImage_Simplified(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonOpenImage_Specific(DiagramContentModelHelper.D_SimplifiedImage);
        }


        private void OnClick_ButtonClearImage(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            m_ViewModel.SetDescriptionImage(altContent, null);

            forceRefreshUI_Image();
        }

        private void OnClick_ButtonClearImage_Specific(string diagramElementName)
        {
            AlternateContent altContent = m_ViewModel.GetAltContent(diagramElementName);
            DebugFix.Assert(altContent != null);

            DescriptionsListView.SelectedItem = altContent;
            DebugFix.Assert(DescriptionsListView.SelectedItem == altContent);

            OnClick_ButtonClearImage(null, null);
        }

        private void OnClick_ButtonClearImage_Tactile(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonClearImage_Specific(DiagramContentModelHelper.D_Tactile);
        }
        private void OnClick_ButtonClearImage_Simplified(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonClearImage_Specific(DiagramContentModelHelper.D_SimplifiedImage);
        }
        private void forceRefreshUI_Image()
        {
            DescriptionsListView.Items.Refresh();

            BindingExpression be = DescriptionImage.GetBindingExpression(Image.SourceProperty);
            if (be != null) be.UpdateTarget();

            BindingExpression be1 = DescriptionImage_Simplified.GetBindingExpression(Image.SourceProperty);
            if (be1 != null) be1.UpdateTarget();

            BindingExpression be2 = DescriptionImage_Tactile.GetBindingExpression(Image.SourceProperty);
            if (be2 != null) be2.UpdateTarget();
        }
    }
}
