using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common.UI;
using urakawa.daisy;
using urakawa.property.alt;

namespace Tobi.Plugin.Descriptions
{
    public partial class DescriptionsView
    {
        private void OnClick_ButtonEditText_Specific(string diagramElementName)
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

            OnClick_ButtonEditText(null, null);

            if (descWasAdded && (altContent.Text == null || string.IsNullOrEmpty(altContent.Text.Text)))
            {
                m_ViewModel.RemoveDescription(altContent);
            }
        }

        private void OnClick_ButtonEditText_LongDesc(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonEditText_Specific(DiagramContentModelHelper.D_LondDesc);
        }

        private void OnClick_ButtonEditText_Summary(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonEditText_Specific(DiagramContentModelHelper.D_Summary);
        }
        private void OnClick_ButtonEditText_SimplifiedLanguage(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonEditText_Specific(DiagramContentModelHelper.D_SimplifiedLanguageDescription);
        }
        private void OnClick_ButtonEditText_SimplifiedImage(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonEditText_Specific(DiagramContentModelHelper.D_SimplifiedImage);
        }

        private void OnClick_ButtonEditText_TactileImage(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonEditText_Specific(DiagramContentModelHelper.D_Tactile);
        }

        private void OnClick_ButtonEditText(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            string oldTxt = altContent.Text != null ? altContent.Text.Text : "";

            // returns null only when dialog is cancelled, otherwise trimmed string (potentially empty)
            string txt = showTextEditorPopupDialog(oldTxt, "Edit description text");

            if (txt == null) return; // cancel
            if (txt == "" || txt == oldTxt) return;

            m_ViewModel.SetDescriptionText(altContent, txt);

            forceRefreshUI();
        }

        private void forceRefreshUI()
        {
            DescriptionsListView.Items.Refresh();

            BindingExpression be = DescriptionTextBox.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be != null) be.UpdateTarget();

            BindingExpression be2 = DescriptionTextBox_LongDesc.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be2 != null) be2.UpdateTarget();

            BindingExpression be3 = DescriptionTextBox_Summary.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be3 != null) be3.UpdateTarget();

            BindingExpression be4 = DescriptionTextBox_SimplifiedLanguage.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be4 != null) be4.UpdateTarget();

            BindingExpression be5 = DescriptionTextBox_TactileImage.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be5 != null) be5.UpdateTarget();

            BindingExpression be6 = DescriptionTextBox_SimplifiedImage.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be6 != null) be6.UpdateTarget();
        }

        private void OnClick_ButtonClearText_Specific(string diagramElementName)
        {
            AlternateContent altContent = m_ViewModel.GetAltContent(diagramElementName);
            DebugFix.Assert(altContent != null);

            DescriptionsListView.SelectedItem = altContent;
            DebugFix.Assert(DescriptionsListView.SelectedItem == altContent);

            OnClick_ButtonClearText(null, null);
        }

        private void OnClick_ButtonRemoveText_Summary(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonClearText_Specific(DiagramContentModelHelper.D_Summary);
        }
        
        private void OnClick_ButtonRemoveText_LongDesc(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonClearText_Specific(DiagramContentModelHelper.D_LondDesc);
        }
        private void OnClick_ButtonRemoveText_SimplifiedLanguage(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonClearText_Specific(DiagramContentModelHelper.D_SimplifiedLanguageDescription);
        }

        private void OnClick_ButtonClearText_SimplifiedImage(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonClearText_Specific(DiagramContentModelHelper.D_SimplifiedImage);
        }

        private void OnClick_ButtonClearText_TactileImage(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonClearText_Specific(DiagramContentModelHelper.D_Tactile);
        }
        
        private void OnClick_ButtonClearText(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            m_ViewModel.SetDescriptionText(altContent, null);

            forceRefreshUI();
        }

        private string showTextEditorPopupDialog(string editedText, String dialogTitle)
        {
            m_Logger.Log("showTextEditorPopupDialog", Category.Debug, Priority.Medium);

            var editBox = new TextBoxReadOnlyCaretVisible
            {
                FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                Text = editedText,
                TextWrapping = TextWrapping.WrapWithOverflow,
                AcceptsReturn = true
            };

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   dialogTitle,
                                                   new ScrollViewer
                                                   {
                                                       Content = editBox,
                                                       HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                                                       VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                                                   },
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 350, 200, null, 40, m_DescriptionPopupModalWindow);

            windowPopup.EnableEnterKeyDefault = true;

            editBox.Loaded += new RoutedEventHandler((sender, ev) =>
            {
                editBox.SelectAll();
                FocusHelper.FocusBeginInvoke(editBox);
            });

            windowPopup.ShowModal();


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

        private void OnKeyUp_Tour(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None
                && e.Key == Key.Space)
            {
                OnClick_Tour(null, null);
            }
        }

        private void OnClick_Tour(object sender, RoutedEventArgs e)
        {
            //string uriStr = ((Hyperlink) sender).NavigateUri.ToString();
            Process.Start(Tobi_Plugin_Descriptions_Lang.TourHelpURI);
        }

        private void OnKeyUp_TactileImage(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None
                && e.Key == Key.Space)
            {
                OnClick_TactileImage(null, null);
            }
        }

        private void OnClick_TactileImage(object sender, RoutedEventArgs e)
        {
            //string uriStr = ((Hyperlink) sender).NavigateUri.ToString();
            Process.Start(Tobi_Plugin_Descriptions_Lang.TactileImageHelpURI);
        }

        private void OnKeyUp_SimplifiedImage(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None
                && e.Key == Key.Space)
            {
                OnClick_SimplifiedImage(null, null);
            }
        }

        private void OnClick_SimplifiedImage(object sender, RoutedEventArgs e)
        {
            //string uriStr = ((Hyperlink) sender).NavigateUri.ToString();
            Process.Start(Tobi_Plugin_Descriptions_Lang.SimplifiedImageHelpURI);
        }


        private void OnKeyUp_SimplifiedDescription(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None
                && e.Key == Key.Space)
            {
                OnClick_SimplifiedDescription(null, null);
            }
        }

        private void OnClick_SimplifiedDescription(object sender, RoutedEventArgs e)
        {
            //string uriStr = ((Hyperlink) sender).NavigateUri.ToString();
            Process.Start(Tobi_Plugin_Descriptions_Lang.SimplifiedDescriptionHelpURI);
        }

        private void OnKeyUp_SummaryDescription(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None
                && e.Key == Key.Space)
            {
                OnClick_SummaryDescription(null, null);
            }
        }

        private void OnClick_SummaryDescription(object sender, RoutedEventArgs e)
        {
            //string uriStr = ((Hyperlink) sender).NavigateUri.ToString();
            Process.Start(Tobi_Plugin_Descriptions_Lang.SummaryDescriptionHelpURI);
        }

        private void OnKeyUp_LongDescription(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None
                && e.Key == Key.Space)
            {
                OnClick_LongDescription(null, null);
            }
        }

        private void OnClick_LongDescription(object sender, RoutedEventArgs e)
        {
            //string uriStr = ((Hyperlink) sender).NavigateUri.ToString();
            Process.Start(Tobi_Plugin_Descriptions_Lang.LongDescriptionHelpURI);
        }
    }
}
