using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Sid.Windows.Controls;
using Tobi.Infrastructure;
using Tobi.Modules.MenuBar;
using Tobi.Modules.NavigationPane;
using urakawa;

namespace Tobi
{
    public class ShellPresenter : IShellPresenter
    {
        // To avoid the shutting-down loop in OnShellWindowClosing()
        private bool _exiting;

        public DelegateCommandWithInputGesture<object> ExitCommand { get; private set; }

        public IShellView View { get; private set; }
        protected ILoggerFacade Logger { get; private set; }
        protected IRegionManager RegionManager { get; private set; }

        //protected MenuBarView MenuBarView { get; private set; }
        //protected NavigationPaneView NavigationPaneView { get; private set; }

        protected IUnityContainer Container { get; private set; }

        ///<summary>
        /// Default constructor
        ///</summary>
        ///<param name="view"></param>
        public ShellPresenter(IShellView view, ILoggerFacade logger, IRegionManager regionManager, IUnityContainer container) //MenuBarView menubarView, NavigationPaneView navView
        {
            _exiting = false;

            //MenuBarView = menubarView;
            //NavigationPaneView = navView;

            View = view;
            Logger = logger;
            Container = container;
            RegionManager = regionManager;

            ExitCommand = new DelegateCommandWithInputGesture<object>(UserInterfaceStrings.Menu_Exit,
                                                                      UserInterfaceStrings.Menu_Exit_,
                                                                      new KeyGesture(Key.Q, ModifierKeys.Control),
                                                                      (VisualBrush)Application.Current.FindResource("document-save"),
                                                            ExitCommand_Executed, ExitCommand_CanExecute);
            AddInputBinding(new KeyBinding(ExitCommand, ExitCommand.KeyGesture));
        }

        private void exit()
        {
            //MessageBox.Show("Good bye !", "Tobi says:");
            /*TaskDialog.Show("Tobi is exiting.",
                "Just saying goodbye...",
                "The Tobi application is now closing.",
                TaskDialogIcon.Information);*/
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

            var menuView = Container.Resolve<MenuBarView>();
            menuView.EnsureViewMenuCheckState(view.RegionName, makeVisible);
        }

        public void ProjectLoaded(Project proj)
        {
            NavigationPaneView navPane = Container.Resolve<NavigationPaneView>();
            navPane.ResetNavigation(proj);
        }

        public void PageEncountered(TextElement textElement)
        {
            NavigationPaneView navPane = Container.Resolve<NavigationPaneView>();
            navPane.AddPage(textElement);
        }

        private bool askUserConfirmExit()
        {
            var window = View as Window;
            if (window != null)
            {
                /*MessageBoxResult result = MessageBox.Show(window, "Confirm quit ?", "Tobi asks:",
                                                          MessageBoxButton.OKCancel);
                if (result != MessageBoxResult.OK)
                {
                    return false;
                }*/

                TaskDialogResult result = TaskDialog.Show(window, "Exit Tobi ?",
                    "Are you sure you want to exit Tobi ?",
                    "Press OK to exit, CANCEL to return to the application.",
                    "You can use the ESCAPE, ENTER or 'C' shortcut keys to cancel,\nor the 'O' shortcut key to confirm.",
                    "Please note that any unsaved work will be lost.",
                TaskDialogButton.OkCancel,
                TaskDialogResult.Cancel,
                TaskDialogIcon.Question,
                TaskDialogIcon.Warning,
                Brushes.White,
                Brushes.Navy);

                if (result != TaskDialogResult.Ok)
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

        public void RemoveInputBinding(InputBinding inputBinding)
        {
            var window = View as Window;
            if (window != null)
            {
                window.InputBindings.Remove(inputBinding);
            }
        }
    }
}
