using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Microsoft.Win32;
using Sid.Windows.Controls;
using Tobi.Infrastructure;
using Tobi.Infrastructure.Commanding;
using Tobi.Infrastructure.UI;
using Tobi.Modules.MenuBar;
using urakawa;
using XukImport;

namespace Tobi
{
    public class ShellPresenter : IShellPresenter
    {
        public Project DocumentProject
        {
            get;
            set;
        }

        public string DocumentFilePath
        {
            get;
            set;
        }

        // To avoid the shutting-down loop in OnShellWindowClosing()
        private bool m_Exiting;

        public RichDelegateCommand<object> ExitCommand { get; private set; }

        public RichDelegateCommand<object> MagnifyUiIncreaseCommand { get; private set; }
        public RichDelegateCommand<object> MagnifyUiDecreaseCommand { get; private set; }

        public RichDelegateCommand<object> ManageShortcutsCommand { get; private set; }

        public RichDelegateCommand<object> SaveAsCommand { get; private set; }
        public RichDelegateCommand<object> SaveCommand { get; private set; }

        public RichDelegateCommand<object> NewCommand { get; private set; }
        public RichDelegateCommand<object> OpenCommand { get; private set; }

        public RichDelegateCommand<object> UndoCommand { get; private set; }
        public RichDelegateCommand<object> RedoCommand { get; private set; }

        public RichDelegateCommand<object> CopyCommand { get; private set; }
        public RichDelegateCommand<object> CutCommand { get; private set; }
        public RichDelegateCommand<object> PasteCommand { get; private set; }

        public RichDelegateCommand<object> HelpCommand { get; private set; }
        public RichDelegateCommand<object> PreferencesCommand { get; private set; }
        public RichDelegateCommand<object> WebHomeCommand { get; private set; }

        public RichDelegateCommand<object> NavNextCommand { get; private set; }
        public RichDelegateCommand<object> NavPreviousCommand { get; private set; }

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
                                                                      UserInterfaceStrings.Menu_Exit_KEYS,
                                                                      RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Exit")),
                //(VisualBrush)Application.Current.FindResource("document-save"),
                                                            ExitCommand_Executed, obj => true);
            RegisterRichCommand(ExitCommand);
            //

            MagnifyUiIncreaseCommand = new RichDelegateCommand<object>(UserInterfaceStrings.UI_IncreaseMagnification,
                                                                       UserInterfaceStrings.UI_IncreaseMagnification_,
                                                                      UserInterfaceStrings.UI_IncreaseMagnification_KEYS,
                                                                      RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Zoom_In")),
                                                            obj => MagnifyUi(0.15), obj => true);
            RegisterRichCommand(MagnifyUiIncreaseCommand);
            //

            MagnifyUiDecreaseCommand = new RichDelegateCommand<object>(UserInterfaceStrings.UI_DecreaseMagnification,
                                                                      UserInterfaceStrings.UI_DecreaseMagnification_,
                                                                      UserInterfaceStrings.UI_DecreaseMagnification_KEYS,
                                                                      RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Zoom_out")),
                                                            obj => MagnifyUi(-0.15), obj => true);
            RegisterRichCommand(MagnifyUiDecreaseCommand);
            //

            ManageShortcutsCommand = new RichDelegateCommand<object>(UserInterfaceStrings.ManageShortcuts,
                                                                      UserInterfaceStrings.ManageShortcuts_,
                                                                      UserInterfaceStrings.ManageShortcuts_KEYS,
                                                                      (VisualBrush)Application.Current.FindResource("preferences-desktop-keyboard-shortcuts"),
                                                            obj => manageShortcuts(), obj => true);
            RegisterRichCommand(ManageShortcutsCommand);
            //
            SaveAsCommand = new RichDelegateCommand<object>(UserInterfaceStrings.SaveAs,
                UserInterfaceStrings.SaveAs_,
                UserInterfaceStrings.SaveAs_KEYS,
                (VisualBrush)Application.Current.FindResource("document-save"),
                //RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save_As")),
                null, obj => true);

