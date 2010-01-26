using System.ComponentModel.Composition;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using urakawa;
using urakawa.core;

namespace Tobi.Plugin.NavigationPane
{
    [Export(typeof(HeadingPaneViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public class HeadingPaneViewModel : ViewModelBase
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

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(onProjectLoaded, ThreadOption.UIThread);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(onProjectUnLoaded, ThreadOption.UIThread);
            m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(onTreeNodeSelected, ThreadOption.UIThread);
            m_EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Subscribe(onSubTreeNodeSelected, ThreadOption.UIThread);
        }

        private void intializeCommands()
        {
            m_Logger.Log("HeadingPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);

            //
            CommandExpandAll = new RichDelegateCommand(
                UserInterfaceStrings.TreeExpandAll,
                UserInterfaceStrings.TreeExpandAll_,
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
                UserInterfaceStrings.TreeCollapseAll,
                UserInterfaceStrings.TreeCollapseAll_,
                null,
                m_ShellView.LoadTangoIcon("list-remove"),
                () => _headingsNavigator.CollapseAll(),
                () => _headingsNavigator != null,
                null, null);

            CommandFindNext = new RichDelegateCommand(
                UserInterfaceStrings.TreeFindNext,
                UserInterfaceStrings.TreeFindNext_,
                null, // KeyGesture obtained from settings (see last parameters below)
                null,
                () => _headingsNavigator.FindNext(),
                () => _headingsNavigator != null,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_TOCFindNext));

            CommandFindPrev = new RichDelegateCommand(
                UserInterfaceStrings.TreeFindPrev,
                UserInterfaceStrings.TreeFindPrev_,
                null, // KeyGesture obtained from settings (see last parameters below)
                null,
                () => _headingsNavigator.FindPrevious(),
                () => _headingsNavigator != null,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Nav_TOCFindPrev));
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
            m_ShellView.RegisterRichCommand(CommandFindNext);
            m_ShellView.RegisterRichCommand(CommandFindPrev);
        }

        public static RichDelegateCommand CommandExpandAll { get; private set; }
        //        public RichDelegateCommand CommandExpand { get; private set; }
        public static RichDelegateCommand CommandCollapseAll { get; private set; }
        //        public RichDelegateCommand CommandCollapse { get; private set; }
        //public RichDelegateCommand CommandEditText { get; private set; }

        public RichDelegateCommand CommandFindNext { get; private set; }
        public RichDelegateCommand CommandFindPrev { get; private set; }

        ~HeadingPaneViewModel()
        {
#if DEBUG
            m_Logger.Log("HeadingPaneViewModel garbage collected.", Category.Debug, Priority.Medium);
#endif
        }


        protected IHeadingPaneView View { get; private set; }
        public void SetView(IHeadingPaneView view)
        {
            View = view;
        }

        public HeadingsNavigator HeadingsNavigator
        {
            get { return _headingsNavigator; }
        }
        #region Events
        private void onProjectLoaded(Project project)
        {
//            _headingsNavigator = new HeadingsNavigator(project);
            _headingsNavigator = new HeadingsNavigator(project, m_ShellView);
            View.LoadProject();
        }
        private void onProjectUnLoaded(Project project)
        {
            View.UnloadProject();
            _headingsNavigator = null;
        }
        private void onSubTreeNodeSelected(TreeNode node)
        {
            onTreeNodeSelected(node);
        }
        private void onTreeNodeSelected(TreeNode node)
        {
            View.SelectTreeNode(node);
        }
        #endregion
    }
}
