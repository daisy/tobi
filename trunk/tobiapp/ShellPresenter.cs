using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Composite.Wpf.Commands;
using Microsoft.Practices.Unity;
using Tobi.Infrastructure;
using Tobi.Modules.MenuBar;
using Tobi.Modules.StatusBar;

namespace Tobi
{
    public class ShellPresenter : IShellPresenter
    {
        // To avoid the shutting-down loop in OnShellWindowClosing()
        private bool _exiting;

        public DelegateCommandWithInputGesture<object> ExitCommand { get; private set; }

        public IShellView View { get; private set; }
        protected IUnityContainer Container { get; private set; }
        protected ILoggerFacade Logger { get; private set; }
        protected IRegionManager RegionManager { get; private set; }

        ///<summary>
        /// Default constructor
        ///</summary>
        ///<param name="view"></param>
        public ShellPresenter(IShellView view, IUnityContainer container, ILoggerFacade logger, IRegionManager regionManager)
        {
            _exiting = false;
            View = view;
            Logger = logger;
            Container = container;
            RegionManager = regionManager;

            ExitCommand = new DelegateCommandWithInputGesture<object>(new KeyGesture(Key.Q, ModifierKeys.Control), ExitCommand_Executed, ExitCommand_CanExecute);
            AddInputBinding(new KeyBinding(ExitCommand, ExitCommand.KeyGesture));
        }

        private void exit()
        {
            MessageBox.Show("Good bye !", "Tobi says:");
            _exiting = true;
            Application.Current.Shutdown();
        }

        public bool OnShellWindowClosing()
        {
            if (_exiting) return true;
            if (ExitCommand.CanExecute(null))
            {
                if (askUserConfirmExit())
                {
                    exit();
                    return true;
                }
            }
            return false;
        }

        public void ToggleView(bool? show, IToggableView view)
        {

            var region = RegionManager.Regions[view.RegionName];
            var isVisible = region.ActiveViews.Contains(view);

            var makeVisible = true;
            switch (show)
            {
                case null:
                    {
                        makeVisible = !isVisible;
                    }
                    break;
                default:
                    {
                        makeVisible = (bool)show;
                    }
                    break;
            }
            if (makeVisible)
            {
                if (!isVisible)
                {
                    region.Add(view);
                    region.Activate(view);
                }
                view.FocusControl();
            }
            else if (isVisible)
            {
                region.Deactivate(view);
                region.Remove(view);
            }

            var presenter = Container.Resolve<IMenuBarPresenter>();
            presenter.View.EnsureViewMenuCheckState(view.RegionName, makeVisible);
        }

        private bool askUserConfirmExit()
        {
            var window = View as Window;
            if (window != null)
            {
                MessageBoxResult result = MessageBox.Show(window, "Confirm quit ?", "Tobi asks:",
                                                          MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private void ExitCommand_Executed(object parameter)
        {
            Logger.Log("MenuBarPresentationModel.ExitCommand_Executed", Category.Debug, Priority.Medium);

            if (askUserConfirmExit())
            {
                exit();
            }
        }

        private bool ExitCommand_CanExecute(object parameter)
        {
            Logger.Log("MenuBarPresentationModel.ExitCommand_CanExecute", Category.Debug, Priority.Medium);
            return true;
        }

        public void AddInputBinding(InputBinding inputBinding)
        {
            var window = View as Window;
            if (window != null)
            {
                window.InputBindings.Add(inputBinding);
            }
        }
    }
}
