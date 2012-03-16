using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Win32;
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

            DescriptionsListView.Items.Refresh();

            BindingExpression be = DescriptionImage.GetBindingExpression(Image.SourceProperty);
            if (be != null) be.UpdateTarget();
        }

        private void OnClick_ButtonClearImage(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            m_ViewModel.SetDescriptionImage(altContent, null);

            DescriptionsListView.Items.Refresh();

            BindingExpression be = DescriptionImage.GetBindingExpression(Image.SourceProperty);
            if (be != null) be.UpdateTarget();
        }

    }
}
