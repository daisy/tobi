using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Unity;

namespace Tobi.Modules.MenuBar
{
    /// <summary>
    /// Interaction logic for MenuBarView.xaml
    /// </summary>
    public partial class MenuBarView
    {
        protected IUnityContainer Container { get; private set; }
        protected ILoggerFacade Logger { get; private set; }
        protected IEventAggregator EventAggregator { get; private set; }

        ///<summary>
        /// Dependency-injected constructor
        ///</summary>
        public MenuBarView(IUnityContainer container, ILoggerFacade logger, IEventAggregator eventAggregator)
        {
            Container = container;
            Logger = logger;
            EventAggregator = eventAggregator;

            Logger.Log("MenuBarView.ctor", Category.Debug, Priority.Medium);

            var shellPresenter = Container.Resolve<IShellPresenter>();
            DataContext = shellPresenter;

            InitializeComponent();
        }

        public void EnsureViewMenuCheckState(string regionName, bool visible)
        {
            //TODO make this generic using a mapping between RegionName and an actual menu trigger check box thing
            //if (ZoomMenuItem.IsChecked != visible) ZoomMenuItem.IsChecked = visible;
        }
    }
}
