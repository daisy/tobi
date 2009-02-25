using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Presentation.Commands;
using Microsoft.Practices.Unity;
using Tobi.Infrastructure;

namespace Tobi.Modules.MenuBar
{
    ///<summary>
    /// Placeholder for future menu bar view data model,
    /// such as logic related to extension points for addins.
    ///</summary>
    public class MenuBarPresentationModel
    {
        private readonly ILoggerFacade _logger;
        private readonly IUnityContainer _container;

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
        
        public MenuBarPresentationModel(IUnityContainer container)
        {
            _container = container;
            _logger = _container.Resolve<ILoggerFacade>();
            
            var shellPresenter = _container.Resolve<IShellPresenter>();

            NewCommand = new DelegateCommandWithInputGesture<object>(new KeyGesture(Key.N, ModifierKeys.Control), NewCommand_Executed, NewCommand_CanExecute);
            shellPresenter.AddInputBinding(new KeyBinding(NewCommand, NewCommand.KeyGesture));
            
            ExitCommand = shellPresenter.ExitCommand;

            // TODO: implement a generic registration mechanism for IToggableViews
            //ZoomToggleCommand = shellPresenter.ZoomToggleCommand;
        }

        private void NewCommand_Executed(object parameter)
        {
            _logger.Log("MenuBarPresentationModel.NewCommand_Executed", Category.Debug, Priority.Medium);
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
            _logger.Log("MenuBarPresentationModel.NewCommand_CanExecute", Category.Debug, Priority.Medium);
            return true;
        }
    }
}
