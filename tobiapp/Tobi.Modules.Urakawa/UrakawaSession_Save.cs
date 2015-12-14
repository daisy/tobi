using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
                UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.CmdDataCleanup_STRICT_ShortDesc) + @"?",
                Tobi_Plugin_Urakawa_Lang.CmdDataCleanup_STRICT_LongDesc);
        }

        private bool askUser(string message, string info)
        {
            m_Logger.Log("ShellView.askUser", Category.Debug, Priority.Medium);

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
                                                   true, 360, 200, details, 40, null);

            windowPopup.ShowModal();

            if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                return true;
            }

            return false;
        }


        private bool saveAsCommand(string destinationFilePath, bool noUI)
        {
            bool interactive = string.IsNullOrEmpty(destinationFilePath);

            bool doCleanup = interactive && askUserCleanup();
            if (doCleanup)
            {
                DataCleanup(true, false);
            }

            string ext = IsXukSpine ? OpenXukAction.XUK_SPINE_EXTENSION : OpenXukAction.XUK_EXTENSION;

            if (interactive)
            {
                var dialog = new SaveFileDialog
                {
                    FileName = Path.GetFileNameWithoutExtension(DocumentFilePath), //@"tobi_project",
                    DefaultExt = ext,
                    Filter = @"XUK (*" + ext + ")|*" + ext,
                    CheckFileExists = false,
                    CheckPathExists = false,
                    AddExtension = true,
                    CreatePrompt = false,
                    DereferenceLinks = true,
                    OverwritePrompt = false,
                    Title =
                        @"Tobi: " + UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.CmdSaveAs_ShortDesc)
                };

                bool? result = false;

                m_ShellView.DimBackgroundWhile(() => { result = dialog.ShowDialog(); });

                if (result == false)
                {
                    return false;
                }

                destinationFilePath = dialog.FileName;
            }

            if (FileDataProvider.NormaliseFullFilePath(destinationFilePath)
                == FileDataProvider.NormaliseFullFilePath(DocumentFilePath))
            {
#if DEBUG
                Debugger.Break();
#endif
                return false;
            }

            if (interactive && checkWarningFilePathLength(destinationFilePath))
            {
                return false;
            }

            if (File.Exists(destinationFilePath))
            {
                if (!interactive)
                {
                    return false;
                }

                if (!askUserConfirmOverwriteFileFolder(destinationFilePath, false, null))
                {
                    return false;
                }
            }

            Uri oldUri = DocumentProject.Presentations.Get(0).RootUri;
            string oldDataDir = DocumentProject.Presentations.Get(0).DataProviderManager.DataFileDirectory;

            string dirPath = Path.GetDirectoryName(destinationFilePath);
            string prefix = Path.GetFileNameWithoutExtension(destinationFilePath);

            DocumentProject.Presentations.Get(0).DataProviderManager.SetCustomDataFileDirectory(prefix);
            DocumentProject.Presentations.Get(0).RootUri = new Uri(dirPath + Path.DirectorySeparatorChar, UriKind.Absolute);

            if (saveAs(destinationFilePath, false, noUI))
            {
                string destinationFolder =
                    DocumentProject.Presentations.Get(0).DataProviderManager.DataFileDirectoryFullPath;

                DocumentProject.Presentations.Get(0).RootUri = oldUri;

                //string datafolderPathSavedAs = DocumentProject.Presentations.Get(0).DataProviderManager.DataFileDirectoryFullPath;
                DocumentProject.Presentations.Get(0).DataProviderManager.DataFileDirectory = oldDataDir;

                //string datafolderPath = DocumentProject.Presentations.Get(0).DataProviderManager.CopyFileDataProvidersToDataFolderWithPrefix(dirPath, prefix);
                //DebugFix.Assert(datafolderPath == datafolderPathSavedAs);


                bool cancelled = false;

                var copier = new DataFolderCopier(DocumentProject.Presentations.Get(0),
                    //dirPath, prefix
                    destinationFolder
                    );

                if (noUI)
                {
                    copier.DoWork();
                }
                else
                {
                    bool error = m_ShellView.RunModalCancellableProgressTask(true,
                        Tobi_Plugin_Urakawa_Lang.CopyingDataFiles,
                        copier,
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
                }
                //DebugFix.Assert(outcome == !cancelled);

                if (interactive && askUserOpenSavedAs(destinationFilePath))
                {
                    try
                    {
                        OpenFile(destinationFilePath);
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandler.Handle(ex, false, m_ShellView);
                    }
                }

                return !cancelled;
            }
            else
            {
                DocumentProject.Presentations.Get(0).RootUri = oldUri;
                DocumentProject.Presentations.Get(0).DataProviderManager.DataFileDirectory = oldDataDir;

                return false;
            }
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

                    bool saved = saveAsCommand(null, false);

                    //var fileDialog = Container.Resolve<IFileDialogService>();
                    //return fileDialog.SaveAs();
                },
                () => DocumentProject != null && !IsXukSpine && !isAudioRecording,
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
                () => save(false),
                () => DocumentProject != null && !isAudioRecording,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Save));

            m_ShellView.RegisterRichCommand(SaveCommand);
        }

        private const string SAVING_EXT = ".SAVING";

        private bool saveAuto()
        {
            if (DocumentProject == null || string.IsNullOrEmpty(DocumentFilePath))
            {
                return false;
            }

            string autoSaveFilePath = Path.Combine(
                Path.GetDirectoryName(DocumentFilePath),
                Path.GetFileName(DocumentFilePath) + ".AUTOSAVE"
                );

            //m_Logger.Log(String.Format(@"UrakawaSession.saveAuto() [{0}]", autoSaveFilePath), Category.Debug, Priority.Medium);
            m_Logger.Log(@"UrakawaSession.saveAuto()", Category.Debug, Priority.Medium);

            return saveAs(autoSaveFilePath, true, true);
        }

        private bool save(bool noUI)
        {
            if (DocumentProject == null)
            {
                return false;
            }
            return saveAs(DocumentFilePath, false, noUI);
        }

        private bool saveAs(string filePath, bool autoSave, bool noUI)
        {
            if (DocumentProject == null)
            {
                return false;
            }

            string saveAsDocumentFilePath = filePath;

            m_Logger.Log(String.Format(@"UrakawaSession.saveas() [{0}]", saveAsDocumentFilePath), Category.Debug, Priority.Medium);

            //var backWorker = new BackgroundWorker
            //    {
            //        WorkerSupportsCancellation = true,
            //        WorkerReportsProgress = true
            //    };

            //bool cancelFlag = false;
            //int currentPercentage = 0;

            var uri = new Uri(saveAsDocumentFilePath + SAVING_EXT, UriKind.Absolute);
            //DocumentProject.OpenXuk(uri);

            DocumentProject.PrettyFormat = Settings.Default.XUK_PrettyFormat;

            var action = new SaveXukAction(DocumentProject, DocumentProject, uri, !Settings.Default.EnableAutoSave)
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

                    if (DocumentFilePath == saveAsDocumentFilePath)
                    {
                        DebugFix.Assert(!autoSave);

                        string saving = DocumentFilePath + SAVING_EXT;

                        if (File.Exists(saving))
                        {
                            if (Settings.Default.EnableAutoSave)
                            {
                                SaveXukAction.Backup(DocumentFilePath);
                            }
                            File.Delete(DocumentFilePath);

                            File.Move(saving, DocumentFilePath);
                            try
                            {
                                File.SetAttributes(DocumentFilePath, FileAttributes.Normal);
                            }
                            catch
                            {
                            }

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
                        string saving = saveAsDocumentFilePath + SAVING_EXT;

                        if (File.Exists(saving))
                        {
                            if (File.Exists(saveAsDocumentFilePath))
                            {
                                if (Settings.Default.EnableAutoSave && !autoSave)
                                {
                                    SaveXukAction.Backup(saveAsDocumentFilePath);
                                }
                                File.Delete(saveAsDocumentFilePath);
                            }

                            File.Move(saving, saveAsDocumentFilePath);
                            try
                            {
                                File.SetAttributes(saveAsDocumentFilePath, FileAttributes.Normal);
                            }
                            catch
                            {
                            }
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

            if (autoSave || noUI)
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

            string savingFile = saveAsDocumentFilePath + SAVING_EXT;
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

        public string messageBoxFilePick(string title, string exeOrBat)
        {
            m_Logger.Log(@"UrakawaSession_Save.messageBoxFilePick", Category.Debug, Priority.Medium);

            string ext = Path.GetExtension(exeOrBat);

            PopupModalWindow windowPopup = null;


            //var textArea = new TextBoxReadOnlyCaretVisible()
            //{
            //    FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

            //    TextReadOnly = info,

            //    BorderThickness = new Thickness(1),
            //    Padding = new Thickness(6),

            //    HorizontalAlignment = HorizontalAlignment.Stretch,
            //    VerticalAlignment = VerticalAlignment.Stretch,
            //    Focusable = true,
            //    TextWrapping = TextWrapping.Wrap
            //};

            //var scroll = new ScrollViewer
            //{
            //    Content = textArea,

            //    Margin = new Thickness(6),
            //    VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            //    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            //};

            var fileText = new TextBoxReadOnlyCaretVisible
            {
                FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"],

                BorderThickness = new Thickness(1),
                BorderBrush = SystemColors.ControlDarkDarkBrush,
                //Padding = new Thickness(6),
                TextReadOnly = " ",
                //IsReadOnly = true,
                Width = 300,

                Margin = new Thickness(0, 8, 0, 0),
                Padding = new Thickness(4),

                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap
            };
            //fileText.SetValue(KeyboardNavigation.TabIndexProperty, 12);
            KeyboardNavigation.SetTabIndex(fileText, 12);

            var label = new TextBlock
            {
                Text = "Please locate [" + exeOrBat + "]",
                Margin = new Thickness(0, 0, 8, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Focusable = true,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = FontWeights.Bold,
            };
            //label.SetValue(KeyboardNavigation.TabIndexProperty, 10);
            KeyboardNavigation.SetTabIndex(label, 10);


            var fileButton = new Button()
                {
                    Content = "Browse...",
                    Margin = new Thickness(0, 0, 0, 0),
                    Padding = new Thickness(8, 0, 8, 0),
                };

            //fileButton.SetValue(KeyboardNavigation.TabIndexProperty, 11);
            KeyboardNavigation.SetTabIndex(fileButton, 11);

            fileButton.Click += (sender, e) =>
                {
                    var dlg_ = new Microsoft.Win32.OpenFileDialog
                        {
                            FileName = exeOrBat,
                            DefaultExt = ext,

                            Filter = @"Executable (*" + ext + ")|*" + ext + "",
                            CheckFileExists = false,
                            CheckPathExists = false,
                            AddExtension = true,
                            DereferenceLinks = true,
                            Title =
                                @"Tobi: " +
                                "Pipeline2 (" + exeOrBat + ")"
                        };

                    bool? result_ = false;

                    m_ShellView.DimBackgroundWhile(
                        () => { result_ = dlg_.ShowDialog(); }
                        , windowPopup
                        );

                    if (result_ == true)
                    {
                        fileText.TextReadOnly = dlg_.FileName;
                    }
                };

            var filePanel = new DockPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,

                LastChildFill = true
            };

            //fileButton.SetValue(DockPanel.DockProperty, Dock.Right);
            DockPanel.SetDock(fileButton, Dock.Right);

            filePanel.Children.Add(fileButton);
            filePanel.Children.Add(label);

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            panel.Children.Add(filePanel);
            panel.Children.Add(fileText);
            //panel.Margin = new Thickness(8, 8, 8, 0);

            windowPopup = new PopupModalWindow(m_ShellView,
                                                   title,
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 380, 200,
                                                   null,//scroll,
                                                   300, null);

            windowPopup.ShowModal();

            if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                return fileText.TextReadOnly;
            }

            return null;
        }

        public void messageBoxText(string title, string text, string info)
        {
            m_Logger.Log(@"UrakawaSession_Save.messageBoxText", Category.Debug, Priority.Medium);

            if (String.IsNullOrEmpty(info))
            {
                info = "";
            }

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

                IsReadOnly = true,
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

            if (String.IsNullOrEmpty(info))
            {
                scroll = null;
            }

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   title,
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.Ok,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 500, 160, scroll, 300, null);

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
