using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Practices.Composite;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.command;
using urakawa.commands;
using urakawa.core;
using urakawa.events.undo;
using AudioLib;
using urakawa.undo;

namespace Tobi.Plugin.NavigationPane
{
    [Export(typeof(MarkersPaneViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public class MarkersPaneViewModel : ViewModelBase, IPartImportsSatisfiedNotification, UndoRedoManager.Hooker.Host
    {
        public RichDelegateCommand CommandToggleMark { get; private set; }
        public RichDelegateCommand CommandRemoveAllMarks { get; private set; }
        
        #region Construction

        //        protected IUnityContainer Container { get; private set; }
        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;

        private readonly IShellView m_ShellView;
        private readonly IUrakawaSession m_UrakawaSession;

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        [ImportingConstructor]
        public MarkersPaneViewModel(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView view,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession urakawaSession)
        {
            m_EventAggregator = eventAggregator;
            m_Logger = logger;
            m_UrakawaSession = urakawaSession;
            m_ShellView = view;


            m_Logger.Log("MarkersPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);

            CommandToggleMark = new RichDelegateCommand(
                Tobi_Plugin_NavigationPane_Lang.CmdNavigationToggleMark_ShortDesc,
                Tobi_Plugin_NavigationPane_Lang.CmdNavigationToggleMark_LongDesc,
                                    null, // KeyGesture obtained from settings (see last parameters below)
                                    m_ShellView.LoadTangoIcon("bookmark-new"),
                                    () =>
                                    {
                                        var cmd = SelectedTreeNode.Presentation.CommandFactory.CreateTreeNodeSetIsMarkedCommand(SelectedTreeNode, !SelectedTreeNode.IsMarked);
                                        SelectedTreeNode.Presentation.UndoRedoManager.Execute(cmd);
                                    },
                                    () => SelectedTreeNode != null && !m_UrakawaSession.isAudioRecording
                     && !m_UrakawaSession.IsXukSpine, //!m_UrakawaSession.IsSplitMaster &&
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_ToggleDocMark));

            m_ShellView.RegisterRichCommand(CommandToggleMark);

            CommandRemoveAllMarks = new RichDelegateCommand(
                Tobi_Plugin_NavigationPane_Lang.CmdNavigationRemoveAllMarks_ShortDesc,
                Tobi_Plugin_NavigationPane_Lang.CmdNavigationRemoveAllMarks_LongDesc,
                                    null, // KeyGesture obtained from settings (see last parameters below)
                                    null, //m_ShellView.LoadTangoIcon("bookmark-new"),
                                    () =>
                                    {
                                        if (MarkersNavigator_MarkedTreeNodes.Count <= 0)
                                        {
                                            return;
                                        }
                                        var treeNodes = new List<TreeNode>(MarkersNavigator_MarkedTreeNodes.Count);
                                        foreach (MarkedTreeNode marked in MarkersNavigator_MarkedTreeNodes)
                                        {
                                            treeNodes.Add(marked.TreeNode);
                                        }

                                        m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction(Tobi_Plugin_NavigationPane_Lang.CmdNavigationRemoveAllMarks_ShortDesc, Tobi_Plugin_NavigationPane_Lang.CmdNavigationRemoveAllMarks_LongDesc, "MARKS_REMOVE_ALL");
                                        foreach (TreeNode treeNode in treeNodes)
                                        {
                                            var cmd = treeNode.Presentation.CommandFactory.CreateTreeNodeSetIsMarkedCommand(treeNode, !treeNode.IsMarked);
                                            treeNode.Presentation.UndoRedoManager.Execute(cmd);
                                        }
                                        m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();
                                    },
                                    () => m_UrakawaSession.DocumentProject != null && !m_UrakawaSession.isAudioRecording
                     && !m_UrakawaSession.IsXukSpine, //SelectedTreeNode != null, //!m_UrakawaSession.IsSplitMaster &&
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_RemoveAllDocMarks));

            m_ShellView.RegisterRichCommand(CommandRemoveAllMarks);

            CommandFindFocusMarkers = new RichDelegateCommand(
                @"MARKERS CommandFindFocus DUMMY TXT",
                @"MARKERS CommandFindFocus DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    if (View != null)
                    {
                        IsSearchVisible = true;
                        FocusHelper.Focus(View.SearchBox);
                        View.SearchBox.SelectAll();
                    }
                },
                () => View != null
                    //&& View.SearchBox.Visibility == Visibility.Visible
                    && View.SearchBox.IsEnabled,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_TOCFindNext)
                );

            CommandFindNextMarkers = new RichDelegateCommand(
                @"MARKERS CommandFindNext DUMMY TXT", //UserInterfaceStrings.MarkersFindNext,
                @"MARKERS CommandFindNext DUMMY TXT", //UserInterfaceStrings.MarkersFindNext_,
                null, // KeyGesture set only for the top-level CompositeCommand
                null, () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    MarkersNavigator.FindNext(true);
                },
                () => MarkersNavigator != null && !string.IsNullOrEmpty(MarkersNavigator.SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_MarkersFindNext)
                );

            CommandFindPrevMarkers = new RichDelegateCommand(
                @"MARKERS CommandFindPrevious DUMMY TXT", //UserInterfaceStrings.MarkersFindPrev,
                @"MARKERS CommandFindPrevious DUMMY TXT", //UserInterfaceStrings.MarkersFindPrev_,
                null, // KeyGesture set only for the top-level CompositeCommand
                null, () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    MarkersNavigator.FindPrevious(true);
                },
                () => MarkersNavigator != null && !string.IsNullOrEmpty(MarkersNavigator.SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_MarkersFindPrev)
                );

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(onProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(onProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<MarkedTreeNodeFoundByFlowDocumentParserEvent>().Subscribe(onMarkedTreeNodeFoundByFlowDocumentParser, MarkedTreeNodeFoundByFlowDocumentParserEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Subscribe(OnTreeNodeSelectionChanged, TreeNodeSelectionChangedEvent.THREAD_OPTION);
        }

        public RichDelegateCommand CommandFindFocusMarkers { get; private set; }
        public RichDelegateCommand CommandFindNextMarkers { get; private set; }
        public RichDelegateCommand CommandFindPrevMarkers { get; private set; }

        public RichDelegateCommand CmdFindNextGlobal { get; private set; }
        public RichDelegateCommand CmdFindPreviousGlobal { get; private set; }

        [NotifyDependsOn("MarkersNavigator")]
        public bool IsSearchEnabled
        {
            get
            {
                return m_UrakawaSession.DocumentProject != null;
            }
        }

        ~MarkersPaneViewModel()
        {
            if (m_GlobalSearchCommand != null)
            {
                m_GlobalSearchCommand.CmdFindFocus.UnregisterCommand(CommandFindFocusMarkers);
                m_GlobalSearchCommand.CmdFindNext.UnregisterCommand(CommandFindNextMarkers);
                m_GlobalSearchCommand.CmdFindPrevious.UnregisterCommand(CommandFindPrevMarkers);
            }
#if DEBUG
            m_Logger.Log("MarkersPaneViewModel garbage collected.", Category.Debug, Priority.Medium);
#endif
        }
        #endregion Construction

        [Import(typeof(IGlobalSearchCommands), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = true, AllowDefault = true)]
        private IGlobalSearchCommands m_GlobalSearchCommand;

        private bool m_GlobalSearchCommandDone = false;
        private void trySearchCommands()
        {
            if (m_GlobalSearchCommand == null || m_GlobalSearchCommandDone)
            {
                return;
            }
            m_GlobalSearchCommandDone = true;

            m_GlobalSearchCommand.CmdFindFocus.RegisterCommand(CommandFindFocusMarkers);
            m_GlobalSearchCommand.CmdFindNext.RegisterCommand(CommandFindNextMarkers);
            m_GlobalSearchCommand.CmdFindPrevious.RegisterCommand(CommandFindPrevMarkers);

            CmdFindNextGlobal = m_GlobalSearchCommand.CmdFindNext;
            RaisePropertyChanged(() => CmdFindNextGlobal);

            CmdFindPreviousGlobal = m_GlobalSearchCommand.CmdFindPrevious;
            RaisePropertyChanged(() => CmdFindPreviousGlobal);
        }

        [NotifyDependsOn("MarkersNavigator")]
        public ObservableCollection<MarkedTreeNode> MarkersNavigator_MarkedTreeNodes
        {
            get
            {
                return MarkersNavigator == null ? null : MarkersNavigator.MarkedTreeNodes;
            }
        }

        private MarkersNavigator _markersNavigator;
        public MarkersNavigator MarkersNavigator
        {
            private set
            {
                _markersNavigator = value;
                RaisePropertyChanged(() => MarkersNavigator);
            }
            get { return _markersNavigator; }
        }


        public TreeNode SelectedTreeNode
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return null;
                var selection = m_UrakawaSession.GetTreeNodeSelection();

                return selection.Item2 ?? selection.Item1;
                //return selection.Item1;
            }
        }

