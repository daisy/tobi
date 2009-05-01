using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Composite.Events;
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
        private bool m_Exiting;

        public RichDelegateCommand<object> ExitCommand { get; private set; }

        public RichDelegateCommand<object> MagnifyUiIncreaseCommand { get; private set; }
        public RichDelegateCommand<object> MagnifyUiDecreaseCommand { get; private set; }

        public IShellView View { get; private set; }
        protected ILoggerFacade Logger { get; private set; }
        protected IRegionManager RegionManager { get; private set; }

        protected IUnityContainer Container { get; private set; }
        protected IEventAggregator EventAggregator { get; private set; }

        ///<summary>
        /// Default constructor
        ///</summary>
        ///<param name="view"></param>
        public ShellPresenter(IShellView view, ILoggerFacade logger,
                            IRegionManager regionManager, IUnityContainer container,
                            IEventAggregator eventAggregator
                            )
        {
            m_Exiting = false;

            View = view;
            Logger = logger;
            Container = container;
            RegionManager = regionManager;
            EventAggregator = eventAggregator;

            Logger.Log("ShellPresenter.ctor", Category.Debug, Priority.Medium);

            initCommands();

        }

        private void initCommands()
        {
            Logger.Log("ShellPresenter.initCommands", Category.Debug, Priority.Medium);

            ExitCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Menu_Exit,
                                                                      UserInterfaceStrings.Menu_Exit_,
                                                                      new KeyGesture(Key.Q, ModifierKeys.Control),
                                                                      (VisualBrush)Application.Current.FindResource("document-save"),
                                                            ExitCommand_Executed, obj => true);
            RegisterRichCommand(ExitCommand);
            //

            MagnifyUiIncreaseCommand = new RichDelegateCommand<object>(null,
                                                                       UserInterfaceStrings.UI_IncreaseMagnification,
                                                                      new KeyGesture(Key.F2, ModifierKeys.Control),
                                                                      RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Zoom_In")),
                                                            obj => MagnifyUi(0.15), obj => true);
            RegisterRichCommand(MagnifyUiIncreaseCommand);
            //

            MagnifyUiDecreaseCommand = new RichDelegateCommand<object>(null,
                                                                      UserInterfaceStrings.UI_DecreaseMagnification,
                                                                      new KeyGesture(Key.F2, ModifierKeys.Control | ModifierKeys.Shift),
                                                                      RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Zoom_out")),
                                                            obj => MagnifyUi(-0.15), obj => true);
            RegisterRichCommand(MagnifyUiDecreaseCommand);
        }

        private void MagnifyUi(double value)
        {
            View.MagnificationLevel += value;
        }

        private void exit()
        {
            Logger.Log("ShellPresenter.exit", Category.Debug, Priority.Medium);

            //MessageBox.Show("Good bye !", "Tobi says:");
            /*TaskDialog.Show("Tobi is exiting.",
                "Just saying goodbye...",
                "The Tobi application is now closing.",
                TaskDialogIcon.Information);*/
            m_Exiting = true;
            Application.Current.Shutdown();
        }

        public bool OnShellWindowClosing()
        {
            Logger.Log("ShellPresenter.OnShellWindowClosing", Category.Debug, Priority.Medium);

            if (m_Exiting) return true;

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
            Logger.Log("ShellPresenter.ToggleView", Category.Debug, Priority.Medium);

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
            Logger.Log("ShellPresenter.ProjectLoaded", Category.Debug, Priority.Medium);

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
            Logger.Log("ShellPresenter.askUserConfirmExit", Category.Debug, Priority.Medium);

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
            if (askUserConfirmExit())
            {
                exit();
            }
        }

        private readonly List<RichDelegateCommand<object>> m_listOfRegisteredRichCommands =
            new List<RichDelegateCommand<object>>();

        public void SetZoomValue(double value)
        {
            /*if (EventAggregator == null)
            {
                return;
            }
            EventAggregator.GetEvent<UserInterfaceScaledEvent>().Publish(value);
             */

            foreach (var command in m_listOfRegisteredRichCommands)
            {
                command.IconDrawScale = value;
            }
        }

        public void RegisterRichCommand(RichDelegateCommand<object> command)
        {
            m_listOfRegisteredRichCommands.Add(command);
            AddInputBinding(command.KeyBinding);
        }

        public void UnRegisterRichCommand(RichDelegateCommand<object> command)
        {
            m_listOfRegisteredRichCommands.Remove(command);
            RemoveInputBinding(command.KeyBinding);
        }

        public bool AddInputBinding(InputBinding inputBinding)
        {
            Logger.Log("ShellPresenter.AddInputBinding", Category.Debug, Priority.Medium);

            var window = View as Window;
            if (window != null && inputBinding != null)
            {
                logInputBinding(inputBinding);
                window.InputBindings.Add(inputBinding);
                return true;
            }

            return false;
        }

        public void RemoveInputBinding(InputBinding inputBinding)
        {
            Logger.Log("ShellPresenter.RemoveInputBinding", Category.Debug, Priority.Medium);

            var window = View as Window;
            if (window != null && inputBinding != null)
            {
                logInputBinding(inputBinding);
                window.InputBindings.Remove(inputBinding);
            }
        }

        private void logInputBinding(InputBinding inputBinding)
        {
            if (inputBinding.Gesture is KeyGesture)
            {
                Logger.Log(
                    "KeyBinding (" +
                    ((KeyGesture)(inputBinding.Gesture)).GetDisplayStringForCulture(CultureInfo.CurrentCulture) + ")",
                    Category.Debug, Priority.Medium);
            }
            else
            {
                Logger.Log(
                       "InputBinding (" +
                       inputBinding.Gesture + ")",
                       Category.Debug, Priority.Medium);
            }
        }
    }
}
