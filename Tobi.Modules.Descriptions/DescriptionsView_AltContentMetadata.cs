using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tobi.Common;
using Tobi.Common.UI;
using urakawa.core;
using urakawa.daisy;
using urakawa.metadata;
using urakawa.property.alt;
using urakawa.xuk;

namespace Tobi.Plugin.Descriptions
{
    public partial class DescriptionsView
    {

        private void OnKeyDown_ListItemMetadataAltContent(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            // We capture only the RETURN KeyUp bubbling-up from UI descendants
            if (key != Key.Return) // || !(sender is ListViewItem))
            {
                return;
            }

            OnMouseDoubleClick_ListItemMetadataAltContent(null, null);

            // We void the effect of the RETURN key
            // (which would normally close the parent dialog window by activating the default button: CANCEL)
            e.Handled = true;
        }

        private void OnClick_ButtonEditMetadataAltContent(object sender, RoutedEventArgs e)
        {
            if (MetadatasAltContentListView.SelectedIndex >= 0)
                OnMouseDoubleClick_ListItemMetadataAltContent(null, null);
        }


        private void OnMouseDoubleClick_ListItemMetadataAltContent(object sender, MouseButtonEventArgs e)
        {
            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetAlternateContentProperty();
            if (altProp == null) return;

            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            if (MetadatasAltContentListView.SelectedIndex < 0) return;
            Metadata md = (Metadata)MetadatasAltContentListView.SelectedItem;
            string newName = null;
            string newValue = null;

            var mdAttrTEMP = new MetadataAttribute();
            mdAttrTEMP.Name = md.NameContentAttribute.Name;
            mdAttrTEMP.Value = md.NameContentAttribute.Value;

            bool invalidSyntax = false;
            bool ok = true;
            while (ok &&
                (
                invalidSyntax
                || string.IsNullOrEmpty(newName)
                || string.IsNullOrEmpty(newValue)
                )
            )
            {
                ok = showMetadataAttributeEditorPopupDialog("Name", "Value", mdAttrTEMP, out newName, out newValue, true, false, invalidSyntax);

                if (!ok)
                {
                    return;
                }
                else if (newName == md.NameContentAttribute.Name
                    && newValue == md.NameContentAttribute.Value)
                {
                    return;
                }

                mdAttrTEMP.Name = newName;
                mdAttrTEMP.Value = newValue;
                if (!string.IsNullOrEmpty(newName) && !string.IsNullOrEmpty(newValue))
                {
                    invalidSyntax =
                        m_ViewModel.IsIDInValid(newName)
                    || (
                    (newName.Equals(XmlReaderWriterHelper.XmlId) || newName.Equals(DiagramContentModelHelper.DiagramElementName))
                    && m_ViewModel.IsIDInValid(newValue)
                    );
                }
            }

            //bool ok = showMetadataAttributeEditorPopupDialog(md.NameContentAttribute, out newName, out newValue, true);
            //if (ok &&
            //    (newName != md.NameContentAttribute.Name || newValue != md.NameContentAttribute.Value))
            //{
            foreach (Metadata m in altContent.Metadatas.ContentsAs_Enumerable)
            {
                if (md == m)
                {
                    continue;
                }

                if (m.NameContentAttribute.Name == newName)
                {
                    var label = new TextBlock
                    {
                        Text = "This attribute already exists.",
                        Margin = new Thickness(8, 0, 8, 0),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Focusable = true,
                        TextWrapping = TextWrapping.Wrap
                    };

                    var iconProvider = new ScalableGreyableImageProvider(m_ShellView.LoadTangoIcon("dialog-warning"), m_ShellView.MagnificationLevel);

                    var panel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Stretch,
                    };
                    panel.Children.Add(iconProvider.IconLarge);
                    panel.Children.Add(label);
                    //panel.Margin = new Thickness(8, 8, 8, 0);

                    var windowPopup = new PopupModalWindow(m_ShellView,
                                                         UserInterfaceStrings.EscapeMnemonic("Duplicate attribute!"),
                                                         panel,
                                                         PopupModalWindow.DialogButtonsSet.Ok,
                                                         PopupModalWindow.DialogButton.Ok,
                                                         true, 300, 160, null, 0, m_DescriptionPopupModalWindow);
                    //view.OwnerWindow = windowPopup;


                    windowPopup.ShowModal();
                    return;
                }
            }

