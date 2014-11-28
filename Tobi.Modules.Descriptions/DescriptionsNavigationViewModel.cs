using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
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
using urakawa.command;
using urakawa.commands;
using urakawa.core;
using urakawa.events.undo;
using urakawa.property.xml;
using urakawa.undo;

namespace Tobi.Plugin.Descriptions
{
    [Export(typeof(DescriptionsNavigationViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public class DescriptionsNavigationViewModel : ViewModelBase, IPartImportsSatisfiedNotification, UndoRedoManager.Hooker.Host
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

            CommandFindNextDescription = new RichDelegateCommand(
                @"DESCRIPTIONS CommandFindNext DUMMY TXT",
                @"DESCRIPTIONS CommandFindNext DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null, () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    DescriptionsNavigator.FindNext(true);
                },
                () => DescriptionsNavigator != null && !string.IsNullOrEmpty(DescriptionsNavigator.SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_DescriptionsFindNext)
                );

            CommandFindPrevDescription = new RichDelegateCommand(
                @"DESCRIPTIONS CommandFindPrevious DUMMY TXT",
                @"DESCRIPTIONS CommandFindPrevious DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null, () =>
                {
                    m_ShellView.RaiseEscapeEvent();

                    DescriptionsNavigator.FindPrevious(true);
                },
                () => DescriptionsNavigator != null && !string.IsNullOrEmpty(DescriptionsNavigator.SearchTerm),
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_DescFindPrev)
                );

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(onProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(onProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<DescribableTreeNodeFoundByFlowDocumentParserEvent>().Subscribe(onDescribableTreeNodeFoundByFlowDocumentParser, DescribableTreeNodeFoundByFlowDocumentParserEvent.THREAD_OPTION);

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
        public ObservableCollection<DescribableTreeNode> DescriptionsNavigator_DescribableTreeNodes
        {
            get
            {
                return DescriptionsNavigator == null ? null : DescriptionsNavigator.DescribableTreeNodes;
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

        //[NotifyDependsOn("SelectedTreeNode")]
        //public bool CurrentTreeNodeIsDescribed
        //{
        //    get
        //    {
        //        return SelectedTreeNode != null && SelectedTreeNode.HasAlternateContentProperty;
        //    }
        //}

        //[NotifyDependsOn("SelectedTreeNode")]
        //public string CurrentTreeNodeLabel
        //{
        //    get
        //    {
        //        if (SelectedTreeNode == null) return "";
        //        return DescribableTreeNode.GetDescriptionLabel(SelectedTreeNode);
        //    }
        //}

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


        private bool m_ShowEmptyAlt = true;
        //[NotifyDependsOn("SelectedTreeNode")]
        public bool ShowEmptyAlt
        {
            get { return m_ShowEmptyAlt; }
            set
            {
                m_ShowEmptyAlt = value;

                if (DescriptionsNavigator == null) return;

                DescriptionsNavigator.InitWithBackupTreeNodes(m_ShowEmptyAlt);

                RaisePropertyChanged(() => DescriptionsNavigator);
            }
        }

        //private void ActiveAwareIsActiveChanged(object sender, EventArgs e)
        //{
        //    IActiveAware activeAware = (sender as IActiveAware);
        //    if (activeAware == null) { return; }
        //    CommandFindNextDescription.IsActive = activeAware.IsActive;
        //    CommandFindPrevDescription.IsActive = activeAware.IsActive;
        //}

        #region Events



        private bool checkTreeNodeFragmentRemoval(bool done, TreeNode node)
        {
            if (node.HasXmlProperty
                && node.GetXmlElementLocalName().Equals("img", StringComparison.OrdinalIgnoreCase))
            {
                if (done)
                {
                    DescriptionsNavigator.RemoveDescribableTreeNode(node);
                }
                else
                {
                    DescriptionsNavigator.AddDescribableTreeNode(node);
                }
                RaisePropertyChanged(() => HasNotDescribableTreeNodes);

                return true; // break
            }
            foreach (var child in node.Children.ContentsAs_Enumerable)
            {
                if (checkTreeNodeFragmentRemoval(done, child))
                {
                    return true;
                }
            }
            return false;
        }

        //if (treeNode.HasAlternateContentProperty && !treeNode.GetAlternateContentProperty().IsEmpty)
        //{
        //    bool treeNodeAlreadyRegistered = false;
        //    foreach (var describableTreeNode in DescriptionsNavigator.DescribableTreeNodes)
        //    {
        //        if (describableTreeNode.TreeNode == treeNode)
        //        {
        //            treeNodeAlreadyRegistered = true;
        //            break;
        //        }
        //    }
        //    if (!treeNodeAlreadyRegistered)
        //    {
        //        DescriptionsNavigator.AddDescribableTreeNode(treeNode);
        //    }
        //}
        //else
        //{
        //    DescribableTreeNode nodeToRemove = null;
        //    foreach (var describableTreeNode in DescriptionsNavigator.DescribableTreeNodes)
        //    {
        //        if (describableTreeNode.TreeNode == treeNode)
        //        {
        //            nodeToRemove = describableTreeNode;
        //            break;
        //        }
        //    }
        //    if (nodeToRemove != null)
        //    {
        //        DescriptionsNavigator.RemoveDescribableTreeNode(nodeToRemove.TreeNode);
        //    }
        //}

        private void InvalidateDescriptions(bool forceInvalidate, TreeNode node)
        {
            foreach (var describableTreeNode in DescriptionsNavigator_DescribableTreeNodes)
            {
                if (forceInvalidate
                    || node == describableTreeNode.TreeNode
                    || node.IsDescendantOf(describableTreeNode.TreeNode))
                {
                    describableTreeNode.RaiseHasDescriptionChanged();
                    describableTreeNode.InvalidateDescription();
                }
            }
        }

        private void OnUndoRedoManagerChanged_AlternateContentCommand(UndoRedoManagerEventArgs eventt, bool isTransactionActive, bool done, AlternateContentCommand command)
        {
            if (command.TreeNode != null) InvalidateDescriptions(false, command.TreeNode);
        }

        private void OnUndoRedoManagerChanged_TreeNodeChangeTextCommand(UndoRedoManagerEventArgs eventt, bool isTransactionActive, bool done, TreeNodeChangeTextCommand command)
        {
            InvalidateDescriptions(false, command.TreeNode);
        }

        private void OnUndoRedoManagerChanged_TextNodeStructureEditCommand(UndoRedoManagerEventArgs eventt, bool isTransactionActive, bool done, TextNodeStructureEditCommand command)
        {
            DebugFix.Assert(command is TreeNodeInsertCommand || command is TreeNodeRemoveCommand);

            //TreeNode node = (command is TreeNodeInsertCommand) ? ((TreeNodeInsertCommand)command).TreeNode : ((TreeNodeRemoveCommand)command).TreeNode;
            TreeNode node = command.TreeNode;

            bool forceInvalidate = (command is TreeNodeInsertCommand && !done) || (command is TreeNodeRemoveCommand && done);
            InvalidateDescriptions(forceInvalidate, node);

            bool done_ = (command is TreeNodeInsertCommand) ? !done : done;
            checkTreeNodeFragmentRemoval(done_, node);
        }

        public void OnUndoRedoManagerChanged(UndoRedoManagerEventArgs eventt, bool isTransactionActive, bool done, Command command)
        {
            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.Invoke(DispatcherPriority.Normal, (Action<UndoRedoManagerEventArgs, bool, bool, Command>)OnUndoRedoManagerChanged, eventt, isTransactionActive, done, command);
                return;
            }

            if (command is CompositeCommand)
            {
#if DEBUG
                Debugger.Break();
#endif
            }
            else if (command is AlternateContentCommand)
            {
                OnUndoRedoManagerChanged_AlternateContentCommand(eventt, isTransactionActive, done, (AlternateContentCommand)command);
            }
            else if (command is TreeNodeChangeTextCommand)
            {
                OnUndoRedoManagerChanged_TreeNodeChangeTextCommand(eventt, isTransactionActive, done, (TreeNodeChangeTextCommand)command);
            }
            else if (command is TextNodeStructureEditCommand)
            {
                OnUndoRedoManagerChanged_TextNodeStructureEditCommand(eventt, isTransactionActive, done, (TextNodeStructureEditCommand)command);
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

            DescriptionsNavigator = new DescriptionsNavigator(View);
            View.LoadProject();

            RaisePropertyChanged(() => SelectedTreeNode);
        }

        private void onProjectUnLoaded(Project project)
        {
            if (m_UndoRedoManagerHooker != null) m_UndoRedoManagerHooker.UnHook();
            m_UndoRedoManagerHooker = null;

            View.UnloadProject();
            DescriptionsNavigator = null;

            RaisePropertyChanged(() => SelectedTreeNode);
        }

        [NotifyDependsOn("DescriptionsNavigator")]
        public bool HasNotDescribableTreeNodes
        {
            get
            {
                return DescriptionsNavigator == null ? true : DescriptionsNavigator.DescribableTreeNodes.Count == 0;
            }
        }

        private void onDescribableTreeNodeFoundByFlowDocumentParser(TreeNode data)
        {
            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.Invoke(DispatcherPriority.Normal, (Action<TreeNode>)onDescribableTreeNodeFoundByFlowDocumentParser, data);
                return;
            }
            DescriptionsNavigator.AddDescribableTreeNode(data);

            RaisePropertyChanged(() => HasNotDescribableTreeNodes);
        }


        private void OnTreeNodeSelectionChanged(Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>> oldAndNewTreeNodeSelection)
        {
            Tuple<TreeNode, TreeNode> oldTreeNodeSelection = oldAndNewTreeNodeSelection.Item1;
            Tuple<TreeNode, TreeNode> newTreeNodeSelection = oldAndNewTreeNodeSelection.Item2;
            //Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = newTreeNodeSelection.Item2 ?? newTreeNodeSelection.Item1;

            foreach (var describableTreeNode in DescriptionsNavigator_DescribableTreeNodes)
            {
                describableTreeNode.IsSelected = describableTreeNode.TreeNode == node;
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