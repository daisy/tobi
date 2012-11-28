using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AudioLib;
using Tobi.Common.MVVM;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.data;
using urakawa.events.progress;
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

        private bool askUserCleanup()
        {
            return askUser(
                UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.CmdDataCleanup_ShortDesc) + @"?",
                Tobi_Plugin_Urakawa_Lang.CmdDataCleanup_LongDesc);
        }

        private bool askUser(string message, string info)
        {
            m_Logger.Log("ShellView.askUserCleanup", Category.Debug, Priority.Medium);

            var label = new TextBlock
            {
                Text = message,
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var iconProvider = new ScalableGreyableImageProvider(
                m_ShellView.LoadTangoIcon("help-browser"),
                m_ShellView.MagnificationLevel);

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            panel.Children.Add(iconProvider.IconLarge);
            panel.Children.Add(label);
            //panel.Margin = new Thickness(8, 8, 8, 0);


            var details = new TextBoxReadOnlyCaretVisible
            {
                FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                TextReadOnly = info
            };

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   message,
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.YesNo,
                                                   PopupModalWindow.DialogButton.No,
                                                   true, 320, 160, details, 40, null);

            windowPopup.ShowModal();

            if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                return true;
            }

            return false;
        }

        private void initCommands_Save()
        {
            //
            SaveAsCommand = new RichDelegateCommand(
                Tobi_Plugin_Urakawa_Lang.CmdSaveAs_ShortDesc,
                Tobi_Plugin_Urakawa_Lang.CmdSaveAs_LongDesc,
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

                    bool doCleanup = askUserCleanup();
                    if (doCleanup)
                    {
                        DataCleanup(true);
                    }

                    var dlg = new SaveFileDialog
                    {
                        FileName = @"tobi_doc",
                        DefaultExt = OpenXukAction.XUK_EXTENSION,
                        Filter = @"XUK (*" + OpenXukAction.XUK_EXTENSION + ")|*" + OpenXukAction.XUK_EXTENSION,
                        CheckFileExists = false,
                        CheckPathExists = false,
                        AddExtension = true,
                        CreatePrompt = false,
                        DereferenceLinks = true,
                        OverwritePrompt = false,
                        Title = @"Tobi: " + UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.CmdSaveAs_ShortDesc)
                    };

                    bool? result = false;

                    m_ShellView.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

                    if (result == false)
                    {
                        return;
                    }

                    if (File.Exists(dlg.FileName))
                    {
                        if (!askUserConfirmOverwriteFileFolder(dlg.FileName, false, null))
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

                    if (saveAs(dlg.FileName, false))
                    {
                        DocumentProject.Presentations.Get(0).RootUri = oldUri;

                        //string datafolderPathSavedAs = DocumentProject.Presentations.Get(0).DataProviderManager.DataFileDirectoryFullPath;
                        DocumentProject.Presentations.Get(0).DataProviderManager.DataFileDirectory = oldDataDir;

                        //string datafolderPath = DocumentProject.Presentations.Get(0).DataProviderManager.CopyFileDataProvidersToDataFolderWithPrefix(dirPath, prefix);
                        //DebugFix.Assert(datafolderPath == datafolderPathSavedAs);


                        bool cancelled = false;

                        bool error = m_ShellView.RunModalCancellableProgressTask(true,
                            Tobi_Plugin_Urakawa_Lang.CopyingDataFiles,
                            new DataFolderCopier(DocumentProject.Presentations.Get(0), dirPath, prefix),
                            () =>
                            {
                                m_Logger.Log(@"CANCELED", Category.Debug, Priority.Medium);
                                cancelled = true;
                            },
                            () =>
                            {
                                m_Logger.Log(@"DONE", Category.Debug, Priority.Medium);
                                cancelled = false;
                            });

                        //DebugFix.Assert(outcome == !cancelled);

                        if (askUserOpenSavedAs(dlg.FileName))
                        {
                            try
                            {
                                OpenFile(dlg.FileName);
                            }
                            catch (Exception ex)
                            {
                                ExceptionHandler.Handle(ex, false, m_ShellView);
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
                Tobi_Plugin_Urakawa_Lang.CmdSave_ShortDesc,
                Tobi_Plugin_Urakawa_Lang.CmdSave_LongDesc,
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

        private bool saveAuto()
        {
            if (DocumentProject == null)
            {
                return false;
            }
            string autoSaveFilePath = Path.Combine(
                Path.GetDirectoryName(DocumentFilePath),
                Path.GetFileName(DocumentFilePath) + ".AUTOSAVE"
                );

            m_Logger.Log(String.Format(@"UrakawaSession.saveAuto() [{0}]", autoSaveFilePath), Category.Debug, Priority.Medium);

            return saveAs(autoSaveFilePath, true);
        }

        private bool save()
        {
            if (DocumentProject == null)
            {
                return false;
            }
            return saveAs(DocumentFilePath, false);
        }

        private string m_SaveAsDocumentFilePath;

        private bool saveAs(string filePath, bool autoSave)
        {
            if (DocumentProject == null)
            {
                return false;
            }

            m_SaveAsDocumentFilePath = filePath;

            m_Logger.Log(String.Format(@"UrakawaSession.saveas() [{0}]", m_SaveAsDocumentFilePath), Category.Debug, Priority.Medium);

            //var backWorker = new BackgroundWorker
            //    {
            //        WorkerSupportsCancellation = true,
            //        WorkerReportsProgress = true
            //    };

            //bool cancelFlag = false;
            //int currentPercentage = 0;

            var uri = new Uri(m_SaveAsDocumentFilePath + SAVING_EXT, UriKind.Absolute);
            //DocumentProject.OpenXuk(uri);

            DocumentProject.SetPrettyFormat(Settings.Default.XUK_PrettyFormat);

            var action = new SaveXukAction(DocumentProject, DocumentProject, uri)
            {
                ShortDescription = Tobi_Plugin_Urakawa_Lang.UrakawaSaveAction_ShortDesc,
                LongDescription = Tobi_Plugin_Urakawa_Lang.UrakawaSaveAction_LongDesc
            };

            bool cancelled = false;
            bool error = false;


            Action cancelledCallback =
                () =>
                {
                    cancelled = true;

                    RaisePropertyChanged(() => IsDirty);
                    //IsDirty = true;

                    //backWorker.CancelAsync();
                };

            Action finishedCallback =
                () =>
                {
                    cancelled = false;

                    if (DocumentFilePath == m_SaveAsDocumentFilePath)
                    {
                        DebugFix.Assert(!autoSave);

                        string saving = DocumentFilePath + SAVING_EXT;

                        if (File.Exists(saving))
                        {
                            SaveXukAction.Backup(DocumentFilePath);
                            File.Delete(DocumentFilePath);

                            File.Move(saving, DocumentFilePath);

                            //File.Copy(DocumentFilePath + SAVING_EXT, DocumentFilePath);
                            //File.Delete(DocumentFilePath + SAVING_EXT);

                            DocumentProject.Presentations.Get(0).UndoRedoManager.SetDirtyMarker();
                        }
                        else
                        {
#if DEBUG
                            Debugger.Break();
#endif

                            m_Logger.Log(@"UrakawaSession_Save SAVING NOT EXIST!!?? => " + saving, Category.Debug,
                                         Priority.Medium);
                        }
                    }
                    else
                    {
                        string saving = m_SaveAsDocumentFilePath + SAVING_EXT;

                        if (File.Exists(saving))
                        {
                            if (File.Exists(m_SaveAsDocumentFilePath))
                            {
                                if (!autoSave)
                                {
                                    SaveXukAction.Backup(m_SaveAsDocumentFilePath);
                                }
                                File.Delete(m_SaveAsDocumentFilePath);
                            }

                            File.Move(saving, m_SaveAsDocumentFilePath);
                        }
                        else
                        {
#if DEBUG
                            Debugger.Break();
#endif

                            m_Logger.Log(@"UrakawaSession_SaveAs SAVING NOT EXIST!!?? => " + saving, Category.Debug,
                                         Priority.Medium);
                        }
                    }


                    if (m_EventAggregator != null)
                    {
                        m_EventAggregator.GetEvent<StatusBarMessageUpdateEvent>()
                                         .Publish(Tobi_Plugin_Urakawa_Lang.Saved);
                    }

                    RaisePropertyChanged(() => IsDirty);
                    //IsDirty = false;
                };

            if (autoSave)
            {
                action.Progress += new EventHandler<ProgressEventArgs>(
                    delegate(object sender, ProgressEventArgs e)
                    {
                    }
                    );

                action.Finished += new EventHandler<FinishedEventArgs>(
                    delegate(object sender, FinishedEventArgs e)
                    {
                        finishedCallback();
                    }
                    );

                action.Cancelled += new EventHandler<CancelledEventArgs>(
                    delegate(object sender, CancelledEventArgs e)
                    {
                        cancelledCallback();
                    }
                    );

                try
                {
                    action.DoWork();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    //Console.WriteLine(ex.Message);
                    //Console.WriteLine(ex.StackTrace);

                    ExceptionHandler.Handle(ex, false, m_ShellView);

                    error = true;
                }
            }
            else
            {
                error = m_ShellView.RunModalCancellableProgressTask(true,
                    Tobi_Plugin_Urakawa_Lang.UrakawaSaveAction_ShortDesc, action,
                    cancelledCallback,
                    finishedCallback
                    );
            }

            string savingFile = m_SaveAsDocumentFilePath + SAVING_EXT;
            if (File.Exists(savingFile))
            {
                if (cancelled && !error)
                {
                    File.Delete(savingFile);
                }

                if (error)
                {
                    m_ShellView.ExecuteShellProcess(Path.GetDirectoryName(savingFile));
                }
            }

            return !cancelled;
        }

        public void messageBoxText(string title, string text, string info)
        {
            m_Logger.Log(@"UrakawaSession_Save.messageBoxText", Category.Debug, Priority.Medium);


            var label = new TextBlock
            {
                Text = text,
                Margin = new Thickness(8, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = FontWeights.Bold,
            };


            var textArea = new TextBoxReadOnlyCaretVisible()
            {
                FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                TextReadOnly = info,

                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),

                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };

            var scroll = new ScrollViewer
            {
                Content = textArea,

                Margin = new Thickness(6),
                VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            panel.Children.Add(label);
            //panel.Margin = new Thickness(8, 8, 8, 0);

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   title,
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.Ok,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 400, 140, scroll, 300, null);

            windowPopup.ShowModal();
        }

        public void messageBoxAlert(string message, Window owner)
        {
            m_Logger.Log(@"UrakawaSession_Save.messageBoxAlert", Category.Debug, Priority.Medium);


            var label = new TextBlock
            {
                Text = message,
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

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   "Warning",
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.Ok,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 600, 160, null, 40, owner);

            windowPopup.ShowModal();
        }

        public bool askUserConfirmOverwriteFileFolder(string path, bool folder, Window owner)
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

            var details = new TextBoxReadOnlyCaretVisible
            {
                FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                TextReadOnly = String.Format(Tobi_Plugin_Urakawa_Lang.UrakawaSession_SavePath, path)
            };

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.Overwrite),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.YesNo,
                                                   PopupModalWindow.DialogButton.No,
                                                   false, 300, 160, details, 40, owner);

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

            var details = new TextBoxReadOnlyCaretVisible
            {
                FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                TextReadOnly = String.Format(Tobi_Plugin_Urakawa_Lang.UrakawaSession_SavePath, path)
            };
            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.OpenSaveAsQuestion_),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.YesNo,
                                                   PopupModalWindow.DialogButton.No,
                                                   false, 380, 190, details, 40, null);

            windowPopup.ShowModal();

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Yes)
            {
                return true;
            }

            return false;
        }
    }
}
