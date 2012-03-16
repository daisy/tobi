using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common.UI;
using urakawa.property.alt;

namespace Tobi.Plugin.Descriptions
{
    public partial class DescriptionsView
    {

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
        }

        private void OnClick_ButtonClearText(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            m_ViewModel.SetDescriptionText(altContent, null);

            DescriptionsListView.Items.Refresh();

            BindingExpression be = DescriptionTextBox.GetBindingExpression(TextBoxReadOnlyCaretVisible.TextReadOnlyProperty);
            if (be != null) be.UpdateTarget();
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
                                                   new ScrollViewer { Content = editBox },
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 300, 160, null, 40);

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
