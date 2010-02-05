using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.UI;
using urakawa.daisy.import;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        private bool doImport()
        {
            m_Logger.Log(String.Format(@"UrakawaSession.doImport() [{0}]", DocumentFilePath), Category.Debug, Priority.Medium);

            var progressBar = new ProgressBar
            {
                IsIndeterminate = false,
                Height = 18,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            var progressBar2 = new ProgressBar
            {
                IsIndeterminate = false,
                Height = 18,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            var label = new TextBlock
            {
                Text = "Importing ...",
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Focusable = true,
            };
            var label2 = new TextBlock
            {
                Text = "...",
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Focusable = true,
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };

            panel.Children.Add(label);
            panel.Children.Add(progressBar);
            //panel.Children.Add(new TextBlock(new Run(" ")));
            //panel.Children.Add(label2);
            //panel.Children.Add(progressBar2);

            label2.Visibility = Visibility.Collapsed;
            progressBar2.Visibility = Visibility.Collapsed;

            //var details = new TextBoxReadOnlyCaretVisible("Converting data and building the in-memory document object model into the Urakawa SDK...");
            var details = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
            };
            details.Children.Add(label2);
            details.Children.Add(progressBar2);

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   "Importing ...",
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.Cancel,
                                                   PopupModalWindow.DialogButton.Cancel,
                                                   false, 500, 150, details, 80);

            var backWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };

            Daisy3_Import converter = null;

            backWorker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                //var dummy = (string)args.Argument;

                if (backWorker.CancellationPending)
                {
                    args.Cancel = true;
                    return;
                }

                converter = new Daisy3_Import(DocumentFilePath);

                converter.ProgressChangedEvent += (sender, e) =>
                   {
                       backWorker.ReportProgress(e.ProgressPercentage, e.UserState);
                   };

                converter.SubProgressChangedEvent += (sender, e) => Application.Current.Dispatcher.BeginInvoke((Action)(
                   () =>
                   {
                       if (e.ProgressPercentage < 0 && e.UserState == null)
                       {
                           progressBar2.Visibility = Visibility.Hidden;
                           label2.Visibility = Visibility.Hidden;
                           return;
                       }

                       if (progressBar2.Visibility != Visibility.Visible)
                           progressBar2.Visibility = Visibility.Visible;

                       if (label2.Visibility != Visibility.Visible)
                           label2.Visibility = Visibility.Visible;

                       if (e.ProgressPercentage < 0)
                       {
                           progressBar2.IsIndeterminate = true;
                       }
                       else
                       {
                           progressBar2.IsIndeterminate = false;
                           progressBar2.Value = e.ProgressPercentage;
                       }

                       label2.Text = (string)e.UserState;
                   }
                               ),
                       DispatcherPriority.Normal);

                converter.DoImport();

                args.Result = @"dummy result";
            };

            backWorker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
            {
                if (converter.RequestCancellation)
                {
                    return;
                }

                if (args.ProgressPercentage < 0)
                {
                    progressBar.IsIndeterminate = true;
                }
                else
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = args.ProgressPercentage;
                }

                label.Text = (string)args.UserState;
            };

            backWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                backWorker = null;

                if (converter.RequestCancellation || args.Cancelled)
                {
                    DocumentFilePath = null;
                    DocumentProject = null;
                    windowPopup.ForceClose(PopupModalWindow.DialogButton.Cancel);
                }
                else
                {
                    DocumentFilePath = converter.XukPath;
                    DocumentProject = converter.Project;
                    windowPopup.ForceClose(PopupModalWindow.DialogButton.ESC);
                }

                //var result = (string)args.Result;
            };

            backWorker.RunWorkerAsync(@"dummy arg");
            windowPopup.ShowModal();

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Cancel)
            {
                if (backWorker == null) return false;

                progressBar.IsIndeterminate = true;
                label.Text = "Please wait while cancelling...";

                progressBar2.Visibility = Visibility.Collapsed;
                label2.Visibility = Visibility.Collapsed;
                
                //details.Text = "Cancelling the current operation...";

                windowPopup = new PopupModalWindow(m_ShellView,
                                                       UserInterfaceStrings.EscapeMnemonic(
                                                           UserInterfaceStrings.CancellingTask),
                                                       panel,
                                                       PopupModalWindow.DialogButtonsSet.None,
                                                       PopupModalWindow.DialogButton.ESC,
                                                       false, 500, 150, null, 80);

                //m_OpenXukActionWorker.CancelAsync();
                converter.RequestCancellation = true;

                windowPopup.ShowModal();

                return false;
            }

            return true;
        }
    }
}
