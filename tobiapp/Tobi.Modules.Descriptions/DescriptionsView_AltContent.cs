﻿using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common.UI;
using urakawa.daisy;
using urakawa.property.alt;

namespace Tobi.Plugin.Descriptions
{
    public partial class DescriptionsView
    {


        private void OnClick_ButtonGoAdvanced(object sender, RoutedEventArgs e)
        {
            forceRefreshDataUI();
            m_ViewModel.ShowAdvancedEditor = true;
        }
        private void OnClick_ButtonGoBasic(object sender, RoutedEventArgs e)
        {
            forceRefreshDataUI();
            m_ViewModel.ShowAdvancedEditor = false;
        }

        private void OnSelectionChanged_DescriptionsList(object sender, SelectionChangedEventArgs e)
        {
            m_ViewModel.SetSelectedAlternateContent((AlternateContent)DescriptionsListView.SelectedItem);
            resetAudioPlayer();
        }


        private bool isUniqueIdInvalid(string txt, string promptedTxt)
        {
            return txt == "" || txt == promptedTxt;
        }
        private bool isDescriptionNameInvalid(string txt, string promptedTxt)
        {
            return txt == "" || txt == promptedTxt;
        }

        private void OnClick_ButtonAddDescription(object sender, RoutedEventArgs e)
        {
            string txt = ""; // PROMPT_DescriptionName;
            string descriptionName = "";
            while (descriptionName != null && isDescriptionNameInvalid(descriptionName, "")) // PROMPT_DescriptionName))
            {
                // returns null only when dialog is cancelled, otherwise trimmed string (potentially empty)
                descriptionName = showLineEditorPopupDialog(txt, "DIAGRAM element name", DiagramContentModelHelper.DIAGRAM_ElementNames);
                txt = descriptionName;
            }
            if (descriptionName == null) return;

            txt = ""; // PROMPT_ID;
            string uid = "";
            while (uid != null && isUniqueIdInvalid(uid, "")) // PROMPT_ID))
            {
                // returns null only when dialog is cancelled, otherwise trimmed string (potentially empty)
                uid = showLineEditorPopupDialog(txt, "Unique identifier", null);
                txt = uid;
            }
            if (uid == null) return;

            m_ViewModel.AddDescription(uid, descriptionName);

            DescriptionsListView.Items.Refresh();
            DescriptionsListView.SelectedIndex = DescriptionsListView.Items.Count - 1;
            //FocusHelper.FocusBeginInvoke(DescriptionsListView);

            MetadatasAltContentListView.Items.Refresh();
            if (MetadatasAltContentListView.Items.Count > 0)
            {
                MetadatasAltContentListView.SelectedIndex = MetadatasAltContentListView.Items.Count - 1;
            }
            //FocusHelper.FocusBeginInvoke(MetadatasAltContentListView);

            //BindingExpression be = DescriptionTextBox.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            //if (be != null) be.UpdateTarget();
        }

        private void OnClick_ButtonRemoveDescription(object sender, RoutedEventArgs e)
        {
            int selectedIndex = DescriptionsListView.SelectedIndex;
            if (selectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            m_ViewModel.RemoveDescription(altContent);

            DescriptionsListView.Items.Refresh();
            if (DescriptionsListView.Items.Count > 0)
            {
                DescriptionsListView.SelectedIndex = selectedIndex < DescriptionsListView.Items.Count
                                                                ? selectedIndex
                                                                : DescriptionsListView.Items.Count - 1;
            }
            //FocusHelper.FocusBeginInvoke(DescriptionsListView);
        }


        private string showLineEditorPopupDialog(string editedText, string dialogTitle, List<string> predefinedCandidates)
        {
            m_Logger.Log("showTextEditorPopupDialog", Category.Debug, Priority.Medium);

            if (predefinedCandidates == null)
            {
                var editBox = new TextBoxReadOnlyCaretVisible
                {
                    FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                    //Watermark = TEXTFIELD_WATERMARK,
                    Text = editedText,
                    TextWrapping = TextWrapping.NoWrap,
                    AcceptsReturn = false
                };

                var panel = new StackPanel();
                panel.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
                panel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
                editBox.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
                panel.Children.Add(editBox);

                var windowPopup = new PopupModalWindow(m_ShellView,
                                                       dialogTitle,
                                                       panel,
                                                       PopupModalWindow.DialogButtonsSet.OkCancel,
                                                       PopupModalWindow.DialogButton.Ok,
                                                       true, 300, 160, null, 40);

                windowPopup.EnableEnterKeyDefault = true;

                editBox.SetValue(AutomationProperties.NameProperty, dialogTitle);

                editBox.Loaded += new RoutedEventHandler((sender, ev) =>
                {
                    editBox.SelectAll();
                    FocusHelper.FocusBeginInvoke(editBox);
                });

                WatermarkTextBoxBehavior.SetEnableWatermark(editBox, true);
                WatermarkTextBoxBehavior.SetLabel(editBox, TEXTFIELD_WATERMARK);

                Style style = (Style)Application.Current.Resources[@"WatermarkTextBoxStyle"];
                WatermarkTextBoxBehavior.SetLabelStyle(editBox, style);

                windowPopup.ShowModal();

                WatermarkTextBoxBehavior.SetEnableWatermark(editBox, false);

                if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
                {
                    string str = editBox.Text == null ? "" : editBox.Text.Trim();
                    //if (string.IsNullOrEmpty(str))
                    //{
                    //    return "";
                    //}
                    return str;
                }

                return null;
            }
            else
            {
                var editBoxCombo_Name = new ComboBox //WithAutomationPeer
                {
                    FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                    Text = editedText,
                    IsEditable = true,
                    IsTextSearchEnabled = true,
#if NET40
                    IsTextSearchCaseSensitive = false
#endif //NET40
                };

                editBoxCombo_Name.ItemsSource = predefinedCandidates;

                var panel = new StackPanel();
                panel.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
                panel.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
                editBoxCombo_Name.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
                panel.Children.Add(editBoxCombo_Name);

                var windowPopup = new PopupModalWindow(m_ShellView,
                                                       dialogTitle,
                                                       panel,
                                                       PopupModalWindow.DialogButtonsSet.OkCancel,
                                                       PopupModalWindow.DialogButton.Ok,
                                                       true, 300, 160, null, 40);

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

                WatermarkComboBoxBehavior.SetEnableWatermark(editBoxCombo_Name, true);
                WatermarkComboBoxBehavior.SetLabel(editBoxCombo_Name, TEXTFIELD_WATERMARK);

                Style style = (Style)Application.Current.Resources[@"WatermarkTextBoxStyle"];
                WatermarkComboBoxBehavior.SetLabelStyle(editBoxCombo_Name, style);

                windowPopup.ShowModal();

                WatermarkComboBoxBehavior.SetEnableWatermark(editBoxCombo_Name, false);

                if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
                {
                    string str = editBoxCombo_Name.Text == null ? "" : editBoxCombo_Name.Text.Trim();
                    //if (string.IsNullOrEmpty(str))
                    //{
                    //    return "";
                    //}
                    return str;
                }

                return null;
            }
        }



    }
}
