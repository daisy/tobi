using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.Practices.Composite.Logging;
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

        private bool showMetadataAttributeEditorPopupDialog(string label1, string label2, MetadataAttribute metadataAttr, out string newName, out string newValue, bool isAltContentMetadata, bool isOptionalAttributes, bool invalidSyntax)
        {
            m_Logger.Log("Descriptions.MetadataAttributeEditor", Category.Debug, Priority.Medium);

            var label_Name = new TextBlock
            {
                Text = label1 + ": ",
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var label_Value = new TextBlock
            {
                Text = label2 + ": ",
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            //var editBox_Name = new TextBox
            //{
            //    Text = metadataAttr.Name,
            //    TextWrapping = TextWrapping.WrapWithOverflow
            //};

            var editBoxCombo_Name = new ComboBox //WithAutomationPeer
            {
                FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                Text = metadataAttr.Name,
                IsEditable = true,
                IsTextSearchEnabled = true,
#if NET40
                IsTextSearchCaseSensitive = false
#endif //NET40
            };


            //var binding = new Binding
            //{
            //    Mode = BindingMode.OneWay,
            //    Source = new RelativeSource(RelativeSourceMode.Self),
            //    Path = new PropertyPath("SelectedItem")
            //};
            ////var expr = editBoxCombo_Name.SetBinding(AutomationProperties.NameProperty, binding);
            //editBoxCombo_Name.SetValue(AutomationProperties.NameProperty, "daniel");

            //editBoxCombo_Name.SelectionChanged += new SelectionChangedEventHandler(
            //    (object sender, SelectionChangedEventArgs e) =>
            //    {
            //        //var expr = editBoxCombo_Name.GetBindingExpression(AutomationProperties.NameProperty);
            //        //expr.UpdateTarget();
            //        //editBoxCombo_Name.NotifyScreenReaderAutomationIfKeyboardFocused();

            //        //var txt = editBoxCombo_Name.Text;
            //        //editBoxCombo_Name.Text = "mike";
            //        //editBoxCombo_Name.Text = txt;

            //        editBoxCombo_Name.NotifyScreenReaderAutomation();

            //        m_Logger.Log("UP TRAGET", Category.Debug, Priority.High);

            //        }
            //    );

            var list = new List<String>();
            if (isAltContentMetadata)
            {
                list.AddRange(DiagramContentModelHelper.DIAGRAM_ElementAttributes);
                
                list.Add(DiagramContentModelHelper.NA);

#if true || SUPPORT_ANNOTATION_ELEMENT
                list.Add(DiagramContentModelHelper.Ref);
                list.Add(DiagramContentModelHelper.Role);
                list.Add(DiagramContentModelHelper.By);
#endif //SUPPORT_ANNOTATION_ELEMENT
            }
            else
            {
                if (isOptionalAttributes)
                {
                    list.AddRange(DiagramContentModelHelper.DIAGRAM_MetadataAdditionalAttributeNames);
                }
                else
                {
                    list.AddRange(DiagramContentModelHelper.DIAGRAM_MetadataProperties);
                }
            }
            editBoxCombo_Name.ItemsSource = list;

            //    col = new ObservableCollection<string> { "Eric", "Phillip" };
            //combo.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = col });

            var editBox_Value = new TextBoxReadOnlyCaretVisible
            {
                //Watermark = TEXTFIELD_WATERMARK,
                Text = metadataAttr.Value,
                TextWrapping = TextWrapping.WrapWithOverflow
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var panelName = new DockPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                LastChildFill = true
            };
            panelName.Margin = new Thickness(0, 0, 0, 8);
            label_Name.SetValue(DockPanel.DockProperty, Dock.Left);
            panelName.Children.Add(label_Name);
            panelName.Children.Add(editBoxCombo_Name);

            var panelValue = new DockPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };
            label_Value.SetValue(DockPanel.DockProperty, Dock.Left);
            panelValue.Children.Add(label_Value);
            panelValue.Children.Add(editBox_Value);

            panel.Children.Add(panelName);
            panel.Children.Add(panelValue);

            if (invalidSyntax)
            {
                var msg = new TextBlock(new Run("(invalid syntax)"))
                {
                    Margin = new Thickness(0, 6, 0, 0),
                    Focusable = true,
                    FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],
                };
                panel.Children.Add(msg);
            }

            //var details = new TextBoxReadOnlyCaretVisible
            //                  {
            //    TextReadOnly = Tobi_Lang.ExitConfirm
            //};

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic("Edit attribute"),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 310, 200, null, 40);

            windowPopup.EnableEnterKeyDefault = true;

            editBoxCombo_Name.Loaded += new RoutedEventHandler((sender, ev) =>
            {
                var textBox = ComboBoxWithAutomationPeer.GetTextBox(editBoxCombo_Name);
                if (textBox != null)
                {
                    textBox.FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"];
                    textBox.SelectAll();
                }

                FocusHelper.FocusBeginInvoke(editBoxCombo_Name);
            });
            editBox_Value.Loaded += new RoutedEventHandler((sender, ev) =>
            {
                editBox_Value.SelectAll();
                //FocusHelper.FocusBeginInvoke(editBox_Name);
            });

            WatermarkComboBoxBehavior.SetEnableWatermark(editBoxCombo_Name, true);
            WatermarkComboBoxBehavior.SetLabel(editBoxCombo_Name, TEXTFIELD_WATERMARK);

            Style style = (Style)Application.Current.Resources[@"WatermarkTextBoxStyle"];
            WatermarkComboBoxBehavior.SetLabelStyle(editBoxCombo_Name, style);


            WatermarkTextBoxBehavior.SetEnableWatermark(editBox_Value, true);
            WatermarkTextBoxBehavior.SetLabel(editBox_Value, TEXTFIELD_WATERMARK);

            //Style style = (Style)Application.Current.Resources[@"WatermarkTextBoxStyle"];
            WatermarkTextBoxBehavior.SetLabelStyle(editBox_Value, style);


            windowPopup.ShowModal();

            WatermarkComboBoxBehavior.SetEnableWatermark(editBoxCombo_Name, false);
            WatermarkTextBoxBehavior.SetEnableWatermark(editBox_Value, false);

            if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                newName = editBoxCombo_Name.Text.Trim();
                newValue = editBox_Value.Text.Trim();

                return true;
            }

            newName = null;
            newValue = null;

            return false;
        }


        private void OnKeyDown_ListItemMetadata(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            // We capture only the RETURN KeyUp bubbling-up from UI descendants
            if (key != Key.Return) // || !(sender is ListViewItem))
            {
                return;
            }

            OnMouseDoubleClick_ListItemMetadata(null, null);

            // We void the effect of the RETURN key
            // (which would normally close the parent dialog window by activating the default button: CANCEL)
            e.Handled = true;
        }

        private void OnKeyDown_ListItemMetadataAttr(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            // We capture only the RETURN KeyUp bubbling-up from UI descendants
            if (key != Key.Return) // || !(sender is ListViewItem))
            {
                return;
            }

            OnMouseDoubleClick_ListItemMetadataAttr(null, null);

            // We void the effect of the RETURN key
            // (which would normally close the parent dialog window by activating the default button: CANCEL)
            e.Handled = true;
        }

        private void OnClick_ButtonEditMetadata(object sender, RoutedEventArgs e)
        {
            if (MetadatasListView.SelectedIndex >= 0)
                OnMouseDoubleClick_ListItemMetadata(null, null);
        }
        private void OnClick_ButtonEditMetadataAttr(object sender, RoutedEventArgs e)
        {
            if (MetadataAttributesListView.SelectedIndex >= 0)
                OnMouseDoubleClick_ListItemMetadataAttr(null, null);
        }


        private void OnMouseDoubleClick_ListItemMetadata(object sender, MouseButtonEventArgs e)
        {
            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (MetadatasListView.SelectedIndex < 0) return;
            Metadata md = (Metadata)MetadatasListView.SelectedItem;
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
                ok = showMetadataAttributeEditorPopupDialog("Property", "Content", mdAttrTEMP, out newName, out newValue, false, false, invalidSyntax);

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

            //bool ok = showMetadataAttributeEditorPopupDialog(md.NameContentAttribute, out newName, out newValue, false);
            //if (ok &&
            //    (newName != md.NameContentAttribute.Name || newValue != md.NameContentAttribute.Value))
            //{
            m_ViewModel.SetMetadataAttr(altProp, null, md, null, newName, newValue);

            MetadatasListView.Items.Refresh();
        }

        private void OnMouseDoubleClick_ListItemMetadataAttr(object sender, MouseButtonEventArgs e)
        {
            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (MetadatasListView.SelectedIndex < 0) return;
            Metadata md = (Metadata)MetadatasListView.SelectedItem;
            MetadataAttribute mdAttr = (MetadataAttribute)MetadataAttributesListView.SelectedItem;
            string newName = null;
            string newValue = null;

            var mdAttrTEMP = new MetadataAttribute();
            mdAttrTEMP.Name = mdAttr.Name;
            mdAttrTEMP.Value = mdAttr.Value;

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
                ok = showMetadataAttributeEditorPopupDialog("Name", "Value", mdAttrTEMP, out newName, out newValue, false, true, invalidSyntax);

                if (!ok)
                {
                    return;
                }
                else if (newName == mdAttr.Name
                    && newValue == mdAttr.Value)
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


            //bool ok = showMetadataAttributeEditorPopupDialog(mdAttr, out newName, out newValue, false);
            //if (ok &&
            //    (newName != mdAttr.Name || newValue != mdAttr.Value))
            //{
            m_ViewModel.SetMetadataAttr(altProp, null, md, mdAttr, newName, newValue);

            MetadataAttributesListView.Items.Refresh();
        }


        private void OnClick_ButtonRemoveMetadata(object sender, RoutedEventArgs e)
        {
            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            //if (DescriptionsListView.SelectedIndex < 0) return;
            //AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            //if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            int selectedIndex = MetadatasListView.SelectedIndex;
            if (selectedIndex < 0) return;
            m_ViewModel.RemoveMetadata(altProp, null, (Metadata)MetadatasListView.SelectedItem);
            MetadatasListView.Items.Refresh();
            if (MetadatasListView.Items.Count > 0)
            {
                MetadatasListView.SelectedIndex = selectedIndex < MetadatasListView.Items.Count
                                                                ? selectedIndex
                                                                : MetadatasListView.Items.Count - 1;
            }
            //FocusHelper.FocusBeginInvoke(MetadatasListView);
        }

        private void OnClick_ButtonAddMetadata(object sender, RoutedEventArgs e)
        {
            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetOrCreateAlternateContentProperty();
            //if (altProp == null) return;

            //if (DescriptionsListView.SelectedIndex < 0) return;
            //AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            //if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

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
                ok = showMetadataAttributeEditorPopupDialog("Property", "Content", mdAttr, out newName, out newValue, false, false, invalidSyntax);
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

            //bool ok = showMetadataAttributeEditorPopupDialog(mdAttr, out newName, out newValue, false);
            //if (ok &&
            //    newName != mdAttr.Name && newValue != mdAttr.Value)
            //{
            m_ViewModel.AddMetadata(altProp, null, newName, newValue);
            MetadatasListView.Items.Refresh();
            MetadatasListView.SelectedIndex = MetadatasListView.Items.Count - 1;
            //FocusHelper.FocusBeginInvoke(MetadatasListView);
        }

        private void OnClick_ButtonRemoveMetadataAttr(object sender, RoutedEventArgs e)
        {
            int selectedIndex = MetadatasListView.SelectedIndex;
            if (selectedIndex < 0) return;
            Metadata md = (Metadata)MetadatasListView.SelectedItem;

            selectedIndex = MetadataAttributesListView.SelectedIndex;
            if (selectedIndex < 0) return;

            m_ViewModel.RemoveMetadataAttr(md, (MetadataAttribute)MetadataAttributesListView.SelectedItem);

            MetadataAttributesListView.Items.Refresh();
            if (MetadataAttributesListView.Items.Count > 0)
            {
                MetadataAttributesListView.SelectedIndex = selectedIndex < MetadataAttributesListView.Items.Count
                                                                ? selectedIndex
                                                                : MetadataAttributesListView.Items.Count - 1;
            }
            //FocusHelper.FocusBeginInvoke(MetadataAttributesListView);
        }


        private void OnClick_ButtonAddMetadataAttr(object sender, RoutedEventArgs e)
        {
            if (MetadatasListView.SelectedIndex < 0) return;
            Metadata md = (Metadata)MetadatasListView.SelectedItem;

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
                ok = showMetadataAttributeEditorPopupDialog("Name", "Value", mdAttr, out newName, out newValue, false, true, invalidSyntax);
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

            //bool ok = showMetadataAttributeEditorPopupDialog(mdAttr, out newName, out newValue, false);
            //if (ok &&
            //    newName != mdAttr.Name && newValue != mdAttr.Value)
            //{
            m_ViewModel.AddMetadataAttr(md, newName, newValue);
            MetadataAttributesListView.Items.Refresh();
            MetadataAttributesListView.SelectedIndex = MetadataAttributesListView.Items.Count - 1;
            //FocusHelper.FocusBeginInvoke(MetadataAttributesListView);
        }

        private void OnSelectionChanged_MetadataList(object sender, SelectionChangedEventArgs e)
        {
            m_ViewModel.SetSelectedMetadata((Metadata)MetadatasListView.SelectedItem);
        }
    }
}