        [NotifyDependsOn("SelectedTreeNode")]
        public bool CurrentTreeNodeSelectionExists
        {
            get
            {
                return SelectedTreeNode != null;
            }
        }

        [NotifyDependsOn("SelectedTreeNode")]
        public bool CurrentTreeNodeIsMarked
        {
            get
            {
                return SelectedTreeNode != null && SelectedTreeNode.IsMarked;
            }
            set
            {
                if (SelectedTreeNode == null) return;
                CommandToggleMark.Execute();
            }
        }

        [NotifyDependsOn("SelectedTreeNode")]
        public string CurrentTreeNodeLabel
        {
            get
            {
                if (SelectedTreeNode == null) return "";
                return MarkedTreeNode.GetMarkerDescription(SelectedTreeNode);
            }
        }

        private bool m_IsSearchVisible;
        public bool IsSearchVisible
        {
            get
            {
                return m_IsSearchVisible;
            }
            set
            {
                if (value == m_IsSearchVisible) return;
                m_IsSearchVisible = value;
                RaisePropertyChanged(() => IsSearchVisible);
            }
        }

        protected MarkersPanelView View { get; private set; }
        public void SetView(MarkersPanelView view)
        {
            View = view;

            ActiveAware = new FocusActiveAwareAdapter(View);
            ActiveAware.IsActiveChanged += (sender, e) => refreshCommandsIsActive();
            m_ShellView.ActiveAware.IsActiveChanged += (sender, e) => refreshCommandsIsActive();
        }