            RegisterRichCommand(SaveAsCommand);
            //
            SaveCommand = new RichDelegateCommand<object>(
                UserInterfaceStrings.Save,
                UserInterfaceStrings.Save_,
                UserInterfaceStrings.Save_KEYS,
                (VisualBrush)Application.Current.FindResource("media-floppy"),
                //RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Save")),
                null, obj => true);

            RegisterRichCommand(SaveCommand);
            //
            NewCommand = new RichDelegateCommand<object>(UserInterfaceStrings.New,
                UserInterfaceStrings.New_,
                UserInterfaceStrings.New_KEYS,
                (VisualBrush)Application.Current.FindResource("document-new"),
                null, obj => true);

            RegisterRichCommand(NewCommand);
            //
            OpenCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Open,
                UserInterfaceStrings.Open_,
                UserInterfaceStrings.Open_KEYS,
                (VisualBrush)Application.Current.FindResource("document-open"),
                obj => OpenFile(), obj => true);

            RegisterRichCommand(OpenCommand);
            //
            UndoCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Undo,
                UserInterfaceStrings.Undo_,
                UserInterfaceStrings.Undo_KEYS,
                (VisualBrush)Application.Current.FindResource("edit-undo"),
                null, obj => true);

            RegisterRichCommand(UndoCommand);
            //
            RedoCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Redo,
                UserInterfaceStrings.Redo_,
                UserInterfaceStrings.Redo_KEYS,
                (VisualBrush)Application.Current.FindResource("edit-redo"),
                null, obj => true);

            RegisterRichCommand(RedoCommand);
            //
            CutCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Cut,
                UserInterfaceStrings.Cut_,
                UserInterfaceStrings.Cut_KEYS,
                (VisualBrush)Application.Current.FindResource("edit-cut"),
                null, obj => true);

            RegisterRichCommand(CutCommand);
            //
            CopyCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Copy,
                UserInterfaceStrings.Copy_,
                UserInterfaceStrings.Copy_KEYS,
                (VisualBrush)Application.Current.FindResource("edit-copy"),
                null, obj => true);

            RegisterRichCommand(CopyCommand);
            //
            PasteCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Paste,
                UserInterfaceStrings.Paste_,
                UserInterfaceStrings.Paste_KEYS,
                (VisualBrush)Application.Current.FindResource("edit-paste"),
                null, obj => true);

            RegisterRichCommand(PasteCommand);
            //
            HelpCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Help,
                UserInterfaceStrings.Help_,
                UserInterfaceStrings.Help_KEYS,
                (VisualBrush)Application.Current.FindResource("help-browser"),
                null, obj => true);

            RegisterRichCommand(HelpCommand);
            //
            PreferencesCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Preferences,
                UserInterfaceStrings.Preferences_,
                UserInterfaceStrings.Preferences_KEYS,
                (VisualBrush)Application.Current.FindResource("preferences-system"),
                null, obj => true);

            RegisterRichCommand(PreferencesCommand);
            //
            WebHomeCommand = new RichDelegateCommand<object>(UserInterfaceStrings.WebHome,
                UserInterfaceStrings.WebHome_,
                UserInterfaceStrings.WebHome_KEYS,
                RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Home_icon")),
                //(VisualBrush)Application.Current.FindResource("go-home"),
                null, obj => true);

            RegisterRichCommand(WebHomeCommand);
            //
            NavNextCommand = new RichDelegateCommand<object>(UserInterfaceStrings.NavNext,
                UserInterfaceStrings.NavNext_,
                UserInterfaceStrings.NavNext_KEYS,
                RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Forward")),
                null, obj => true);

            RegisterRichCommand(NavNextCommand);
            //
            NavPreviousCommand = new RichDelegateCommand<object>(UserInterfaceStrings.NavPrevious,
                UserInterfaceStrings.NavPrevious_,
                UserInterfaceStrings.NavPrevious_KEYS,
                RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Back")),
                null, obj => true);

            RegisterRichCommand(NavPreviousCommand);
            //
        }

        public void OpenFile()
        {
            var dlg = new OpenFileDialog();
            dlg.FileName = "dtbook"; // Default file name
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "DTBook, OPF, EPUB or XUK (.xml, *.opf, *.xuk, *.epub)|*.xml;*.opf;*.xuk;*.epub";
            bool? result = dlg.ShowDialog();
            if (result == false)
            {
                return;
            }
            DocumentFilePath = dlg.FileName;
            if (Path.GetExtension(DocumentFilePath) == ".xuk")
            {
                DocumentProject = new Project();

                Uri uri = new Uri(DocumentFilePath, UriKind.Absolute);
                DocumentProject.OpenXuk(uri);
            }
            else
            {
                var converter = new DaisyToXuk(DocumentFilePath);
                DocumentProject = converter.Project;
            }

            Logger.Log("-- PublishEvent [ProjectLoadedEvent] ShellPresenter.OpenFile", Category.Debug, Priority.Medium);

            EventAggregator.GetEvent<ProjectLoadedEvent>().Publish(DocumentProject);
        }

        private void manageShortcuts()
        {
            Logger.Log("ShellPresenter.manageShortcuts", Category.Debug, Priority.Medium);

            var window = View as Window;

            var windowPopup = new PopupModalWindow(window ?? Application.Current.MainWindow,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       UserInterfaceStrings.ManageShortcuts),
                                                   new KeyboardShortcuts(this),
                                                   PopupModalWindow.DialogButtonsSet.Ok,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true)
                                                   {
                                                       Height = 600,
                                                       Width = 500
                                                   };
            windowPopup.Show();

            /*
            var windowPopup = new Window()
            {
                Owner = (window ?? Application.Current.MainWindow),
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Title = UserInterfaceStrings.EscapeMnemonic(UserInterfaceStrings.ManageShortcuts),
                Height = 600,
                Width = 500,
                Content = new KeyboardShortcuts(this)
            };
            windowPopup.ShowDialog();
             * */

            /*
        if (window != null)
        {
            var dialog = new TaskDialog();
            dialog.MaxWidth = 600;
            dialog.MaxHeight = 500;
            dialog.TopMost = InteropWindowZOrder.TopMost;
            dialog.TaskDialogWindow.Title = UserInterfaceStrings.EscapeMnemonic(UserInterfaceStrings.ManageShortcuts);
            dialog.TaskDialogButton = TaskDialogButton.Custom;
            dialog.Button1Text = "_Ok";
            dialog.DefaultResult = TaskDialogResult.Button1;
            dialog.IsButton1Cancel = true;
            dialog.Content = new KeyboardShortcuts(this);
            dialog.Show();
                
        } * */

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

        private bool askUserConfirmExit()
        {
            Logger.Log("ShellPresenter.askUserConfirmExit", Category.Debug, Priority.Medium);

            var window = View as Window;

            var label = new TextBlock
                            {
                                Text = UserInterfaceStrings.ExitConfirm,
                                Margin = new Thickness(15),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                            };
            var windowPopup = new PopupModalWindow(window ?? Application.Current.MainWindow,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       UserInterfaceStrings.Exit),
                                                   label,
                                                   PopupModalWindow.DialogButtonsSet.YesNo,
                                                   PopupModalWindow.DialogButton.No,
                                                   true)
            {
                Height = 140,
                Width = 300
            };
            windowPopup.Show();

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Yes)
            {
                return true;
            }

            return false;


            if (window != null)
            {
                /*MessageBoxResult result = MessageBox.Show(window, "Confirm quit ?", "Tobi asks:",
                                                          MessageBoxButton.OKCancel);
                if (result != MessageBoxResult.OK)
                {
                    return false;
                }*/

                /*
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
                }*/
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

        public List<RichDelegateCommand<object>> RegisteredRichCommands
        {
            get
            {
                return m_listOfRegisteredRichCommands;
            }
        }

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
