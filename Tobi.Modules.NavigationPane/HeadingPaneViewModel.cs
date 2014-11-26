﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.commands;
using urakawa.core;
using urakawa.events.undo;
using urakawa.command;

namespace Tobi.Plugin.NavigationPane
{
    [Export(typeof(HeadingPaneViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public class HeadingPaneViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    {
        //        protected IUnityContainer Container { get; private set; }
        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;

        private readonly IShellView m_ShellView;
        private readonly IUrakawaSession m_session;

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        [ImportingConstructor]
        public HeadingPaneViewModel(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView view,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session)
        {
            //Container = container;
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            m_ShellView = view;
            m_session = session;

            intializeCommands();

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(onProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(onProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Subscribe(OnTreeNodeSelectionChanged, TreeNodeSelectionChangedEvent.THREAD_OPTION);
        }

        [NotifyDependsOn("HeadingsNavigator")]
        public bool IsSearchEnabled
        {
            get
            {
                return m_session.DocumentProject != null;
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

        private void intializeCommands()
        {
            m_Logger.Log("HeadingPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);

            //
            CommandExpandAll = new RichDelegateCommand(
                Tobi_Plugin_NavigationPane_Lang.CmdTreeExpandAll_ShortDesc,
                Tobi_Plugin_NavigationPane_Lang.CmdTreeExpandAll_LongDesc,
                null,
                m_ShellView.LoadTangoIcon("list-add"),
                () => HeadingsNavigator.ExpandAll(),
                () => HeadingsNavigator != null,
                null, null);
            //
            CommandCollapseAll = new RichDelegateCommand(
                Tobi_Plugin_NavigationPane_Lang.CmdTreeCollapseAll_ShortDesc,
                Tobi_Plugin_NavigationPane_Lang.CmdTreeCollapseAll_LongDesc,
                null,
                m_ShellView.LoadTangoIcon("list-remove"),
                () => HeadingsNavigator.CollapseAll(),
                () => HeadingsNavigator != null,
                null, null);
            //
            m_ShellView.RegisterRichCommand(CommandExpandAll);
            m_ShellView.RegisterRichCommand(CommandCollapseAll);
            //
            CommandFindFocus = new RichDelegateCommand(
                @"HEADINGS CommandFindFocus DUMMY TXT",
                @"HEADINGS CommandFindFocus DUMMY TXT",
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
            //
            CommandFindNext = new RichDelegateCommand(
                @"HEADINGS CommandFindNext DUMMY TXT", //UserInterfaceStrings.TreeFindNext,
                @"HEADINGS CommandFindNext DUMMY TXT", //UserInterfaceStrings.TreeFindNext_,
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    HeadingsNavigator.FindNext(true);
                },
                () => HeadingsNavigator != null && !string.IsNullOrEmpty(HeadingsNavigator.SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_TOCFindNext)
                );
            //
            CommandFindPrev = new RichDelegateCommand(
                @"HEADINGS CommandFindPrev DUMMY TXT", //UserInterfaceStrings.TreeFindPrev,
                @"HEADINGS CommandFindPrev DUMMY TXT", //UserInterfaceStrings.TreeFindPrev_,
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    HeadingsNavigator.FindPrevious(true);
                },
                () => HeadingsNavigator != null && !string.IsNullOrEmpty(HeadingsNavigator.SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_TOCFindPrev)
                );


            /*
                        CommandExpand = new RichDelegateCommand(
                            UserInterfaceStrings.TreeExpand,
                            UserInterfaceStrings.TreeExpand_,
                            null,
                            shellView.LoadGnomeNeuIcon("Neu_list-add"),
                            ()=> _headingsNavigator.Expand((HeadingTreeNodeWrapper)obj),
                            ()=> (_headingsNavigator != null && (obj as HeadingTreeNodeWrapper).HasChildren && !(obj as HeadingTreeNodeWrapper).IsExpanded));
             */
            //
            /*
                        CommandCollapse = new RichDelegateCommand(
                            UserInterfaceStrings.TreeCollapse,
                            UserInterfaceStrings.TreeCollapse_,
                            null,
                            shellView.LoadGnomeNeuIcon("Neu_list-remove"),
                            ()=> _headingsNavigator.Collapse((HeadingTreeNodeWrapper)obj),
                            ()=> (_headingsNavigator != null && (obj as HeadingTreeNodeWrapper).HasChildren && (obj as HeadingTreeNodeWrapper).IsExpanded));
            */
            /*CommandEditText = new RichDelegateCommand(
                UserInterfaceStrings.TreeEdit,
                UserInterfaceStrings.TreeEdit_,
                null,
                shellView.LoadTangoIcon("accessories-text-editor"),
                ()=> _headingsNavigator.EditText((HeadingTreeNodeWrapper)obj),
                ()=> _headingsNavigator!=null);*/

        }

        public RichDelegateCommand CommandExpandAll { get; private set; }
        public RichDelegateCommand CommandCollapseAll { get; private set; }

        public RichDelegateCommand CommandFindFocus { get; private set; }
        public RichDelegateCommand CommandFindNext { get; private set; }
        public RichDelegateCommand CommandFindPrev { get; private set; }

        ~HeadingPaneViewModel()
        {
            if (m_GlobalSearchCommand != null)
            {
                m_GlobalSearchCommand.CmdFindFocus.UnregisterCommand(CommandFindFocus);
                m_GlobalSearchCommand.CmdFindNext.UnregisterCommand(CommandFindNext);
                m_GlobalSearchCommand.CmdFindPrevious.UnregisterCommand(CommandFindPrev);
            }
#if DEBUG
            m_Logger.Log("HeadingPaneViewModel garbage collected.", Category.Debug, Priority.Medium);
#endif
        }

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

            m_GlobalSearchCommand.CmdFindFocus.RegisterCommand(CommandFindFocus);
            m_GlobalSearchCommand.CmdFindNext.RegisterCommand(CommandFindNext);
            m_GlobalSearchCommand.CmdFindPrevious.RegisterCommand(CommandFindPrev);

            CmdFindNextGlobal = m_GlobalSearchCommand.CmdFindNext;
            RaisePropertyChanged(() => CmdFindNextGlobal);

            CmdFindPreviousGlobal = m_GlobalSearchCommand.CmdFindPrevious;
            RaisePropertyChanged(() => CmdFindPreviousGlobal);
        }

        public RichDelegateCommand CmdFindNextGlobal { get; private set; }
        public RichDelegateCommand CmdFindPreviousGlobal { get; private set; }

        public HeadingPanelView View { get; private set; }

        public void SetView(HeadingPanelView view)
        {
            View = view;

            ActiveAware = new FocusActiveAwareAdapter(View);
            ActiveAware.IsActiveChanged += (sender, e) => refreshCommandsIsActive();
            m_ShellView.ActiveAware.IsActiveChanged += (sender, e) => refreshCommandsIsActive();
        }

        public IActiveAware ActiveAware { get; private set; }

        private void refreshCommandsIsActive()
        {
            CommandFindFocus.IsActive = m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
            CommandFindNext.IsActive = m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
            CommandFindPrev.IsActive = m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
        }

        [NotifyDependsOn("HeadingsNavigator")]
        public ObservableCollection<HeadingTreeNodeWrapper> HeadingsNavigator_Roots
        {
            get
            {
                return HeadingsNavigator == null ? null : HeadingsNavigator.Roots;
            }
        }

        private HeadingsNavigator _headingsNavigator;
        public HeadingsNavigator HeadingsNavigator
        {
            private set
            {
                _headingsNavigator = value;
                RaisePropertyChanged(() => HeadingsNavigator);
            }
            get { return _headingsNavigator; }
        }

        private void OnUndoRedoManagerChanged(object sender, UndoRedoManagerEventArgs eventt)
        {
            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.Invoke(DispatcherPriority.Normal, (Action<object, UndoRedoManagerEventArgs>)OnUndoRedoManagerChanged, sender, eventt);
                return;
            }

            //m_Logger.Log("DocumentPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);

            if (!(eventt is DoneEventArgs
                           || eventt is UnDoneEventArgs
                           || eventt is ReDoneEventArgs
                           || eventt is TransactionEndedEventArgs
                           || eventt is TransactionCancelledEventArgs
                           ))
            {
                //Debug.Fail("This should never happen !!");
                return;
            }

            if (!(eventt.Command is TreeNodeChangeTextCommand)
                && !(eventt.Command is TreeNodeInsertCommand)
                && !(eventt.Command is TreeNodeRemoveCommand)
                && !(eventt.Command is CompositeCommand)
                )
            {
                return;
            }


            if (m_session.DocumentProject.Presentations.Get(0).UndoRedoManager.IsTransactionActive)
            {
                DebugFix.Assert(eventt is DoneEventArgs || eventt is TransactionEndedEventArgs);
                //m_Logger.Log("DocumentPaneViewModel.OnUndoRedoManagerChanged (exit: ongoing TRANSACTION...)", Category.Debug, Priority.Medium);
                //return;
            }

            bool done = eventt is DoneEventArgs || eventt is ReDoneEventArgs || eventt is TransactionEndedEventArgs;
            DebugFix.Assert(done == !(eventt is UnDoneEventArgs || eventt is TransactionCancelledEventArgs));




            if (eventt.Command is CompositeCommand)
            {
                var compo = (CompositeCommand)eventt.Command;
                bool allStructEdits = true;
                foreach (Command command in compo.ChildCommands.ContentsAs_Enumerable)
                {
                    if (!(command is TextNodeStructureEditCommand))
                    {
                        allStructEdits = false;
                        break;
                    }
                }
                //if (allStructEdits && compo.ChildCommands.Count > 0)
                //{
                //    cmd = compo.ChildCommands.Get(compo.ChildCommands.Count - 1); //last
                //}
                if (allStructEdits)
                {
                    //if (!done)
                    //{
                    //    for (var i = compo.ChildCommands.Count - 1; i >= 0; i--)
                    //    {
                    //        Command command = compo.ChildCommands.Get(i);

                    //        TreeNode node = (command is TreeNodeInsertCommand) ? ((TreeNodeInsertCommand)command).TreeNode : ((TreeNodeRemoveCommand)command).TreeNode;
                    //        bool done_ = (command is TreeNodeInsertCommand) ? !done : done;

                    //        foreach (var headingTreeNodeWrapper in HeadingsNavigator_Roots)
                    //        {
                    //            //if ((command is TreeNodeInsertCommand && !done) || (command is TreeNodeRemoveCommand && done)
                    //            //    || (node == headingTreeNodeWrapper.WrappedTreeNode_LevelHeading
                    //            //    || node == headingTreeNodeWrapper.WrappedTreeNode_Level
                    //            //    || headingTreeNodeWrapper.WrappedTreeNode_LevelHeading != null && node.IsDescendantOf(headingTreeNodeWrapper.WrappedTreeNode_LevelHeading)
                    //            //    //|| node.IsDescendantOf(headingTreeNodeWrapper.WrappedTreeNode_Level)
                    //            //))
                    //            if (true)
                    //            {
                    //                checkNodeTitleChanged(headingTreeNodeWrapper, node);
                    //            }
                    //        }
                    //    }
                    //}
                    //else if (eventt is ReDoneEventArgs)
                    //{
                    //    //foreach (Command command in compo.ChildCommands.ContentsAs_Enumerable)
                    //    for (var i = 0; i < compo.ChildCommands.Count; i++)
                    //    {
                    //        Command command = compo.ChildCommands.Get(i);

                    //        TreeNode node = (command is TreeNodeInsertCommand) ? ((TreeNodeInsertCommand)command).TreeNode : ((TreeNodeRemoveCommand)command).TreeNode;
                    //        bool done_ = (command is TreeNodeInsertCommand) ? !done : done;

                    //        foreach (var headingTreeNodeWrapper in HeadingsNavigator_Roots)
                    //        {
                    //            //if ((command is TreeNodeInsertCommand && !done) || (command is TreeNodeRemoveCommand && done)
                    //            //    || (node == headingTreeNodeWrapper.WrappedTreeNode_LevelHeading
                    //            //    || node == headingTreeNodeWrapper.WrappedTreeNode_Level
                    //            //    || headingTreeNodeWrapper.WrappedTreeNode_LevelHeading != null && node.IsDescendantOf(headingTreeNodeWrapper.WrappedTreeNode_LevelHeading)
                    //            //    //|| node.IsDescendantOf(headingTreeNodeWrapper.WrappedTreeNode_Level)
                    //            //))
                    //            if (true)
                    //            {
                    //                checkNodeTitleChanged(headingTreeNodeWrapper, node);
                    //            }
                    //        }
                    //    }
                    //}

                    View.UnloadProject();
                    HeadingsNavigator = null;

                    HeadingsNavigator = new HeadingsNavigator(m_session.DocumentProject, this);
                    View.LoadProject();

                    return;
                }
            }

            var com = eventt.Command;
            if (com is TreeNodeInsertCommand || com is TreeNodeRemoveCommand)
            {
                //TreeNode node = (com is TreeNodeInsertCommand) ? ((TreeNodeInsertCommand)com).TreeNode : ((TreeNodeRemoveCommand)com).TreeNode;
                //bool done_ = (com is TreeNodeInsertCommand) ? !done : done;

                //foreach (var headingTreeNodeWrapper in HeadingsNavigator_Roots)
                //{
                //    //if ((com is TreeNodeInsertCommand && !done) || (com is TreeNodeRemoveCommand && done)
                //    //    || (node == headingTreeNodeWrapper.WrappedTreeNode_LevelHeading
                //    //    || node == headingTreeNodeWrapper.WrappedTreeNode_Level
                //    //    || headingTreeNodeWrapper.WrappedTreeNode_LevelHeading != null && node.IsDescendantOf(headingTreeNodeWrapper.WrappedTreeNode_LevelHeading)
                //    //    //|| node.IsDescendantOf(headingTreeNodeWrapper.WrappedTreeNode_Level)
                //    //))
                //    if (true)
                //    {
                //        checkNodeTitleChanged(headingTreeNodeWrapper, node);
                //    }
                //}

                View.UnloadProject();
                HeadingsNavigator = null;

                HeadingsNavigator = new HeadingsNavigator(m_session.DocumentProject, this);
                View.LoadProject();

                return;
            }


            var cmd = eventt.Command as TreeNodeChangeTextCommand;

            if (cmd == null) return;

            foreach (var headingTreeNodeWrapper in HeadingsNavigator_Roots)
            {
                checkNodeTitleChanged(headingTreeNodeWrapper, cmd.TreeNode);
            }
        }

        private void checkNodeTitleChanged(HeadingTreeNodeWrapper parent, TreeNode treeNode)
        {
            //var wrappedNode = headingTreeNodeWrapper.WrappedTreeNode_LevelHeading ?? headingTreeNodeWrapper.WrappedTreeNode_Level;

            if (
                parent.WrappedTreeNode_LevelHeading != null
                &&
                (treeNode == parent.WrappedTreeNode_LevelHeading
                   || treeNode.IsDescendantOf(parent.WrappedTreeNode_LevelHeading)))
            {
                parent.InvalidateTitle();
            }

            foreach (var child in parent.Children)
            {
                checkNodeTitleChanged(child, treeNode);
            }
        }

        private void onProjectLoaded(Project project)
        {
            if (m_session.IsXukSpine)
            {
                return;
            }

            project.Presentations.Get(0).UndoRedoManager.CommandDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionEnded += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionCancelled += OnUndoRedoManagerChanged;

            HeadingsNavigator = new HeadingsNavigator(project, this);

            View.LoadProject();
        }

        private void onProjectUnLoaded(Project project)
        {
            project.Presentations.Get(0).UndoRedoManager.CommandDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionEnded -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionCancelled -= OnUndoRedoManagerChanged;

            View.UnloadProject();

            HeadingsNavigator = null;
        }

        private void OnTreeNodeSelectionChanged(Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>> oldAndNewTreeNodeSelection)
        {
            Tuple<TreeNode, TreeNode> oldTreeNodeSelection = oldAndNewTreeNodeSelection.Item1;
            Tuple<TreeNode, TreeNode> newTreeNodeSelection = oldAndNewTreeNodeSelection.Item2;

            View.SelectTreeNode(newTreeNodeSelection.Item2 ?? newTreeNodeSelection.Item1, false);
        }

        public void OnImportsSatisfied()
        {
            trySearchCommands();
        }
    }
}