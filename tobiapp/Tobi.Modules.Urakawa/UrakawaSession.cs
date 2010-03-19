using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.core;
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
        private readonly Object m_TreeNodeSelectionLock = new object();
        private TreeNode m_TreeNode;
        private TreeNode m_SubTreeNode;

        [Conditional("DEBUG")]
        private void verifyTreeNodeSelection()
        {
            if (m_SubTreeNode != null)
            {
                Debug.Assert(m_TreeNode != null);
                Debug.Assert(m_TreeNode.IsAncestorOf(m_SubTreeNode)); // nodes cannot be equal
            }

            if (m_TreeNode == null) return;

            TreeNode nodeAncestorAudio = m_TreeNode.GetFirstAncestorWithManagedAudio();
            bool nodeHasDirectAudio = m_TreeNode.GetManagedAudioMediaOrSequenceMedia() != null;
            TreeNode nodeDescendantAudio = m_TreeNode.GetFirstDescendantWithManagedAudio();

            if (nodeAncestorAudio != null)
            {
                Debug.Assert(!nodeHasDirectAudio);
                Debug.Assert(nodeDescendantAudio == null);
            }

            if (nodeHasDirectAudio)
            {
                Debug.Assert(nodeAncestorAudio == null);
                Debug.Assert(nodeDescendantAudio == null);
            }

            if (nodeDescendantAudio != null)
            {
                Debug.Assert(!nodeHasDirectAudio);
                Debug.Assert(nodeAncestorAudio == null);
            }

            if (m_SubTreeNode == null)
            {
                if (nodeAncestorAudio != null || nodeHasDirectAudio || nodeDescendantAudio != null)
                {
                    Debug.Assert(nodeHasDirectAudio && nodeAncestorAudio == null && nodeDescendantAudio == null);
                }
                return;
            }

            TreeNode subnodeAncestorAudio = m_SubTreeNode.GetFirstAncestorWithManagedAudio();
            bool subnodeHasDirectAudio = m_SubTreeNode.GetManagedAudioMediaOrSequenceMedia() != null;
            TreeNode subnodeDescendantAudio = m_SubTreeNode.GetFirstDescendantWithManagedAudio();

            if (subnodeAncestorAudio != null)
            {
                Debug.Assert(!subnodeHasDirectAudio);
                Debug.Assert(subnodeDescendantAudio == null);
            }

            if (subnodeHasDirectAudio)
            {
                Debug.Assert(subnodeAncestorAudio == null);
                Debug.Assert(subnodeDescendantAudio == null);
            }

            if (subnodeDescendantAudio != null)
            {
                Debug.Assert(!subnodeHasDirectAudio);
                Debug.Assert(subnodeAncestorAudio == null);
            }

            if (subnodeAncestorAudio != null || subnodeHasDirectAudio || subnodeDescendantAudio != null)
            {
                Debug.Assert(subnodeHasDirectAudio && subnodeAncestorAudio == null && subnodeDescendantAudio == null);
            }
        }

        public Tuple<TreeNode, TreeNode> PerformTreeNodeSelection(TreeNode node)
        {
            if (node == null) throw new ArgumentNullException("node");

            // Reminder: the "lock" block statement is equivalent to:
            //Monitor.Enter(LOCK);
            //try
            //{
            //    
            //}
            //finally
            //{
            //    Monitor.Exit(LOCK);
            //}
            lock (m_TreeNodeSelectionLock)
            {
                var oldTreeNodeSelection = new Tuple<TreeNode, TreeNode>(m_TreeNode, m_SubTreeNode);
#if DEBUG
                // After audio edits, the old selection may be invalid.
                //verifyTreeNodeSelection();
#endif
                // performing a valid selection is based on branch logic, it depends on the position of the audio, if any
                bool nodeHasDirectAudio = node.GetManagedAudioMediaOrSequenceMedia() != null;
                TreeNode nodeDescendantAudio = node.GetFirstDescendantWithManagedAudio();
                TreeNode nodeAncestorAudio = node.GetFirstAncestorWithManagedAudio();

                if (m_TreeNode == null) // brand new selection
                {
                    if (nodeHasDirectAudio) // right on
                    {
                        m_TreeNode = node;
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else if (nodeAncestorAudio != null) // shift up
                    {
                        m_TreeNode = nodeAncestorAudio;
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else if (nodeDescendantAudio != null) // sub shift down
                    {
                        m_TreeNode = node;
                        m_SubTreeNode = nodeDescendantAudio;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else // no audio on this branch, always legal position
                    {
                        m_TreeNode = node;
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    goto done;
                }

                // From here we know that m_TreeNode != null

                if (node == m_TreeNode)
                {
                    if (nodeHasDirectAudio) // right on
                    {
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else if (nodeAncestorAudio != null) // shift up
                    {
                        m_TreeNode = nodeAncestorAudio;
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else if (nodeDescendantAudio != null) // shift down
                    {
                        if (m_SubTreeNode == null)
                        {
                            m_TreeNode = nodeDescendantAudio;
                            m_SubTreeNode = null;
#if DEBUG
                            verifyTreeNodeSelection();
#endif
                        }
                        else
                        {
                            bool subtreenodeHasDirectAudio = m_SubTreeNode.GetManagedAudioMediaOrSequenceMedia() != null;
                            if (subtreenodeHasDirectAudio)
                            {
                                m_TreeNode = m_SubTreeNode;
                                m_SubTreeNode = null;
#if DEBUG
                                verifyTreeNodeSelection();
#endif
                            }
                            else
                            {
                                TreeNode subtreenodeDescendantAudio = m_SubTreeNode.GetFirstDescendantWithManagedAudio();

                                if (subtreenodeDescendantAudio == null)
                                {
                                    m_TreeNode = nodeDescendantAudio;
                                    m_SubTreeNode = null;
                                }
                                else
                                {
                                    m_TreeNode = subtreenodeDescendantAudio;
                                    m_SubTreeNode = null;
                                }
#if DEBUG
                                verifyTreeNodeSelection();
#endif
                            }
                        }
                    }
                    else // no audio on this branch, always legal position
                    {
                        if (m_SubTreeNode != null) // toggle
                        {
                            m_TreeNode = m_SubTreeNode;
                            m_SubTreeNode = null;
#if DEBUG
                            verifyTreeNodeSelection();
#endif
                        }
                    }
                    goto done;
                }

                bool treenodeHasDirectAudio = m_TreeNode.GetManagedAudioMediaOrSequenceMedia() != null;
                TreeNode treenodeDescendantAudio = m_TreeNode.GetFirstDescendantWithManagedAudio();
                TreeNode treenodeAncestorAudio = m_TreeNode.GetFirstAncestorWithManagedAudio();

                if (m_SubTreeNode != null && node == m_SubTreeNode)
                {
                    if (nodeHasDirectAudio) // shift down
                    {
                        m_TreeNode = m_SubTreeNode;
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else // toggle
                    {
                        m_TreeNode = m_TreeNode; // unchanged
                        m_SubTreeNode = null;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    goto done;
                }

                if (node.IsDescendantOf(m_TreeNode)) // or: m_TreeNode.IsAncestorOf(node)
                {
                    if (nodeHasDirectAudio) // right on
                    {
                        m_SubTreeNode = node;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else if (nodeAncestorAudio != null) // shift up
                    {
                        m_SubTreeNode = nodeAncestorAudio;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else if (nodeDescendantAudio == null) // legal shift down
                    {
                        m_SubTreeNode = node;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    else
                    {
                        m_SubTreeNode = node;
#if DEBUG
                        verifyTreeNodeSelection();
#endif
                    }
                    goto done;
                }

                if (node.IsAncestorOf(m_TreeNode)) // or: m_TreeNode.IsAncestorOf(node)
                {
                    if (m_SubTreeNode == null)
                    {
                        m_SubTreeNode = m_TreeNode;
                    }
                    m_TreeNode = node;
#if DEBUG
                    verifyTreeNodeSelection();
#endif
                    goto done;
                }

                //FINAL ELSE:

                m_TreeNode = node;
                if (nodeDescendantAudio != null)
                {
                    m_SubTreeNode = nodeDescendantAudio;
#if DEBUG
                    verifyTreeNodeSelection();
#endif
                }
                else
                {
                    m_SubTreeNode = null;
#if DEBUG
                    verifyTreeNodeSelection();
#endif
                }

            done:

                var treeNodeSelection = new Tuple<TreeNode, TreeNode>(m_TreeNode, m_SubTreeNode);

                if (oldTreeNodeSelection != treeNodeSelection)
                    m_EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Publish(new Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>>(oldTreeNodeSelection, treeNodeSelection));

                return treeNodeSelection;
            }
        }

        public Tuple<TreeNode, TreeNode> GetTreeNodeSelection()
        {
            lock (m_TreeNodeSelectionLock)
            {
                return new Tuple<TreeNode, TreeNode>(m_TreeNode, m_SubTreeNode);
            }
        }

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

                    m_ShellView.ExecuteShellProcess(Path.GetDirectoryName(DocumentFilePath));
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
                () =>
                {
                    PopupModalWindow.DialogButton button =
                        CheckSaveDirtyAndClose(PopupModalWindow.DialogButtonsSet.YesNoCancel, "confirm");
                    //if (button != PopupModalWindow.DialogButton.Ok)
                    //{
                    //    return false;
                    //}
                },
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
            // Backup before close.
            string docPath = DocumentFilePath;
            Project project = DocumentProject;

            // Closing is REQUIRED ! 
            PopupModalWindow.DialogButton button = CheckSaveDirtyAndClose(PopupModalWindow.DialogButtonsSet.OkCancel, "data cleanup");
            if (!PopupModalWindow.IsButtonOkYesApply(button))
            {
                return;
            }

            project.Presentations.Get(0).UndoRedoManager.FlushCommands();
            //RaisePropertyChanged(()=>IsDirty);

            // ==> SAVED AND CLOSED (clipboard removed), undo-redo removed.

            var dataFolderPath = project.Presentations.Get(0).DataProviderManager.DataFileDirectoryFullPath;

            var deletedDataFolderPath = Path.Combine(dataFolderPath, "__DELETED" + Path.DirectorySeparatorChar);
            if (!Directory.Exists(deletedDataFolderPath))
            {
                Directory.CreateDirectory(deletedDataFolderPath);
            }

            bool cancelled = false;

            bool result = m_ShellView.RunModalCancellableProgressTask(true,
                Tobi_Plugin_Urakawa_Lang.CleaningUpDataFiles,
                new Cleaner(project.Presentations.Get(0), deletedDataFolderPath), //project.Presentations.Get(0).Cleanup();
                () =>
                {
                    m_Logger.Log(@"CANCELLED", Category.Debug, Priority.Medium);
                    cancelled = true;

                },
                () =>
                {
                    m_Logger.Log(@"DONE", Category.Debug, Priority.Medium);
                    cancelled = false;

                });

            if (cancelled)
            {
                Debug.Assert(!result);

                // We restore the old one, not cleaned-up (or partially...).

                DocumentFilePath = null;
                DocumentProject = null;

                try
                {
                    OpenFile(docPath);
                }
                catch (Exception ex)
                {
                    ExceptionHandler.Handle(ex, false, m_ShellView);
                }
            }
            else
            {
                var listOfDataProviderFiles = new List<string>();
                foreach (var dataProvider in project.Presentations.Get(0).DataProviderManager.ManagedObjects.ContentsAs_YieldEnumerable)
                {
                    var fileDataProvider = dataProvider as FileDataProvider;
                    if (fileDataProvider == null) continue;

                    listOfDataProviderFiles.Add(fileDataProvider.DataFileRelativePath);
                }


                bool folderIsShowing = false;
                if (Directory.GetFiles(deletedDataFolderPath).Length != 0)
                {
                    folderIsShowing = true;

                    m_ShellView.ExecuteShellProcess(deletedDataFolderPath);
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

                if (!folderIsShowing && Directory.GetFiles(deletedDataFolderPath).Length != 0)
                {
                    m_ShellView.ExecuteShellProcess(deletedDataFolderPath);
                }

                // We must now save the modified cleaned-up doc

                DocumentFilePath = docPath;
                DocumentProject = project;

                if (save())
                {
                    DocumentFilePath = null;
                    DocumentProject = null;

                    try
                    {
                        OpenFile(docPath);
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandler.Handle(ex, false, m_ShellView);
                    }
                }
            }

            //if (!Dispatcher.CurrentDispatcher.CheckAccess())
            //{
            //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
            //    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, (Action) (() =>
            //      {
            //          var p = new Process
            //          {
            //              StartInfo = { FileName = deletedDataFolderPath }
            //          };
            //          p.Start();
            //      }));
            //}
            //else
            //{
            //    //Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
            //    //{
            //    //    var p = new Process
            //    //    {
            //    //        StartInfo = { FileName = deletedDataFolderPath }
            //    //    };
            //    //    p.Start();
            //    //}));

            //    //var p = new Process
            //    //{
            //    //    StartInfo = { FileName = deletedDataFolderPath }
            //    //};
            //    //p.Start();


            //    Thread thread = new Thread(new ThreadStart(() =>
            //    {
            //        var p = new Process
            //        {
            //            StartInfo =
            //                {
            //                    FileName = deletedDataFolderPath,
            //                    CreateNoWindow = true,
            //                    UseShellExecute = true,
            //                    WorkingDirectory = deletedDataFolderPath
            //                }
            //        };
            //        p.Start();
            //    }));
            //    thread.Name = "File explorer popup from Tobi (after cleanup _DELETED)";
            //    thread.Priority = ThreadPriority.Normal;
            //    thread.IsBackground = true;
            //    thread.Start();
            //}

        }

        public PopupModalWindow.DialogButton CheckSaveDirtyAndClose(PopupModalWindow.DialogButtonsSet buttonset, string role)
        {
            if (DocumentProject == null)
            {
                return PopupModalWindow.DialogButton.Ok;
            }

            var result = PopupModalWindow.DialogButton.Ok;

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
                                                           Tobi_Plugin_Urakawa_Lang.UnsavedChanges) + (string.IsNullOrEmpty(role) ? "" : " (" + role + ")"),
                                                       panel,
                                                       buttonset,
                                                       PopupModalWindow.DialogButton.Cancel,
                                                       buttonset == PopupModalWindow.DialogButtonsSet.Cancel
                                                       || buttonset == PopupModalWindow.DialogButtonsSet.OkCancel
                                                       || buttonset == PopupModalWindow.DialogButtonsSet.YesNoCancel,
                                                       300, 160, details, 40);

                windowPopup.ShowModal();

                if (PopupModalWindow.IsButtonEscCancel(windowPopup.ClickedDialogButton))
                {
                    return PopupModalWindow.DialogButton.Cancel;
                }

                if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
                {
                    if (!save())
                    {
                        return PopupModalWindow.DialogButton.Cancel;
                    }

                    result = PopupModalWindow.DialogButton.Ok;
                }
                else
                {
                    result = PopupModalWindow.DialogButton.No;
                }
            }

            m_Logger.Log(@"-- PublishEvent [ProjectUnLoadedEvent] UrakawaSession.close", Category.Debug, Priority.Medium);

            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Publish(DocumentProject);

            DocumentFilePath = null;
            DocumentProject = null;

            return result;
        }
    }
}
