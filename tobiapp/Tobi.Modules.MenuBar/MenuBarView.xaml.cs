using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Composite.Logging;
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
        public DelegateCommandWithInputGesture<object> NewCommand { get; private set; }

        ///<summary>
        /// Delegates to <see cref="IShellPresenter.ExitCommand"/>
        ///</summary>
        public ICommand ExitCommand { get; private set; }

        ///<summary>
        /// Delegates to <see cref="IShellPresenter.ZoomToggleCommand"/>
        ///</summary>
        public ICommand ZoomToggleCommand { get; private set; }

        protected IUnityContainer Container { get; private set; }
        protected ILoggerFacade Logger { get; private set; }

        ///<summary>
        /// Dependency-injected constructor
        ///</summary>
        public MenuBarView(IUnityContainer container, ILoggerFacade logger)
        {
            InitializeComponent();

            Container = container;
            Logger = logger;

            NewCommand = new DelegateCommandWithInputGesture<object>(new KeyGesture(Key.N, ModifierKeys.Control), NewCommand_Executed, NewCommand_CanExecute);
        }

        private void OnMenuBarLoaded(object sender, RoutedEventArgs e)
        {
            var shellPresenter = Container.Resolve<IShellPresenter>();
            shellPresenter.AddInputBinding(new KeyBinding(NewCommand, NewCommand.KeyGesture));
            ExitCommand = shellPresenter.ExitCommand;
            // TODO: implement a generic registration mechanism for IToggableViews
            //ZoomToggleCommand = shellPresenter.ZoomToggleCommand;
        }
        private void NewCommand_Executed(object parameter)
        {
            Logger.Log("MenuBarPresentationModel.NewCommand_Executed", Category.Debug, Priority.Medium);
            if (parameter != null) MessageBox.Show(parameter.ToString());
        }

        private bool NewCommand_CanExecute(object parameter)
        {
            /*
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (DispatcherOperationCallback)delegate(object o)
            {
                MessageBox.Show("CanNew");
                return null;
            }, null);
             */
            Logger.Log("MenuBarPresentationModel.NewCommand_CanExecute", Category.Debug, Priority.Medium);
            return true;
        }
        public void EnsureViewMenuCheckState(string regionName, bool visible)
        {
            //TODO make this generic using a mapping between RegionName and an actual menu trigger check box thing
            if (ZoomMenuItem.IsChecked != visible)
                ZoomMenuItem.IsChecked = visible;
        }

    }

}
