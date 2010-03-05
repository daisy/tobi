using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.daisy;
using urakawa.data;
using urakawa.events;

namespace Tobi.Plugin.Urakawa
{
    ///<summary>
    /// Single shared instance (singleton) of a session to host the Urakawa SDK aurthoring data model.
    ///</summary>
    [Export(typeof(IUrakawaSession)), PartCreationPolicy(CreationPolicy.Shared)]
    public sealed partial class UrakawaSession : PropertyChangedNotifyBase, IUrakawaSession, IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        private readonly ILoggerFacade m_Logger;
        private readonly IEventAggregator m_EventAggregator;
        private readonly IShellView m_ShellView;

        ///<summary>
        /// We inject a few dependencies in this constructor.
        /// No document is open and IsDirty is initialized to false.
        ///</summary>
        ///<param name="logger">normally obtained from the Unity container, it's a built-in CAG service</param>
        ///<param name="eventAggregator">normally obtained from the Unity container, it's a built-in CAG service</param>
        ///<param name="shellView">normally obtained from the Unity container, it's a Tobi-specific entity</param>
        [ImportingConstructor]
        public UrakawaSession(
            ILoggerFacade logger,
            IEventAggregator eventAggregator,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView)
        {
            m_Logger = logger;
            m_EventAggregator = eventAggregator;
            m_ShellView = shellView;

            //IsDirty = false;

            InitializeCommands();
            InitializeRecentFiles();
        }

        //#pragma warning disable 1591 // missing comments

        //#pragma warning restore 1591
        //public RichDelegateCommand NewCommand { get; private set; }

        public RichDelegateCommand CloseCommand { get; private set; }

        public RichDelegateCommand UndoCommand { get; private set; }
        public RichDelegateCommand RedoCommand { get; private set; }

        public RichDelegateCommand OpenDocumentFolderCommand { get; private set; }

        public RichDelegateCommand DataCleanupCommand { get; private set; }

        private Project m_DocumentProject;
        public Project DocumentProject
        {
            get { return m_DocumentProject; }
            set
            {
                if (m_DocumentProject == value)
                {
                    return;
                }
                if (m_DocumentProject != null)
                {
                    //m_DocumentProject.Changed -= OnDocumentProjectChanged;
                    m_DocumentProject.Presentations.Get(0).UndoRedoManager.CommandDone -= OnUndoRedoManagerChanged;
                    m_DocumentProject.Presentations.Get(0).UndoRedoManager.CommandReDone -= OnUndoRedoManagerChanged;
                    m_DocumentProject.Presentations.Get(0).UndoRedoManager.CommandUnDone -= OnUndoRedoManagerChanged;
                }

                //IsDirty = false;
                m_DocumentProject = value;
                if (m_DocumentProject != null)
                {
                    //m_DocumentProject.Changed += OnDocumentProjectChanged;
                    m_DocumentProject.Presentations.Get(0).UndoRedoManager.CommandDone += OnUndoRedoManagerChanged;
                    m_DocumentProject.Presentations.Get(0).UndoRedoManager.CommandReDone += OnUndoRedoManagerChanged;
                    m_DocumentProject.Presentations.Get(0).UndoRedoManager.CommandUnDone += OnUndoRedoManagerChanged;
                }
                RaisePropertyChanged(() => DocumentProject);
                RaisePropertyChanged(() => IsDirty);
            }
        }

        private void OnUndoRedoManagerChanged(object sender, DataModelChangedEventArgs e)
        {
            RaisePropertyChanged(() => IsDirty);
            //IsDirty = m_DocumentProject.Presentations.Get(0).UndoRedoManager.CanUndo;
        }

        //private void OnDocumentProjectChanged(object sender, DataModelChangedEventArgs e)
        //{
        //    RaisePropertyChanged(() => IsDirty);
        //    //IsDirty = true;
        //}

        private string m_DocumentFilePath;
        [NotifyDependsOn("DocumentProject")]
        public string DocumentFilePath
        {
            get { return m_DocumentFilePath; }
            set
            {
                if (m_DocumentFilePath == value)
                {
                    return;
                }
                m_DocumentFilePath = value;
                RaisePropertyChanged(() => DocumentFilePath);
            }
        }

