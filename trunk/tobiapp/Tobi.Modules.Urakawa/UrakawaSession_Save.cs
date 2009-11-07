using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using DaisyExport;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.events.progress;
using urakawa.xuk;
using HorizontalAlignment=System.Windows.HorizontalAlignment;
using Orientation=System.Windows.Controls.Orientation;
using ProgressBar=System.Windows.Controls.ProgressBar;
using SaveFileDialog=Microsoft.Win32.SaveFileDialog;

namespace Tobi.Modules.Urakawa
{
    public partial class UrakawaSession
    {
        private void initCommands_Save()
        {
            var shellPresenter = Container.Resolve<IShellPresenter>();
            //
            //
            ExportCommand = new RichDelegateCommand(
                UserInterfaceStrings.Export,
                UserInterfaceStrings.Export_,
                UserInterfaceStrings.Export_KEYS,
                shellPresenter.LoadTangoIcon("emblem-symbolic-link"),
                //ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save_As")),
                ()=>
                {
                    if (DocumentProject == null)
                    {
                        return;
                    }

                    Logger.Log("UrakawaSession.Export", Category.Debug, Priority.Medium);

                    var dlg = new FolderBrowserDialog
                    {
                        ShowNewFolderButton = true,
                        Description = "Tobi: " + UserInterfaceStrings.EscapeMnemonic(UserInterfaceStrings.Export)
                    };

                    var shellPresenter_ = Container.Resolve<IShellPresenter>();

                    DialogResult result = DialogResult.Abort;

                    shellPresenter_.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

                    if (result != DialogResult.OK && result != DialogResult.Yes)
                    {
                        return;
                    }

                    if (Directory.Exists(dlg.SelectedPath))
                    {
                        if (!askUserConfirmOverwriteFileFolder(dlg.SelectedPath, true))
                        {
                            return;
                        }

                        Directory.Delete(dlg.SelectedPath, true);
                        Directory.CreateDirectory(dlg.SelectedPath);
                    }

                    var exporter = new DAISY3Export(DocumentProject.Presentations.Get(0));
                    exporter.ExportToDaisy3(dlg.SelectedPath);
                },
                () => DocumentProject != null);

            shellPresenter.RegisterRichCommand(ExportCommand);
            //
            SaveAsCommand = new RichDelegateCommand(
                UserInterfaceStrings.SaveAs,
                UserInterfaceStrings.SaveAs_,
                UserInterfaceStrings.SaveAs_KEYS,
                shellPresenter.LoadTangoIcon("document-save"),
                //ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save_As")),
                ()=>
                {
                    if (DocumentProject == null)
                    {
                        return;
                    }

                    Logger.Log("UrakawaSession.saveAs", Category.Debug, Priority.Medium);

                    var dlg = new SaveFileDialog
                    {
                        FileName = "tobi_doc",
                        DefaultExt = ".xuk",
                        Filter = "XUK (*.xuk)|*.xuk",
                        CheckFileExists = false,
                        CheckPathExists = false,
                        AddExtension = true,
                        CreatePrompt = false,
                        DereferenceLinks = true,
                        OverwritePrompt = false,
                        Title = "Tobi: " + UserInterfaceStrings.EscapeMnemonic(UserInterfaceStrings.SaveAs)
                    };

                    var shellPresenter_ = Container.Resolve<IShellPresenter>();

                    bool? result = false;

                    shellPresenter_.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

                    if (result == false)
                    {
                        return;
                    }

                    if (File.Exists(dlg.FileName))
                    {
                        if (!askUserConfirmOverwriteFileFolder(dlg.FileName, false))
                        {
                            return;
                        }
                    }

                    if (saveAs(dlg.FileName))
                    {
                        Debug.Assert(!IsDirty);

                        DocumentProject.Presentations.Get(0).DataProviderManager.AllowCopyDataOnUriChanged(true);
                        DocumentProject.Presentations.Get(0).RootUri = new Uri(Path.GetDirectoryName(dlg.FileName) + Path.DirectorySeparatorChar, UriKind.Absolute);
                        DocumentProject.Presentations.Get(0).DataProviderManager.AllowCopyDataOnUriChanged(false);

                        IsDirty = false;
                        CloseCommand.Execute();

                        openFile(dlg.FileName);
                    }

                    return;

                    //var fileDialog = Container.Resolve<IFileDialogService>();
                    //return fileDialog.SaveAs();
                },
                () => DocumentProject != null);

            shellPresenter.RegisterRichCommand(SaveAsCommand);
            //
            SaveCommand = new RichDelegateCommand(
                UserInterfaceStrings.Save,
                UserInterfaceStrings.Save_,
                UserInterfaceStrings.Save_KEYS,
                shellPresenter.LoadTangoIcon("media-floppy"),
                //ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save")),
                ()=> save(),
                ()=> DocumentProject != null);

            shellPresenter.RegisterRichCommand(SaveCommand);
        }

        private BackgroundWorker m_SaveXukActionWorker;
        private bool m_SaveXukActionCancelFlag;
        private int m_SaveXukActionCurrentPercentage;

        private void OnSaveXukAction_cancelled(object sender, CancelledEventArgs e)
        {
            if (File.Exists(m_SaveAsDocumentFilePath + SAVING_EXT))
            {
                File.Delete(m_SaveAsDocumentFilePath + SAVING_EXT);
            }

            IsDirty = true;

            m_SaveXukActionWorker.CancelAsync();
        }

