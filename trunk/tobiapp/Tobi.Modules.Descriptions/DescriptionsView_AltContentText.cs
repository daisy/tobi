using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

            DescriptionsListView.Items.Refresh();

            BindingExpression be = DescriptionTextBox.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be != null) be.UpdateTarget();

            BindingExpression be2 = DescriptionTextBox_LongDesc.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be2 != null) be2.UpdateTarget();

            BindingExpression be3 = DescriptionTextBox_Summary.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be3 != null) be3.UpdateTarget();

            BindingExpression be4 = DescriptionTextBox_SimplifiedLanguage.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be4 != null) be4.UpdateTarget();
        }

        private void OnClick_ButtonClearText_Specific(string diagramElementName)
        {
            AlternateContent altContent = m_ViewModel.GetAltContent(diagramElementName);
            DebugFix.Assert(altContent != null);

            DescriptionsListView.SelectedItem = altContent;
            DebugFix.Assert(DescriptionsListView.SelectedItem == altContent);

            OnClick_ButtonClearText(null, null);
        }

        private void OnClick_ButtonClearText_Summary(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonClearText_Specific(DiagramContentModelHelper.D_Summary);
        }
        
        private void OnClick_ButtonClearText_LongDesc(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonClearText_Specific(DiagramContentModelHelper.D_LondDesc);
        }
        private void OnClick_ButtonClearText_SimplifiedLanguage(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonClearText_Specific(DiagramContentModelHelper.D_SimplifiedLanguageDescription);
        }
        
        private void OnClick_ButtonClearText(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            m_ViewModel.SetDescriptionText(altContent, null);

            DescriptionsListView.Items.Refresh();

            BindingExpression be = DescriptionTextBox.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be != null) be.UpdateTarget();

            BindingExpression be2 = DescriptionTextBox_LongDesc.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be2 != null) be2.UpdateTarget();

            BindingExpression be3 = DescriptionTextBox_Summary.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be3 != null) be3.UpdateTarget();

            BindingExpression be4 = DescriptionTextBox_SimplifiedLanguage.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be4 != null) be4.UpdateTarget();
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
                                                   true, 350, 200, null, 40);

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

    }
}
