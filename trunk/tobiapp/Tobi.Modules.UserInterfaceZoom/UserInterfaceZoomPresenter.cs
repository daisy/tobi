using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Infrastructure;

namespace Tobi.Modules.UserInterfaceZoom
{
    ///<summary>
    /// The Presenter implementation
    ///</summary>
    public class UserInterfaceZoomPresenter : IUserInterfaceZoomPresenter
    {
        #region IUserInterfaceZoomPresenter

        public DelegateCommandWithInputGesture<bool?> ZoomToggleCommand { get; private set; }
        public DelegateCommandWithInputGesture<double?> IncreaseZoomCommand { get; private set; }
        public DelegateCommandWithInputGesture<double?> DecreaseZoomCommand { get; private set; }

        public double MinimumZoom
        {
            get { return 1; }
        }

        public double MaximumZoom
        {
            get { return 5; }
        }

        public double ZoomStep
        {
            get { return 0.2; }
        }

        ///<summary>
        /// The View is injected in the constructor by the DI container.
        ///</summary>
        public IUserInterfaceZoomView View { get; private set; }

        #endregion IUserInterfaceZoomPresenter

        ///<summary>
        /// The Shell presenter, injected in the constructor by the DI container (needed to obtain the UI zoom binding).
        ///</summary>
        protected IShellPresenter ShellPresenter { get; private set; }

        ///<summary>
        /// The DI container, injected in the constructor by the DI container himself !
        ///</summary>
        protected IUnityContainer Container { get; private set; }

        ///<summary>
        /// The application Logger, injected in the constructor by the DI container.
        ///</summary>
        protected ILoggerFacade Logger { get; private set; }

        ///<summary>
        /// The RegionManager, injected in the constructor by the DI container.
        ///</summary>
        protected IRegionManager RegionManager { get; private set; }

        ///<summary>
        /// A keyboard shortcut. TODO: this should become a class property, with a value fetched from a configuration service. The value hard-coded here would be the default one and could be changed dynamically according to user preferences.
        ///</summary>
        private readonly KeyGesture _zoomToggleKeyGesture = new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift);

        ///<summary>
        /// A keyboard shortcut. TODO: this should become a class property, with a value fetched from a configuration service. The value hard-coded here would be the default one and could be changed dynamically according to user preferences.
        ///</summary>
        private readonly KeyGesture _increaseZoomKeyGesture = new KeyGesture(Key.PageUp, ModifierKeys.Control | ModifierKeys.Shift);

        ///<summary>
        /// A keyboard shortcut. TODO: this should become a class property, with a value fetched from a configuration service. The value hard-coded here would be the default one and could be changed dynamically according to user preferences.
        ///</summary>
        private readonly KeyGesture _decreaseZoomKeyGesture = new KeyGesture(Key.PageDown, ModifierKeys.Control | ModifierKeys.Shift);

        ///<summary>
        /// Assigns itself as the Model (DataContext) of the View. Creates the commands and registers the keyboard shortcuts at the application level.
        ///</summary>
        ///<param name="view"></param>
        ///<param name="shellPresenter"></param>
        ///<param name="container"></param>
        ///<param name="logger"></param>
        ///<param name="regionManager"></param>
        public UserInterfaceZoomPresenter(IUserInterfaceZoomView view, IShellPresenter shellPresenter, IUnityContainer container, ILoggerFacade logger, IRegionManager regionManager)
        {
            View = view;
            ShellPresenter = shellPresenter;

            Logger = logger;
            Container = container;
            RegionManager = regionManager;

            View.Model = this;

            ZoomToggleCommand = new DelegateCommandWithInputGesture<bool?>(_zoomToggleKeyGesture, zoomToggleCommand_Executed);
            var keyBinding = new KeyBinding(ZoomToggleCommand, ZoomToggleCommand.KeyGesture) { CommandParameter = null };
            ShellPresenter.AddInputBinding(keyBinding);

            IncreaseZoomCommand = new DelegateCommandWithInputGesture<double?>(_increaseZoomKeyGesture, increaseZoomCommand_Executed);
            ShellPresenter.AddInputBinding(new KeyBinding(IncreaseZoomCommand, IncreaseZoomCommand.KeyGesture) { CommandParameter = null });

            DecreaseZoomCommand = new DelegateCommandWithInputGesture<double?>(_decreaseZoomKeyGesture, decreaseZoomCommand_Executed);
            ShellPresenter.AddInputBinding(new KeyBinding(DecreaseZoomCommand, DecreaseZoomCommand.KeyGesture) { CommandParameter = null });
        }

        private void decreaseZoomCommand_Executed(double? parameter)
        {
            Logger.Log(string.Format("DecreaseZoomCommand_Executed {0}", parameter), Category.Debug, Priority.Medium);

            ShellPresenter.View.DecreaseZoom(parameter);
        }

        private void increaseZoomCommand_Executed(double? parameter)
        {
            Logger.Log(string.Format("IncreaseZoomCommand_Executed {0}", parameter), Category.Debug, Priority.Medium);

            ShellPresenter.View.IncreaseZoom(parameter);
        }

        private void zoomToggleCommand_Executed(bool? parameter)
        {
            Logger.Log(string.Format("ZoomToggleCommand_Executed {0}", parameter), Category.Debug, Priority.Medium);

            ShellPresenter.ToggleView(parameter, View);
        }
    }
}