        //private bool m_IsDirty;
        public bool IsDirty
        {
            get
            {
                if (m_DocumentProject != null)
                {
                    return !m_DocumentProject.Presentations.Get(0).UndoRedoManager.IsOnDirtyMarker();
                }
                return false;
                //return m_IsDirty;
            }
            //set
            //{
            //    if (m_IsDirty == value)
            //    {
            //        return;
            //    }
            //    m_IsDirty = value;
            //    RaisePropertyChanged(() => IsDirty);
            //}
        }

        internal void InitializeCommands()
        {
            initCommands_Open();
            initCommands_Save();
            initCommands_Export();

            OpenDocumentFolderCommand = new RichDelegateCommand(
                Tobi_Plugin_Urakawa_Lang.OpenDocumentFolder,
                Tobi_Plugin_Urakawa_Lang.OpenDocumentFolder_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeFoxtrotIcon(@"Foxtrot_user-home"),
                () =>
                {
                    m_Logger.Log(@"ShellView.OpenDocumentFolderCommand", Category.Debug, Priority.Medium);

                    var p = new Process
                    {
                        StartInfo = { FileName = Path.GetDirectoryName(DocumentFilePath) }
                    };
                    p.Start();
                },
                 () => DocumentProject != null && !string.IsNullOrEmpty(DocumentFilePath),
                Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_ShowTobiFolder)
                );