        public IActiveAware ActiveAware { get; private set; }

        private void refreshCommandsIsActive()
        {
            CommandFindFocusMarkers.IsActive = m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
            CommandFindNextMarkers.IsActive = m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
            CommandFindPrevMarkers.IsActive = m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
        }

        //private void ActiveAwareIsActiveChanged(object sender, EventArgs e)
        //{
        //    IActiveAware activeAware = (sender as IActiveAware);
        //    if (activeAware == null) { return; }
        //    CommandFindNextMarkers.IsActive = activeAware.IsActive;
        //    CommandFindPrevMarkers.IsActive = activeAware.IsActive;
        //}

        #region Events

        private bool checkTreeNodeFragmentRemoval(bool done, TreeNode node)
        {
            bool found = false;

            if (node.IsMarked)
            {
                if (done)
                {
                    MarkersNavigator.RemoveMarkedTreeNode(node);
                }
                else
                {
                    MarkersNavigator.AddMarkedTreeNode(node);
                }
                RaisePropertyChanged(() => HasNotMarkers);

                found = true;
            }
            foreach (var child in node.Children.ContentsAs_Enumerable)
            {
                bool found_ = checkTreeNodeFragmentRemoval(done, child);
                found = found || found_;
            }

            return found;
        }

        private void InvalidateMarkers(bool forceInvalidate, TreeNode node)
        {
            foreach (var markedTreeNode in MarkersNavigator_MarkedTreeNodes)
            {
                if (forceInvalidate
                    || node == markedTreeNode.TreeNode
                    || node.IsDescendantOf(markedTreeNode.TreeNode))
                {
                    markedTreeNode.InvalidateDescription();
                }
            }
        }

        private void OnUndoRedoManagerChanged_TreeNodeChangeTextCommand(UndoRedoManagerEventArgs eventt, bool done, TreeNodeChangeTextCommand command, bool isTransactionEndEvent, bool isNoTransactionOrTrailingEdge)
        {
            InvalidateMarkers(false, command.TreeNode);
        }

        private void OnUndoRedoManagerChanged_TreeNodeSetIsMarkedCommand(UndoRedoManagerEventArgs eventt, bool done, TreeNodeSetIsMarkedCommand command, bool isTransactionEndEvent, bool isNoTransactionOrTrailingEdge)
        {
            if (command.TreeNode.IsMarked)
                MarkersNavigator.AddMarkedTreeNode(command.TreeNode);
            else
                MarkersNavigator.RemoveMarkedTreeNode(command.TreeNode);

            RaisePropertyChanged(() => HasNotMarkers);
        }

