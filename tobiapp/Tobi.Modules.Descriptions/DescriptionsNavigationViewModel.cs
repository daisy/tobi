using System;
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
using urakawa.property.alt;

namespace Tobi.Plugin.Descriptions
{
    [Export(typeof(DescriptionsNavigationViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public class DescriptionsNavigationViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    {
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
        public DescriptionsNavigationViewModel(
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


            m_Logger.Log("DescriptionsNavigationViewModel.initializeCommands", Category.Debug, Priority.Medium);

            CommandFindFocusDescriptions = new RichDelegateCommand(
                @"DESCRIPTIONS CommandFindFocus DUMMY TXT",
                @"DESCRIPTIONS CommandFindFocus DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () =>
                {
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

            CommandFindNextDescription = new RichDelegateCommand(
                @"DESCRIPTIONS CommandFindNext DUMMY TXT",
                @"DESCRIPTIONS CommandFindNext DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null, () => DescriptionsNavigator.FindNext(true),
                () => DescriptionsNavigator != null && !string.IsNullOrEmpty(DescriptionsNavigator.SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_DescriptionsFindNext)
                );

            CommandFindPrevDescription = new RichDelegateCommand(
                @"DESCRIPTIONS CommandFindPrevious DUMMY TXT",
                @"DESCRIPTIONS CommandFindPrevious DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null, () => DescriptionsNavigator.FindPrevious(true),
                () => DescriptionsNavigator != null && !string.IsNullOrEmpty(DescriptionsNavigator.SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_DescFindPrev)
                );

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(onProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(onProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<DescribedTreeNodeFoundByFlowDocumentParserEvent>().Subscribe(onDescribedTreeNodeFoundByFlowDocumentParser, DescribedTreeNodeFoundByFlowDocumentParserEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Subscribe(OnTreeNodeSelectionChanged, TreeNodeSelectionChangedEvent.THREAD_OPTION);
        }

        public RichDelegateCommand CommandFindFocusDescriptions { get; private set; }
        public RichDelegateCommand CommandFindNextDescription { get; private set; }
        public RichDelegateCommand CommandFindPrevDescription { get; private set; }

        public RichDelegateCommand CmdFindNextGlobal { get; private set; }
        public RichDelegateCommand CmdFindPreviousGlobal { get; private set; }

        [NotifyDependsOn("DescriptionsNavigator")]
        public bool IsSearchEnabled
        {
            get
            {
                return m_UrakawaSession.DocumentProject != null;
            }
        }

        ~DescriptionsNavigationViewModel()
        {
            if (m_GlobalSearchCommand != null)
            {
                m_GlobalSearchCommand.CmdFindFocus.UnregisterCommand(CommandFindFocusDescriptions);
                m_GlobalSearchCommand.CmdFindNext.UnregisterCommand(CommandFindNextDescription);
                m_GlobalSearchCommand.CmdFindPrevious.UnregisterCommand(CommandFindPrevDescription);
            }
#if DEBUG
            m_Logger.Log("DescriptionsNavigationViewModel garbage collected.", Category.Debug, Priority.Medium);
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

            m_GlobalSearchCommand.CmdFindFocus.RegisterCommand(CommandFindFocusDescriptions);
            m_GlobalSearchCommand.CmdFindNext.RegisterCommand(CommandFindNextDescription);
            m_GlobalSearchCommand.CmdFindPrevious.RegisterCommand(CommandFindPrevDescription);

            CmdFindNextGlobal = m_GlobalSearchCommand.CmdFindNext;
            RaisePropertyChanged(() => CmdFindNextGlobal);

            CmdFindPreviousGlobal = m_GlobalSearchCommand.CmdFindPrevious;
            RaisePropertyChanged(() => CmdFindPreviousGlobal);
        }

        [NotifyDependsOn("DescriptionsNavigator")]
        public ObservableCollection<DescribedTreeNode> DescriptionsNavigator_DescribedTreeNodes
        {
            get
            {
                return DescriptionsNavigator == null ? null : DescriptionsNavigator.DescribedTreeNodes;
            }
        }

        private DescriptionsNavigator _descriptionsNavigator;
        public DescriptionsNavigator DescriptionsNavigator
        {
            private set
            {
                _descriptionsNavigator = value;
                RaisePropertyChanged(() => DescriptionsNavigator);
            }
            get { return _descriptionsNavigator; }
        }


        public TreeNode SelectedTreeNode
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null) return null;
                var selection = m_UrakawaSession.GetTreeNodeSelection();
                return selection.Item2 ?? selection.Item1;
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
        public bool CurrentTreeNodeIsDescribed
        {
            get
            {
                return SelectedTreeNode != null && SelectedTreeNode.HasAlternateContentProperty;
            }
        }

        [NotifyDependsOn("SelectedTreeNode")]
        public string CurrentTreeNodeLabel
        {
            get
            {
                if (SelectedTreeNode == null) return "";
                return DescribedTreeNode.GetDescriptionLabel(SelectedTreeNode);
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

        protected DescriptionsNavigationView View { get; private set; }
        public void SetView(DescriptionsNavigationView view)
        {
            View = view;

            ActiveAware = new FocusActiveAwareAdapter(View);
            ActiveAware.IsActiveChanged += (sender, e) => refreshCommandsIsActive();
            m_ShellView.ActiveAware.IsActiveChanged += (sender, e) => refreshCommandsIsActive();
        }

        public IActiveAware ActiveAware { get; private set; }

        private void refreshCommandsIsActive()
        {
            CommandFindFocusDescriptions.IsActive = m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
            CommandFindNextDescription.IsActive = m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
            CommandFindPrevDescription.IsActive = m_ShellView.ActiveAware.IsActive && ActiveAware.IsActive;
        }

        //private void ActiveAwareIsActiveChanged(object sender, EventArgs e)
        //{
        //    IActiveAware activeAware = (sender as IActiveAware);
        //    if (activeAware == null) { return; }
        //    CommandFindNextDescription.IsActive = activeAware.IsActive;
        //    CommandFindPrevDescription.IsActive = activeAware.IsActive;
        //}

        #region Events
        private void onProjectLoaded(Project project)
        {
            DescriptionsNavigator = new DescriptionsNavigator(View);
            View.LoadProject();

            RaisePropertyChanged(() => SelectedTreeNode);

            project.Presentations.Get(0).UndoRedoManager.CommandDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionCancelled += OnUndoRedoManagerChanged;
        }

        private void onProjectUnLoaded(Project project)
        {
            View.UnloadProject();
            DescriptionsNavigator = null;

            RaisePropertyChanged(() => SelectedTreeNode);

            project.Presentations.Get(0).UndoRedoManager.CommandDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.TransactionCancelled += OnUndoRedoManagerChanged;
        }
        private void OnUndoRedoManagerChanged(object sender, UndoRedoManagerEventArgs eventt)
        {
            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.Invoke(DispatcherPriority.Normal,
                                  (Action<object, UndoRedoManagerEventArgs>)OnUndoRedoManagerChanged, sender, eventt);
                return;
            }

            if (!(eventt is DoneEventArgs
                  || eventt is UnDoneEventArgs
                  || eventt is ReDoneEventArgs
                  || eventt is TransactionCancelledEventArgs
                 ))
            {
                Debug.Fail("This should never happen !!");
                return;
            }

            if (!(eventt.Command is AlternateContentAddCommand)
                && !(eventt.Command is AlternateContentRemoveCommand)
                && !(eventt.Command is AlternateContentMetadataAddCommand)
                && !(eventt.Command is AlternateContentMetadataRemoveCommand)
                && !(eventt.Command is AlternateContentSetManagedMediaCommand)
                && !(eventt.Command is AlternateContentRemoveManagedMediaCommand)
                && !(eventt.Command is TreeNodeChangeTextCommand)
                && !(eventt.Command is CompositeCommand)
                )
            {
                return;
            }

            if (eventt.Command is TreeNodeChangeTextCommand)
            {
                var node = ((TreeNodeChangeTextCommand)eventt.Command).TreeNode;

                foreach (var describedTreeNode in DescriptionsNavigator_DescribedTreeNodes)
                {
                    if (node == describedTreeNode.TreeNode
                        || node.IsDescendantOf(describedTreeNode.TreeNode))
                    {
                        describedTreeNode.RaiseDescriptionChanged();
                    }
                }
                return;
            }

            RaisePropertyChanged(() => SelectedTreeNode);

            TreeNode treeNode = null;

            if (eventt.Command is CompositeCommand)
            {
                foreach (var childCmd in ((CompositeCommand)eventt.Command).ChildCommands.ContentsAs_Enumerable)
                {
                    if (childCmd is AlternateContentAddCommand)
                    {
                        treeNode = ((AlternateContentAddCommand)childCmd).TreeNode;
                        break;
                    }
                    if (childCmd is AlternateContentRemoveCommand)
                    {
                        treeNode = ((AlternateContentRemoveCommand)childCmd).TreeNode;
                        break;
                    }
                    if (childCmd is AlternateContentMetadataAddCommand)
                    {
                        treeNode = ((AlternateContentMetadataAddCommand)childCmd).TreeNode;
                        break;
                    }
                    if (childCmd is AlternateContentMetadataRemoveCommand)
                    {
                        treeNode = ((AlternateContentMetadataRemoveCommand)childCmd).TreeNode;
                        break;
                    }
                    if (childCmd is AlternateContentSetManagedMediaCommand)
                    {
                        treeNode = ((AlternateContentSetManagedMediaCommand)childCmd).TreeNode;
                        break;
                    }
                    if (childCmd is AlternateContentRemoveManagedMediaCommand)
                    {
                        treeNode = ((AlternateContentRemoveManagedMediaCommand)childCmd).TreeNode;
                        break;
                    }
                }
            }
            else if (eventt.Command is AlternateContentAddCommand)
            {
                treeNode = ((AlternateContentAddCommand)eventt.Command).TreeNode;
            }
            else if (eventt.Command is AlternateContentRemoveCommand)
            {
                treeNode = ((AlternateContentRemoveCommand)eventt.Command).TreeNode;
            }
            else if (eventt.Command is AlternateContentMetadataAddCommand)
            {
                treeNode = ((AlternateContentMetadataAddCommand)eventt.Command).TreeNode;
            }
            else if (eventt.Command is AlternateContentMetadataRemoveCommand)
            {
                treeNode = ((AlternateContentMetadataRemoveCommand)eventt.Command).TreeNode;
            }
            else if (eventt.Command is AlternateContentSetManagedMediaCommand)
            {
                treeNode = ((AlternateContentSetManagedMediaCommand)eventt.Command).TreeNode;
            }
            else if (eventt.Command is AlternateContentRemoveManagedMediaCommand)
            {
                treeNode = ((AlternateContentRemoveManagedMediaCommand)eventt.Command).TreeNode;
            }

            if (treeNode == null) return;

            if (treeNode.HasAlternateContentProperty && !treeNode.GetAlternateContentProperty().IsEmpty)
            {
                bool treeNodeAlreadyRegistered = false;
                foreach (var describedTreeNode in DescriptionsNavigator.DescribedTreeNodes)
                {
                    if (describedTreeNode.TreeNode == treeNode)
                    {
                        treeNodeAlreadyRegistered = true;
                        break;
                    }
                }
                if (!treeNodeAlreadyRegistered)
                {
                    DescriptionsNavigator.AddDescribedTreeNode(treeNode);
                }
            }
            else
            {
                DescribedTreeNode nodeToRemove = null;
                foreach (var describedTreeNode in DescriptionsNavigator.DescribedTreeNodes)
                {
                    if (describedTreeNode.TreeNode == treeNode)
                    {
                        nodeToRemove = describedTreeNode;
                        break;
                    }
                }
                if (nodeToRemove != null)
                {
                    DescriptionsNavigator.RemoveDescribedTreeNode(nodeToRemove.TreeNode);
                }
            }
        }

        private void onDescribedTreeNodeFoundByFlowDocumentParser(TreeNode data)
        {
            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.Invoke(DispatcherPriority.Normal, (Action<TreeNode>)onDescribedTreeNodeFoundByFlowDocumentParser, data);
                return;
            }
            DescriptionsNavigator.AddDescribedTreeNode(data);
        }


        private void OnTreeNodeSelectionChanged(Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>> oldAndNewTreeNodeSelection)
        {
            Tuple<TreeNode, TreeNode> oldTreeNodeSelection = oldAndNewTreeNodeSelection.Item1;
            Tuple<TreeNode, TreeNode> newTreeNodeSelection = oldAndNewTreeNodeSelection.Item2;
            //Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = newTreeNodeSelection.Item2 ?? newTreeNodeSelection.Item1;

            foreach (var describedTreeNode in DescriptionsNavigator_DescribedTreeNodes)
            {
                describedTreeNode.IsSelected = describedTreeNode.TreeNode == node;
            }

            RaisePropertyChanged(() => SelectedTreeNode);
        }

        public void OnImportsSatisfied()
        {
            trySearchCommands();
        }
        #endregion
    }
}