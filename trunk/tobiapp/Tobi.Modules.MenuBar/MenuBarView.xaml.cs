using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Events;
using Microsoft.Practices.Unity;
using Tobi.Infrastructure;

namespace Tobi.Modules.MenuBar
{
    /// <summary>
    /// Interaction logic for MenuBarView.xaml
    /// </summary>
    public partial class MenuBarView
    {
        ///<summary>
        /// Creates a new document.
        /// TODO: move this to the DocumentManagerModule
        ///</summary>
        public RichDelegateCommand<object> NewCommand { get; private set; }

        ///<summary>
        /// Delegates to <see cref="IShellPresenter.ExitCommand"/>
        ///</summary>
        public ICommand ExitCommand { get; private set; }

        ///<summary>
        /// Delegates to <see cref="IShellPresenter.ZoomToggleCommand"/>
        ///</summary>
        //public ICommand ZoomToggleCommand { get; private set; }

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

            initializeCommands();

            InitializeComponent();
        }

        private void initializeCommands()
        {
            Logger.Log("MenuBarView.initializeCommands", Category.Debug, Priority.Medium);

            var shellPresenter = Container.Resolve<IShellPresenter>();

            NewCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Menu_New,
                UserInterfaceStrings.Menu_New_,
                UserInterfaceStrings.Menu_New_KEYS,
                (VisualBrush)FindResource("document-new"),
                NewCommand_Executed, obj => true);
            shellPresenter.RegisterRichCommand(NewCommand);

            ExitCommand = shellPresenter.ExitCommand;
        }

        private void NewCommand_Executed(object parameter)
        {
            Logger.Log("MenuBarView.NewCommand_Executed", Category.Debug, Priority.Medium);

            if (parameter != null) MessageBox.Show(parameter.ToString());
        }

        public void EnsureViewMenuCheckState(string regionName, bool visible)
        {
            //TODO make this generic using a mapping between RegionName and an actual menu trigger check box thing
            //if (ZoomMenuItem.IsChecked != visible) ZoomMenuItem.IsChecked = visible;
        }
    }
}
