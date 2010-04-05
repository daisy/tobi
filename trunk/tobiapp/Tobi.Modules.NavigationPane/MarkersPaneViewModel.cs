using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
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

namespace Tobi.Plugin.NavigationPane
{
    [Export(typeof(MarkersPaneViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public class MarkersPaneViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    {
        private MarkersNavigator _markersNavigator;
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

            CommandFindFocusMarkers = new RichDelegateCommand(
                @"DUMMY TXT",
                @"DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () => { if (View != null) FocusHelper.Focus(View.SearchBox); },
                () => View != null && View.SearchBox.Visibility == Visibility.Visible,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_TOCFindNext)
                );

            CommandFindNextMarkers = new RichDelegateCommand(
                @"DUMMY TXT", //UserInterfaceStrings.MarkersFindNext,
                @"DUMMY TXT", //UserInterfaceStrings.MarkersFindNext_,
                null, // KeyGesture set only for the top-level CompositeCommand
                null, () => _markersNavigator.FindNext(),
                () => _markersNavigator != null,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_MarkersFindNext)
                );

            CommandFindPrevMarkers = new RichDelegateCommand(
                @"DUMMY TXT", //UserInterfaceStrings.MarkersFindPrev,
                @"DUMMY TXT", //UserInterfaceStrings.MarkersFindPrev_,
                null, // KeyGesture set only for the top-level CompositeCommand
                null, () => _markersNavigator.FindPrevious(),
                () => _markersNavigator != null,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_MarkersFindPrev)
                );

            //m_ShellView.RegisterRichCommand(CommandFindNextMarkers);
            //m_ShellView.RegisterRichCommand(CommandFindPrevMarkers);

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(onProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(onProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<MarkedTreeNodeFoundByFlowDocumentParserEvent>().Subscribe(onMarkedTreeNodeFoundByFlowDocumentParser, MarkedTreeNodeFoundByFlowDocumentParserEvent.THREAD_OPTION);

            //m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(onTreeNodeSelected, TreeNodeSelectedEvent.THREAD_OPTION);
            //m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Subscribe(onSubTreeNodeSelected, TreeNodeSelectedEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Subscribe(OnTreeNodeSelectionChanged, TreeNodeSelectionChangedEvent.THREAD_OPTION);
        }

        public RichDelegateCommand CommandFindFocusMarkers { get; private set; }
        public RichDelegateCommand CommandFindNextMarkers { get; private set; }
        public RichDelegateCommand CommandFindPrevMarkers { get; private set; }

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

        private void trySearchCommands()
        {
            if (m_GlobalSearchCommand == null) { return; }

            m_GlobalSearchCommand.CmdFindFocus.RegisterCommand(CommandFindFocusMarkers);
            m_GlobalSearchCommand.CmdFindNext.RegisterCommand(CommandFindNextMarkers);
            m_GlobalSearchCommand.CmdFindPrevious.RegisterCommand(CommandFindPrevMarkers);
        }

        public MarkersNavigator MarkersNavigator
        {
            get { return _markersNavigator; }
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
        public bool CurrentTreeNodeIsMarked
        {
            get
            {
                return SelectedTreeNode != null && SelectedTreeNode.IsMarked;
            }
            set
            {
                if (SelectedTreeNode == null) return;
                var cmd = SelectedTreeNode.Presentation.CommandFactory.CreateTreeNodeSetIsMarkedCommand(SelectedTreeNode, value);
                SelectedTreeNode.Presentation.UndoRedoManager.Execute(cmd);
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


        protected MarkersPanelView View { get; private set; }
        public void SetView(MarkersPanelView view)
        {
            View = view;

            var focusAware = new FocusActiveAwareAdapter(View);
            focusAware.IsActiveChanged += (sender, e) =>
            {
                // ALWAYS ACTIVE ! CommandFindFocusMarkers.IsActive = focusAware.IsActive;
                CommandFindNextMarkers.IsActive = focusAware.IsActive;
                CommandFindPrevMarkers.IsActive = focusAware.IsActive;
            };

            //IActiveAware activeAware = View as IActiveAware;
            //if (activeAware != null) { activeAware.IsActiveChanged += ActiveAwareIsActiveChanged; }
        }

        //private void ActiveAwareIsActiveChanged(object sender, EventArgs e)
        //{
        //    IActiveAware activeAware = (sender as IActiveAware);
        //    if (activeAware == null) { return; }
        //    CommandFindNextMarkers.IsActive = activeAware.IsActive;
        //    CommandFindPrevMarkers.IsActive = activeAware.IsActive;
        //}

        #region Events
        private void onProjectLoaded(Project project)
        {
            _markersNavigator = new MarkersNavigator(this);
            View.LoadProject();

            RaisePropertyChanged(() => SelectedTreeNode);

            project.Presentations.Get(0).UndoRedoManager.CommandDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone += OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone += OnUndoRedoManagerChanged;
            //project.Presentations.Get(0).UndoRedoManager.TransactionEnded += OnUndoRedoManagerChanged;
        }

        private void onProjectUnLoaded(Project project)
        {
            View.UnloadProject();
            _markersNavigator = null;

            RaisePropertyChanged(() => SelectedTreeNode);

            project.Presentations.Get(0).UndoRedoManager.CommandDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandReDone -= OnUndoRedoManagerChanged;
            project.Presentations.Get(0).UndoRedoManager.CommandUnDone -= OnUndoRedoManagerChanged;
            //project.Presentations.Get(0).UndoRedoManager.TransactionEnded += OnUndoRedoManagerChanged;
        }
        private void OnUndoRedoManagerChanged(object sender, UndoRedoManagerEventArgs eventt)
        {
            m_Logger.Log("MarkersPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);

            if (!(eventt is DoneEventArgs
                  || eventt is UnDoneEventArgs
                  || eventt is ReDoneEventArgs
                  //|| eventt is TransactionEndedEventArgs
                  ))
            {
                Debug.Fail("This should never happen !!");
                return;
            }

            //if (eventt is DoneEventArgs &&
            //    m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.IsTransactionActive)
            //{
            //    m_Logger.Log("MarkersPaneViewModel.OnUndoRedoManagerChanged (exit: ongoing TRANSACTION...)",
            //                 Category.Debug, Priority.Medium);
            //    return;
            //}

            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal,
                                  (Action<object, UndoRedoManagerEventArgs>) OnUndoRedoManagerChanged, sender, eventt);
                return;
            }

            if (eventt.Command is TreeNodeSetIsMarkedCommand)
            {
                RaisePropertyChanged(() => SelectedTreeNode);
                var cmd = (TreeNodeSetIsMarkedCommand) eventt.Command;

                if (cmd.TreeNode.IsMarked)
                    _markersNavigator.AddMarkedTreeNode(cmd.TreeNode);
                else
                    _markersNavigator.RemoveMarkedTreeNode(cmd.TreeNode);
            }
        }

        private void onMarkedTreeNodeFoundByFlowDocumentParser(TreeNode data)
        {
            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action<TreeNode>)onMarkedTreeNodeFoundByFlowDocumentParser, data);
                return;
            }
            _markersNavigator.AddMarkedTreeNode(data);
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