            m_ShellView.RegisterRichCommand(OpenDocumentFolderCommand);
            //
            //
            UndoCommand = new RichDelegateCommand(
                Tobi_Plugin_Urakawa_Lang.Undo,
                Tobi_Plugin_Urakawa_Lang.Undo_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeNeuIcon(@"Neu_edit-undo"),
                () => DocumentProject.Presentations.Get(0).UndoRedoManager.Undo(),
                () => DocumentProject != null && DocumentProject.Presentations.Get(0).UndoRedoManager.CanUndo,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Undo));

            m_ShellView.RegisterRichCommand(UndoCommand);
            //
            RedoCommand = new RichDelegateCommand(
                Tobi_Plugin_Urakawa_Lang.Redo,
                Tobi_Plugin_Urakawa_Lang.Redo_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeNeuIcon(@"Neu_edit-redo"),
                () => DocumentProject.Presentations.Get(0).UndoRedoManager.Redo(),
                () => DocumentProject != null && DocumentProject.Presentations.Get(0).UndoRedoManager.CanRedo,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Redo));

            m_ShellView.RegisterRichCommand(RedoCommand);
            //
            CloseCommand = new RichDelegateCommand(
                Tobi_Plugin_Urakawa_Lang.Close,
                Tobi_Plugin_Urakawa_Lang.Close_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"emblem-symbolic-link"),
                () => Close(),
                () => DocumentProject != null,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Close));

            m_ShellView.RegisterRichCommand(CloseCommand);
            //
            DataCleanupCommand = new RichDelegateCommand(
                Tobi_Plugin_Urakawa_Lang.DataCleanup,
                Tobi_Plugin_Urakawa_Lang.DataCleanup_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeNeuIcon(@"Neu_user-trash-full"),
                DataCleanup,
                () => DocumentProject != null,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_DataCleanup));

            m_ShellView.RegisterRichCommand(DataCleanupCommand);
        }

        public void DataCleanup()
        {
            string docPath = DocumentFilePath;
            Project project = DocumentProject;

            if (!Close()) return;

            project.Presentations.Get(0).Cleanup(); //TODO: time consuming progress bar

            var listOfDataProviderFiles = new List<string>();
            foreach (var dataProvider in project.Presentations.Get(0).DataProviderManager.ManagedObjects.ContentsAs_YieldEnumerable)
            {
                var fileDataProvider = dataProvider as FileDataProvider;
                if (fileDataProvider == null) continue;

                listOfDataProviderFiles.Add(fileDataProvider.DataFileRelativePath);
            }

            var dataFolderPath = project.Presentations.Get(0).DataProviderManager.DataFileDirectoryFullPath;

            var deletedDataFolderPath = Path.Combine(dataFolderPath, "__DELETED");
            if (!Directory.Exists(deletedDataFolderPath))
            {
                Directory.CreateDirectory(deletedDataFolderPath);
            }


            foreach (string filePath in Directory.GetFiles(dataFolderPath))
            {
                var fileName = Path.GetFileName(filePath);
                if (!listOfDataProviderFiles.Contains(fileName))
                {
                    var filePathDest = Path.Combine(deletedDataFolderPath, fileName);
                    File.Move(filePath, filePathDest);
                }
            }

            if (Directory.GetFiles(deletedDataFolderPath).Length == 0) return;

            var p = new Process
            {
                StartInfo = { FileName = deletedDataFolderPath }
            };
            p.Start();

            DocumentFilePath =  docPath;
            DocumentProject = project;

            if (save())
            {
                DocumentFilePath = null;
                DocumentProject = null;

                OpenFile(docPath);
            }
        }

        public bool Close()
        {
            if (DocumentProject == null)
            {
                return true;
            }

            if (IsDirty)
            {
                m_Logger.Log(@"UrakawaSession.askUserSave", Category.Debug, Priority.Medium);

                var label = new TextBlock //TextBoxReadOnlyCaretVisible(Tobi_Plugin_Urakawa_Lang.UnsavedChangesConfirm) //
                {
                    Text = Tobi_Plugin_Urakawa_Lang.UnsavedChangesConfirm,
                    Margin = new Thickness(8, 0, 8, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Focusable = true,
                    TextWrapping = TextWrapping.Wrap
                };

                var iconProvider = new ScalableGreyableImageProvider(m_ShellView.LoadTangoIcon(@"help-browser"),
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

                var details = new TextBoxReadOnlyCaretVisible(Tobi_Plugin_Urakawa_Lang.UnsavedChangesDetails);

                var windowPopup = new PopupModalWindow(m_ShellView,
                                                       UserInterfaceStrings.EscapeMnemonic(
                                                           Tobi_Plugin_Urakawa_Lang.UnsavedChanges),
                                                       panel,
                                                       PopupModalWindow.DialogButtonsSet.YesNoCancel,
                                                       PopupModalWindow.DialogButton.Cancel,
                                                       false, 300, 160, details, 40);

                windowPopup.ShowModal();

                if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Cancel)
                {
                    return false;
                }

                if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Yes)
                {
                    if (!save())
                    {
                        return false;
                    }
                }
            }

            m_Logger.Log(@"-- PublishEvent [ProjectUnLoadedEvent] UrakawaSession.close", Category.Debug, Priority.Medium);

            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Publish(DocumentProject);

            DocumentFilePath = null;
            DocumentProject = null;

            return true;
        }


        protected bool DoWorkProgressUI(string title, IDualCancellableProgressReporter converter,
            Action actionCancelled, Action actionCompleted)
        {
            m_Logger.Log(String.Format(@"UrakawaSession.DoWorkProgressUI() [{0}]", DocumentFilePath), Category.Debug, Priority.Medium);

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
                Text = title,
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
                                                   title,
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.Cancel,
                                                   PopupModalWindow.DialogButton.Cancel,
                                                   false, 500, 150, details, 80);

            var backWorker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };

            Exception workException = null;
            backWorker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                //var dummy = (string)args.Argument;

                if (backWorker.CancellationPending)
                {
                    args.Cancel = true;
                    return;
                }

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

                converter.DoWork();

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
                workException = args.Error;

                backWorker = null;

                if (converter.RequestCancellation || args.Cancelled)
                {
                    actionCancelled();
                    windowPopup.ForceClose(PopupModalWindow.DialogButton.Cancel);
                }
                else
                {
                    actionCompleted();
                    windowPopup.ForceClose(PopupModalWindow.DialogButton.ESC);
                }

                //var result = (string)args.Result;
            };

            backWorker.RunWorkerAsync(@"dummy arg");
            windowPopup.ShowModal();

            if (workException != null)
            {
                throw workException;
            }

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Cancel)
            {
                if (backWorker == null) return false;

                progressBar.IsIndeterminate = true;
                label.Text = "Please wait while cancelling...";                     // TODO LOCALIZE WaitToCancel

                progressBar2.Visibility = Visibility.Collapsed;
                label2.Visibility = Visibility.Collapsed;

                //details.Text = "Cancelling the current operation...";

                windowPopup = new PopupModalWindow(m_ShellView,
                                                       UserInterfaceStrings.EscapeMnemonic(
                                                           Tobi_Plugin_Urakawa_Lang.CancellingTask),
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
