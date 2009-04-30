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
        private KeyBinding NewKeyBinding;

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

        protected IEventAggregator EventAggregator { get; private set; }

        ///<summary>
        /// Dependency-injected constructor
        ///</summary>
        public MenuBarView(IUnityContainer container, ILoggerFacade logger, IEventAggregator eventAggregator)
        {
            Container = container;
            Logger = logger;
            EventAggregator = eventAggregator;

            EventAggregator.GetEvent<UserInterfaceScaledEvent>().Subscribe(OnUserInterfaceScaled, ThreadOption.UIThread);

            NewCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Menu_New, UserInterfaceStrings.Menu_New_,
                new KeyGesture(Key.N, ModifierKeys.Control),
                (VisualBrush)FindResource("document-new"),
                NewCommand_Executed, NewCommand_CanExecute);
            NewKeyBinding = new KeyBinding(NewCommand, NewCommand.KeyGesture);

            var shellPresenter = Container.Resolve<IShellPresenter>();
            shellPresenter.AddInputBinding(NewKeyBinding);
            ExitCommand = shellPresenter.ExitCommand;

            InitializeComponent();
        }

        private void OnUserInterfaceScaled(double obj)
        {
            //xxx
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
            //if (ZoomMenuItem.IsChecked != visible) ZoomMenuItem.IsChecked = visible;
        }

    }

}