        private void OnSaveXukAction_finished(object sender, FinishedEventArgs e)
        {
            if (DocumentFilePath == m_SaveAsDocumentFilePath)
            {
                File.Delete(DocumentFilePath);
                File.Move(DocumentFilePath + SAVING_EXT, DocumentFilePath);

                //File.Copy(DocumentFilePath + SAVING_EXT, DocumentFilePath);
                //File.Delete(DocumentFilePath + SAVING_EXT);
            }
            else
            {
                if (File.Exists(m_SaveAsDocumentFilePath))
                {
                    File.Delete(m_SaveAsDocumentFilePath);
                }
                File.Move(m_SaveAsDocumentFilePath + SAVING_EXT, m_SaveAsDocumentFilePath);
                DocumentFilePath = m_SaveAsDocumentFilePath;
            }

            EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish("Saved.");

            IsDirty = false;
        }

        private void OnSaveXukAction_progress(object sender, ProgressEventArgs e)
        {
            double val = e.Current;
            double max = e.Total;
            var percent = (int)((val / max) * 100);

            if (percent != m_SaveXukActionCurrentPercentage)
            {
                m_SaveXukActionCurrentPercentage = percent;
                m_SaveXukActionWorker.ReportProgress(m_SaveXukActionCurrentPercentage);
            }

            if (m_SaveXukActionCancelFlag)
            {
                e.Cancel();
            }
        }

        private const string SAVING_EXT = ".SAVING";

        private bool save()
        {
            if (DocumentProject == null)
            {
                return false;
            }
            return saveAs(DocumentFilePath);
        }

        private string m_SaveAsDocumentFilePath;

        private bool saveAs(string filePath)
        {
            if (DocumentProject == null)
            {
                return false;
            }

            m_SaveAsDocumentFilePath = filePath;

            Logger.Log(String.Format("UrakawaSession.saveas() [{0}]", m_SaveAsDocumentFilePath), Category.Debug, Priority.Medium);

            m_SaveXukActionCancelFlag = false;
            m_SaveXukActionCurrentPercentage = 0;

            var uri = new Uri(m_SaveAsDocumentFilePath + SAVING_EXT, UriKind.Absolute);
            //DocumentProject.OpenXuk(uri);

            var action = new SaveXukAction(DocumentProject, DocumentProject, uri)
            {
                ShortDescription = "Saving XUK file...",
                LongDescription = "Serializing the document object model from the Urakawa SDK as XML content into a XUK file..."
            };

            action.Progress += OnSaveXukAction_progress;
            action.Finished += OnSaveXukAction_finished;
            action.Cancelled += OnSaveXukAction_cancelled;

            var shellPresenter = Container.Resolve<IShellPresenter>();
            //var window = shellPresenter.View as Window;

            var progressBar = new ProgressBar
            {
                IsIndeterminate = true,
                Height = 18,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };

            var label = new TextBlock
            {
                Text = action.ShortDescription,
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

            var details = new TextBoxReadOnlyCaretVisible(action.LongDescription)
            {
            };

            var windowPopup = new PopupModalWindow(shellPresenter,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       UserInterfaceStrings.RunningTask),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.Cancel,
                                                   PopupModalWindow.DialogButton.Cancel,
                                                   false, 500, 150, details, 80);

            m_SaveXukActionWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };

            m_SaveXukActionWorker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                var dummy = (string)args.Argument;

                if (m_SaveXukActionWorker.CancellationPending)
                {
                    args.Cancel = true;
                    return;
                }

                action.Execute();

                args.Result = "dummy result";
            };

            m_SaveXukActionWorker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
            {
                progressBar.Value = args.ProgressPercentage;
            };

            m_SaveXukActionWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                if (args.Cancelled)
                {
                    IsDirty = true;
                    windowPopup.ForceClose(PopupModalWindow.DialogButton.Cancel);
                }
                else
                {
                    windowPopup.ForceClose(PopupModalWindow.DialogButton.ESC);
                }

                var result = (string)args.Result;

                m_SaveXukActionWorker = null;
            };

            m_SaveXukActionWorker.RunWorkerAsync("dummy arg");
            windowPopup.ShowModal();

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Cancel)
            {
                m_SaveXukActionCancelFlag = true;
                return false;
            }
            return true;
        }

        private bool askUserConfirmOverwriteFileFolder(string path, bool folder)
        {
            Logger.Log("ShellPresenter.askUserConfirmExit", Category.Debug, Priority.Medium);

            var shellPresenter = Container.Resolve<IShellPresenter>();
            //var window = shellPresenter.View as Window;

            /*
            try
            {
                throw new ArgumentException("Opps !", new ArgumentOutOfRangeException("Oops 2 !!"));
            }
            catch (Exception ex)
            {
                App.handleException(ex);
            }*/

            var label = new TextBlock
            {
                Text = (folder ? UserInterfaceStrings.OverwriteConfirm_Folder : UserInterfaceStrings.OverwriteConfirm_File),
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var iconProvider = new ScalableGreyableImageProvider(shellPresenter.LoadTangoIcon("dialog-warning"))
                                   {
                                       IconDrawScale = shellPresenter.View.MagnificationLevel
                                   };
            //var zoom = (Double)Resources["MagnificationLevel"]; //Application.Current.

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            panel.Children.Add(iconProvider.IconLarge);
            panel.Children.Add(label);
            //panel.Margin = new Thickness(8, 8, 8, 0);

            var details = new TextBoxReadOnlyCaretVisible("Path: " + path)
            {
            };

            var windowPopup = new PopupModalWindow(shellPresenter,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       UserInterfaceStrings.Overwrite),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.YesNo,
                                                   PopupModalWindow.DialogButton.No,
                                                   false, 300, 160, details, 40);

            windowPopup.ShowModal();

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Yes)
            {
                return true;
            }

            return false;
        }
    }
}
