using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Tobi.Common.MVVM;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.xuk;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Orientation = System.Windows.Controls.Orientation;
using ProgressBar = System.Windows.Controls.ProgressBar;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        public RichDelegateCommand SaveCommand { get; private set; }
        public RichDelegateCommand SaveAsCommand { get; private set; }

        private void initCommands_Save()
        {
            //
            SaveAsCommand = new RichDelegateCommand(
                Tobi_Plugin_Urakawa_Lang.SaveAs,
                Tobi_Plugin_Urakawa_Lang.SaveAs_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"document-save"),
                //ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save_As")),
                () =>
                {
                    if (DocumentProject == null)
                    {
                        return;
                    }

                    m_Logger.Log(@"UrakawaSession.saveAs", Category.Debug, Priority.Medium);

                    var dlg = new SaveFileDialog
                    {
                        FileName = @"tobi_doc",
                        DefaultExt = @".xuk",
                        Filter = @"XUK (*.xuk)|*.xuk",
                        CheckFileExists = false,
                        CheckPathExists = false,
                        AddExtension = true,
                        CreatePrompt = false,
                        DereferenceLinks = true,
                        OverwritePrompt = false,
                        Title = @"Tobi: " + UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.SaveAs)
                    };

                    bool? result = false;

                    m_ShellView.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

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

                    Uri oldUri = DocumentProject.Presentations.Get(0).RootUri;
                    string oldDataDir = DocumentProject.Presentations.Get(0).DataProviderManager.DataFileDirectory;

                    string dirPath = Path.GetDirectoryName(dlg.FileName);
                    string prefix = Path.GetFileNameWithoutExtension(dlg.FileName);

                    DocumentProject.Presentations.Get(0).DataProviderManager.SetDataFileDirectoryWithPrefix(prefix);
                    DocumentProject.Presentations.Get(0).RootUri = new Uri(dirPath + Path.DirectorySeparatorChar, UriKind.Absolute);

                    if (saveAs(dlg.FileName))
                    {
                        DocumentProject.Presentations.Get(0).RootUri = oldUri;

                        string datafolderPathSavedAs =
                            DocumentProject.Presentations.Get(0).DataProviderManager.DataFileDirectoryFullPath;
                        DocumentProject.Presentations.Get(0).DataProviderManager.DataFileDirectory = oldDataDir;

                        //TODO: add progress report
                        string datafolderPath = DocumentProject.Presentations.Get(0).DataProviderManager.CopyFileDataProvidersToDataFolderWithPrefix(dirPath, prefix);

                        Debug.Assert(datafolderPath == datafolderPathSavedAs);

                        if (askUserOpenSavedAs(dlg.FileName))
                        {
                            //CloseCommand.Execute();
                            //if (DocumentProject == null) //effectively closed
                            if (Close())
                            {
                                try
                                {
                                    OpenFile(dlg.FileName);

                                    Debug.Assert(!IsDirty);
                                }
                                catch (Exception ex)
                                {
                                    ExceptionHandler.Handle(ex, false, m_ShellView);
                                }
                            }
                        }
                    }
                    else
                    {
                        DocumentProject.Presentations.Get(0).RootUri = oldUri;
                        DocumentProject.Presentations.Get(0).DataProviderManager.DataFileDirectory = oldDataDir;
                    }

                    return;

                    //var fileDialog = Container.Resolve<IFileDialogService>();
                    //return fileDialog.SaveAs();
                },
                () => DocumentProject != null,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_SaveAs));

            m_ShellView.RegisterRichCommand(SaveAsCommand);
            //
            SaveCommand = new RichDelegateCommand(
                Tobi_Plugin_Urakawa_Lang.Save,
                Tobi_Plugin_Urakawa_Lang.Save_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"drive-harddisk"),
                //ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save")),
                () => save(),
                () => DocumentProject != null,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Save));

            m_ShellView.RegisterRichCommand(SaveCommand);
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

            m_Logger.Log(String.Format(@"UrakawaSession.saveas() [{0}]", m_SaveAsDocumentFilePath), Category.Debug, Priority.Medium);

            var backWorker = new BackgroundWorker
                {
                    WorkerSupportsCancellation = true,
                    WorkerReportsProgress = true
                };

            bool cancelFlag = false;
            int currentPercentage = 0;

            var uri = new Uri(m_SaveAsDocumentFilePath + SAVING_EXT, UriKind.Absolute);
            //DocumentProject.OpenXuk(uri);

            var action = new SaveXukAction(DocumentProject, DocumentProject, uri)
            {
                ShortDescription = "Saving XUK file...",                               // TODO LOCALIZE SaveXukFile
                LongDescription = "Serializing the document object model from the Urakawa SDK as XML content into a XUK file..."       // TODO LOCALIZE SerializeDOMIntoXukFile
            };

            action.Progress += (sender, e) =>
            {
                double val = e.Current;
                double max = e.Total;
                var percent = (int)((val / max) * 100);

                if (percent != currentPercentage)
                {
                    currentPercentage = percent;
                    backWorker.ReportProgress(currentPercentage);
                }

                if (cancelFlag)
                {
                    e.Cancel();
                }
            };
            action.Finished += (sender, e) =>
            {
                if (DocumentFilePath == m_SaveAsDocumentFilePath)
                {
                    File.Delete(DocumentFilePath);
                    File.Move(DocumentFilePath + SAVING_EXT, DocumentFilePath);

                    //File.Copy(DocumentFilePath + SAVING_EXT, DocumentFilePath);
                    //File.Delete(DocumentFilePath + SAVING_EXT);

                    DocumentProject.Presentations.Get(0).UndoRedoManager.SetDirtyMarker();
                }
                else
                {
                    if (File.Exists(m_SaveAsDocumentFilePath))
                    {
                        File.Delete(m_SaveAsDocumentFilePath);
                    }
                    File.Move(m_SaveAsDocumentFilePath + SAVING_EXT, m_SaveAsDocumentFilePath);
                }


                m_EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish("Saved.");            // TODO LOCALIZE Saved

                RaisePropertyChanged(() => IsDirty);
                //IsDirty = false;
            };
            action.Cancelled += (sender, e) =>
            {
                if (File.Exists(m_SaveAsDocumentFilePath + SAVING_EXT))
                {
                    File.Delete(m_SaveAsDocumentFilePath + SAVING_EXT);
                }

                RaisePropertyChanged(() => IsDirty);
                //IsDirty = true;

                backWorker.CancelAsync();
            };

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

            var details = new TextBoxReadOnlyCaretVisible(action.LongDescription);

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       Tobi_Plugin_Urakawa_Lang.RunningTask),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.Cancel,
                                                   PopupModalWindow.DialogButton.Cancel,
                                                   false, 500, 150, details, 80);

            Exception workException = null;
            backWorker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                //var dummy = (string)args.Argument;

                if (backWorker.CancellationPending)
                {
                    args.Cancel = true;
                    return;
                }

                action.Execute();

                args.Result = @"dummy result";
            };

            backWorker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
            {
                progressBar.Value = args.ProgressPercentage;
            };

            backWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                workException = args.Error;

                if (args.Cancelled)
                {
                    RaisePropertyChanged(() => IsDirty);
                    //IsDirty = true;
                    windowPopup.ForceClose(PopupModalWindow.DialogButton.Cancel);
                }
                else
                {
                    windowPopup.ForceClose(PopupModalWindow.DialogButton.ESC);
                }

                //var result = (string)args.Result;

                backWorker = null;
            };

            backWorker.RunWorkerAsync(@"dummy arg");
            windowPopup.ShowModal();

            if (workException != null)
            {
                throw workException;
            }

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Cancel)
            {
                cancelFlag = true;
                return false;
            }
            return true;
        }

        private bool askUserConfirmOverwriteFileFolder(string path, bool folder)
        {
            m_Logger.Log(@"UrakawaSession_Save.askUserConfirmOverwriteFileFolder", Category.Debug, Priority.Medium);


            var label = new TextBlock
            {
                Text = (folder ? Tobi_Plugin_Urakawa_Lang.OverwriteConfirm_Folder : Tobi_Plugin_Urakawa_Lang.OverwriteConfirm_File),
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var iconProvider = new ScalableGreyableImageProvider(m_ShellView.LoadTangoIcon(@"dialog-warning"),
                                                                 m_ShellView.MagnificationLevel);

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            panel.Children.Add(iconProvider.IconLarge);
            panel.Children.Add(label);
            //panel.Margin = new Thickness(8, 8, 8, 0);

            var details = new TextBoxReadOnlyCaretVisible("Path: " + path);

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       Tobi_Plugin_Urakawa_Lang.Overwrite),
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


        private bool askUserOpenSavedAs(string path)
        {
            m_Logger.Log(@"UrakawaSession_Save.askUserOpenSavedAs", Category.Debug, Priority.Medium);


            var label = new TextBlock
            {
                Text = Tobi_Plugin_Urakawa_Lang.OpenSaveAsQuestion,
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var iconProvider = new ScalableGreyableImageProvider(m_ShellView.LoadGnomeNeuIcon(@"Neu_dialog-question"),
                                                                 m_ShellView.MagnificationLevel);

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            panel.Children.Add(iconProvider.IconLarge);
            panel.Children.Add(label);
            //panel.Margin = new Thickness(8, 8, 8, 0);

            var details = new TextBoxReadOnlyCaretVisible("Path: " + path);

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       Tobi_Plugin_Urakawa_Lang.OpenSaveAsQuestion_),
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
