using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.core;

namespace Tobi.Plugin.NavigationPane
{
    [Export(typeof(HeadingPaneViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public class HeadingPaneViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    {
        private HeadingsNavigator _headingsNavigator;

        //        protected IUnityContainer Container { get; private set; }
        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;

        private readonly IShellView m_ShellView;

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        [ImportingConstructor]
        public HeadingPaneViewModel(
            IEventAggregator eventAggregator,
            ILoggerFacade logger,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView view)
        {
            //Container = container;
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            m_ShellView = view;


            intializeCommands();

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(onProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(onProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);
            
            //m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(onTreeNodeSelected, TreeNodeSelectedEvent.THREAD_OPTION);
            //m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Subscribe(onSubTreeNodeSelected, SubTreeNodeSelectedEvent.THREAD_OPTION);

            m_EventAggregator.GetEvent<TreeNodeSelectionChangedEvent>().Subscribe(OnTreeNodeSelectionChanged, TreeNodeSelectionChangedEvent.THREAD_OPTION);
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
                () => _headingsNavigator.ExpandAll(),
                () => _headingsNavigator != null,
                null, null);
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
            CommandCollapseAll = new RichDelegateCommand(
                Tobi_Plugin_NavigationPane_Lang.CmdTreeCollapseAll_ShortDesc,
                Tobi_Plugin_NavigationPane_Lang.CmdTreeCollapseAll_LongDesc,
                null,
                m_ShellView.LoadTangoIcon("list-remove"),
                () => _headingsNavigator.CollapseAll(),
                () => _headingsNavigator != null,
                null, null);

            CommandFindFocus = new RichDelegateCommand(
                @"DUMMY TXT",
                @"DUMMY TXT",
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () => { if (View != null) FocusHelper.Focus(View.SearchBox); },
                () => View != null && View.SearchBox.Visibility == Visibility.Visible,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_TOCFindNext)
                );

            CommandFindNext = new RichDelegateCommand(
                @"DUMMY TXT", //UserInterfaceStrings.TreeFindNext,
                @"DUMMY TXT", //UserInterfaceStrings.TreeFindNext_,
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () => _headingsNavigator.FindNext(),
                () => _headingsNavigator != null,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_TOCFindNext)
                );

            CommandFindPrev = new RichDelegateCommand(
                @"DUMMY TXT", //UserInterfaceStrings.TreeFindPrev,
                @"DUMMY TXT", //UserInterfaceStrings.TreeFindPrev_,
                null, // KeyGesture set only for the top-level CompositeCommand
                null,
                () => _headingsNavigator.FindPrevious(),
                () => _headingsNavigator != null,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_TOCFindPrev)
                );
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

            m_ShellView.RegisterRichCommand(CommandExpandAll);
            //            shellView.RegisterRichCommand(CommandExpand);
            m_ShellView.RegisterRichCommand(CommandCollapseAll);
            //            shellView.RegisterRichCommand(CommandCollapse);
            
            //shellView.RegisterRichCommand(CommandEditText);

            //m_ShellView.RegisterRichCommand(CommandFindNext);
            //m_ShellView.RegisterRichCommand(CommandFindPrev);
        }

        public static RichDelegateCommand CommandExpandAll { get; private set; }
        //        public RichDelegateCommand CommandExpand { get; private set; }
        public static RichDelegateCommand CommandCollapseAll { get; private set; }
        //        public RichDelegateCommand CommandCollapse { get; private set; }
        //public RichDelegateCommand CommandEditText { get; private set; }

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

        private void trySearchCommands()
        {
            if (m_GlobalSearchCommand == null) { return; }

            m_GlobalSearchCommand.CmdFindFocus.RegisterCommand(CommandFindFocus);
            m_GlobalSearchCommand.CmdFindNext.RegisterCommand(CommandFindNext);
            m_GlobalSearchCommand.CmdFindPrevious.RegisterCommand(CommandFindPrev);
        }

        protected HeadingPanelView View { get; private set; }
        public void SetView(HeadingPanelView view)
        {
            View = view;

            var focusAware = new FocusActiveAwareAdapter(View);
            focusAware.IsActiveChanged += (sender, e) =>
            {
                // ALWAYS ACTIVE ! CommandFindFocus.IsActive = focusAware.IsActive;
                CommandFindNext.IsActive = focusAware.IsActive;
                CommandFindPrev.IsActive = focusAware.IsActive;
            };

            //IActiveAware activeAware = View as IActiveAware;
            //if (activeAware != null) { activeAware.IsActiveChanged += ActiveAwareIsActiveChanged; }
        }
        //private void ActiveAwareIsActiveChanged(object sender, EventArgs e)
        //{
        //    IActiveAware activeAware = (sender as IActiveAware);
        //    if (activeAware == null) { return; }
        //    CommandFindNext.IsActive = activeAware.IsActive;
        //    CommandFindPrev.IsActive = activeAware.IsActive;
        //}

        public HeadingsNavigator HeadingsNavigator
        {
            get { return _headingsNavigator; }
        }
        #region Events
        private void onProjectLoaded(Project project)
        {
            _headingsNavigator = new HeadingsNavigator(project, m_ShellView);
            View.LoadProject();
        }
        private void onProjectUnLoaded(Project project)
        {
            View.UnloadProject();
            _headingsNavigator = null;
        }

        private void OnTreeNodeSelectionChanged(Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>> oldAndNewTreeNodeSelection)
        {
            Tuple<TreeNode, TreeNode> oldTreeNodeSelection = oldAndNewTreeNodeSelection.Item1;
            Tuple<TreeNode, TreeNode> newTreeNodeSelection = oldAndNewTreeNodeSelection.Item2;

            View.SelectTreeNode(newTreeNodeSelection.Item2 ?? newTreeNodeSelection.Item1);
        }
        //private void onSubTreeNodeSelected(TreeNode node)
        //{
        //    onTreeNodeSelected(node);
        //}
        //private void onTreeNodeSelected(TreeNode node)
        //{
        //    View.SelectTreeNode(node);
        //}
        public void OnImportsSatisfied()
        {
            trySearchCommands();
        }
        #endregion
    }
}
