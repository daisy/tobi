using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Unity;
using Tobi.Common;
using Tobi.Common.MVVM;
using urakawa;
using urakawa.core;

namespace Tobi.Plugin.NavigationPane
{
    public partial class HeadingPaneViewModel : ViewModelBase
    {
        private HeadingsNavigator _headingsNavigator;
        #region Construction

        //        protected IUnityContainer Container { get; private set; }
        public IEventAggregator EventAggregator { get; private set; }
        public ILoggerFacade Logger { get; private set; }

        ///<summary>
        /// Dependency-Injected constructor
        ///</summary>
        public HeadingPaneViewModel(IUnityContainer container, IEventAggregator eventAggregator, ILoggerFacade logger)
            : base(container)
        {
            //Container = container;
            EventAggregator = eventAggregator;
            Logger = logger;

            initialize();
        }

        ~HeadingPaneViewModel()
        {
#if DEBUG
            Logger.Log("HeadingPaneViewModel garbage collected.", Category.Debug, Priority.Medium);
#endif
        }


        #endregion Construction

        protected IHeadingPaneView View { get; private set; }
        public void SetView(IHeadingPaneView view)
        {
            View = view;
        }
        private void initialize()
        {
            intializeCommands();
            EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(onProjectLoaded, ThreadOption.UIThread);
            EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(onProjectUnLoaded, ThreadOption.UIThread);
            EventAggregator.GetEvent<TreeNodeSelectedEvent>().Subscribe(onTreeNodeSelected, ThreadOption.UIThread);
            EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Subscribe(onSubTreeNodeSelected, ThreadOption.UIThread);
        }
        public HeadingsNavigator HeadingsNavigator
        {
            get { return _headingsNavigator; }
        }
        #region Events
        private void onProjectLoaded(Project project)
        {
//            _headingsNavigator = new HeadingsNavigator(project);
            _headingsNavigator = new HeadingsNavigator(project, Container.Resolve<IShellView>());
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