        private void OnUndoRedoManagerChanged_TextNodeStructureEditCommand(UndoRedoManagerEventArgs eventt, bool done, TextNodeStructureEditCommand command, bool isTransactionEndEvent, bool isNoTransactionOrTrailingEdge)
        {
            DebugFix.Assert(command is TreeNodeInsertCommand || command is TreeNodeRemoveCommand);

            //TreeNode node = (command is TreeNodeInsertCommand) ? ((TreeNodeInsertCommand)command).TreeNode : ((TreeNodeRemoveCommand)command).TreeNode;
            TreeNode node = command.TreeNode;

            bool forceInvalidate = (command is TreeNodeInsertCommand && !done) || (command is TreeNodeRemoveCommand && done);
            InvalidateMarkers(forceInvalidate, node);
            
            bool done_ = (command is TreeNodeInsertCommand) ? !done : done;
            checkTreeNodeFragmentRemoval(done_, node);
        }

        public void OnUndoRedoManagerChanged(UndoRedoManagerEventArgs eventt, bool done, Command command, bool isTransactionEndEvent, bool isNoTransactionOrTrailingEdge)
        {
            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.Invoke(DispatcherPriority.Normal, (Action<UndoRedoManagerEventArgs, bool, Command, bool, bool>)OnUndoRedoManagerChanged, eventt, done, command, isTransactionEndEvent, isNoTransactionOrTrailingEdge);
                return;
            }

            if (isTransactionEndEvent)
            {
                return;
            }

            if (command is CompositeCommand)
            {
#if DEBUG
                Debugger.Break();
#endif
            }
            else if (command is TreeNodeSetIsMarkedCommand)
            {
                OnUndoRedoManagerChanged_TreeNodeSetIsMarkedCommand(eventt, done, (TreeNodeSetIsMarkedCommand)command, isTransactionEndEvent, isNoTransactionOrTrailingEdge);
            }
            else if (command is TreeNodeChangeTextCommand)
            {
                OnUndoRedoManagerChanged_TreeNodeChangeTextCommand(eventt, done, (TreeNodeChangeTextCommand)command, isTransactionEndEvent, isNoTransactionOrTrailingEdge);
            }
            else if (command is TextNodeStructureEditCommand)
            {
                OnUndoRedoManagerChanged_TextNodeStructureEditCommand(eventt, done, (TextNodeStructureEditCommand)command, isTransactionEndEvent, isNoTransactionOrTrailingEdge);
            }

            RaisePropertyChanged(() => SelectedTreeNode);
        }

        private UndoRedoManager.Hooker m_UndoRedoManagerHooker = null;

        private void onProjectLoaded(Project project)
        {
            if (m_UrakawaSession.IsXukSpine)
            {
                return;
            }

            m_UndoRedoManagerHooker = project.Presentations.Get(0).UndoRedoManager.Hook(this);

            MarkersNavigator = new MarkersNavigator(View);

            View.LoadProject();

            RaisePropertyChanged(() => SelectedTreeNode);
        }

        private void onProjectUnLoaded(Project project)
        {
            if (m_UndoRedoManagerHooker != null) m_UndoRedoManagerHooker.UnHook();
            m_UndoRedoManagerHooker = null;

            View.UnloadProject();
            MarkersNavigator = null;

            RaisePropertyChanged(() => SelectedTreeNode);
        }

        [NotifyDependsOn("MarkersNavigator")]
        public bool HasNotMarkers
        {
            get
            {
                return MarkersNavigator == null ? true : MarkersNavigator.MarkedTreeNodes.Count == 0;
            }
        }

        private void onMarkedTreeNodeFoundByFlowDocumentParser(TreeNode data)
        {
            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.Invoke(DispatcherPriority.Normal, (Action<TreeNode>)onMarkedTreeNodeFoundByFlowDocumentParser, data);
                return;
            }

            MarkersNavigator.AddMarkedTreeNode(data);

            RaisePropertyChanged(() => HasNotMarkers);
        }


        private void OnTreeNodeSelectionChanged(Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>> oldAndNewTreeNodeSelection)
        {
            Tuple<TreeNode, TreeNode> oldTreeNodeSelection = oldAndNewTreeNodeSelection.Item1;
            Tuple<TreeNode, TreeNode> newTreeNodeSelection = oldAndNewTreeNodeSelection.Item2;

            //View.UpdateMarkersListSelection(newTreeNodeSelection.Item2 ?? newTreeNodeSelection.Item1);

            RaisePropertyChanged(() => SelectedTreeNode);
        }
        //private void onTreeNodeSelected(TreeNode node)
        //{
        //    View.UpdateMarkersListSelection(node);
        //}
        //private void onSubTreeNodeSelected(TreeNode node)
        //{
        //    View.UpdateMarkersListSelection(node);
        //}
        public void OnImportsSatisfied()
        {
            trySearchCommands();
        }
        #endregion
    }
}