            m_ViewModel.SetMetadataAttr(null, altContent, md, null, newName, newValue);

            MetadatasAltContentListView.Items.Refresh();


            DescriptionsListView.Items.Refresh();
        }

        private void OnClick_ButtonRemoveMetadataAltContent(object sender, RoutedEventArgs e)
        {
            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetAlternateContentProperty();
            if (altProp == null) return;

            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            int selectedIndex = MetadatasAltContentListView.SelectedIndex;
            if (selectedIndex < 0) return;
            m_ViewModel.RemoveMetadata(null, altContent, (Metadata)MetadatasAltContentListView.SelectedItem);
            MetadatasAltContentListView.Items.Refresh();
            if (MetadatasAltContentListView.Items.Count > 0)
            {
                MetadatasAltContentListView.SelectedIndex = selectedIndex < MetadatasAltContentListView.Items.Count
                                                                ? selectedIndex
                                                                : MetadatasAltContentListView.Items.Count - 1;
            }
            //FocusHelper.FocusBeginInvoke(MetadatasAltContentListView);


            DescriptionsListView.Items.Refresh();
        }

        private void OnClick_ButtonAddMetadataAltContent(object sender, RoutedEventArgs e)
        {
            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetAlternateContentProperty();
            if (altProp == null) return;

            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            var mdAttr = new MetadataAttribute();
            mdAttr.Name = ""; // PROMPT_MD_NAME;
            mdAttr.Value = ""; // PROMPT_MD_VALUE;
            string newName = null;
            string newValue = null;

            bool invalidSyntax = false;
            bool ok = true;
            while (ok &&
                (
                invalidSyntax
                || string.IsNullOrEmpty(newName)
                || string.IsNullOrEmpty(newValue)
                //|| newName == PROMPT_MD_NAME
                //|| newValue == PROMPT_MD_VALUE
                )
            )
            {
                ok = showMetadataAttributeEditorPopupDialog("Name", "Value", mdAttr, out newName, out newValue, true, false, invalidSyntax);
                mdAttr.Name = newName;
                mdAttr.Value = newValue;
                if (!string.IsNullOrEmpty(newName) && !string.IsNullOrEmpty(newValue))
                {
                    invalidSyntax =
                        m_ViewModel.IsIDInValid(newName)
                    || (
                    (newName.Equals(XmlReaderWriterHelper.XmlId) || newName.Equals(DiagramContentModelHelper.DiagramElementName))
                    && m_ViewModel.IsIDInValid(newValue)
                    );
                }
            }
            if (!ok) return;

            //bool ok = showMetadataAttributeEditorPopupDialog(mdAttr, out newName, out newValue, true);
            //if (ok &&
            //    newName != mdAttr.Name && newValue != mdAttr.Value)
            //{
            foreach (Metadata m in altContent.Metadatas.ContentsAs_Enumerable)
            {
                if (m.NameContentAttribute.Name == newName)
                {
                    var label = new TextBlock
                    {
                        Text = "This attribute already exists.",
                        Margin = new Thickness(8, 0, 8, 0),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Focusable = true,
                        TextWrapping = TextWrapping.Wrap
                    };

                    var iconProvider = new ScalableGreyableImageProvider(m_ShellView.LoadTangoIcon("dialog-warning"), m_ShellView.MagnificationLevel);

                    var panel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Stretch,
                    };
                    panel.Children.Add(iconProvider.IconLarge);
                    panel.Children.Add(label);
                    //panel.Margin = new Thickness(8, 8, 8, 0);

                    var windowPopup = new PopupModalWindow(m_ShellView,
                                                         UserInterfaceStrings.EscapeMnemonic("Duplicate attribute!"),
                                                         panel,
                                                         PopupModalWindow.DialogButtonsSet.Ok,
                                                         PopupModalWindow.DialogButton.Ok,
                                                         true, 300, 160, null, 0, m_DescriptionPopupModalWindow);
                    //view.OwnerWindow = windowPopup;

                    windowPopup.ShowModal();
                    return;
                }
            }


            m_ViewModel.AddMetadata(null, altContent, newName, newValue);
            MetadatasAltContentListView.Items.Refresh();
            MetadatasAltContentListView.SelectedIndex = MetadatasAltContentListView.Items.Count - 1;
            //FocusHelper.FocusBeginInvoke(MetadatasAltContentListView);

            DescriptionsListView.Items.Refresh();
        }
    }
}
