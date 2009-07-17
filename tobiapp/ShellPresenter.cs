using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Infrastructure;
using Tobi.Infrastructure.Commanding;
using Tobi.Infrastructure.UI;

namespace Tobi
{
    public class ShellPresenter : IShellPresenter
    {
        private void playAudioCue(string audioClipName)
        {
            string audioClipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                               audioClipName);
            if (File.Exists(audioClipPath))
            {
                new SoundPlayer(audioClipPath).Play();
            }
        }

        public void PlayAudioCueTock()
        {
            playAudioCue("tock.wav");
        }

        public void PlayAudioCueTockTock()
        {
            playAudioCue("tocktock.wav");
        }

        // To avoid the shutting-down loop in OnShellWindowClosing()
        private bool m_Exiting;

        public RichDelegateCommand<object> ExitCommand { get; private set; }

        public RichDelegateCommand<object> MagnifyUiIncreaseCommand { get; private set; }
        public RichDelegateCommand<object> MagnifyUiDecreaseCommand { get; private set; }

        public RichDelegateCommand<object> ManageShortcutsCommand { get; private set; }

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

            App.LOGGER = Logger;

            initCommands();
        }


        private void initCommands()
        {
            var resourceKeys = new[]
            {
"accessories-calculator",
"accessories-character-map",
"accessories-text-editor",
"address-book-new",
"application-certificate",
"application-x-executable",
"applications-accessories",
"applications-development",
"applications-games",
"applications-graphics",
"applications-internet",
"applications-multimedia",
"applications-office",
"applications-other",
"applications-system",
"appointment-new",
"audio-card",
"audio-input-microphone",
"audio-volume-high",
"audio-volume-low",
"audio-volume-medium",
"audio-volume-muted",
"audio-x-generic",
"battery-caution",
"battery",
"bookmark-new",
"camera-photo",
"camera-video",
"computer",
"contact-new",
"dialog-error",
"dialog-information",
"dialog-warning",
"document-new",
"document-open",
"document-print-preview",
"document-print",
"document-properties",
"document-save-as",
"document-save",
"drive-harddisk",
"drive-optical",
"drive-removable-media",
"edit-clear",
"edit-copy",
"edit-cut",
"edit-delete",
"edit-find-replace",
"edit-find",
"edit-paste",
"edit-redo",
"edit-select-all",
"edit-undo",
"emblem-favorite",
"emblem-important",
"emblem-photos",
"emblem-readonly",
"emblem-symbolic-link",
"emblem-system",
"emblem-unreadable",
"face-angel",
"face-crying",
"face-devilish",
"face-glasses",
"face-grin",
"face-kiss",
"face-monkey",
"face-plain",
"face-sad",
"face-smile-big",
"face-smile",
"face-surprise",
"face-wink",
"folder-drag-accept",
"folder-new",
"folder-open",
"folder-remote",
"folder-saved-search",
"folder-visiting",
"folder",
"font-x-generic",
"format-indent-less",
"format-indent-more",
"format-justify-center",
"format-justify-fill",
"format-justify-left",
"format-justify-right",
"format-text-bold",
"format-text-italic",
"format-text-strikethrough",
"format-text-underline",
"go-bottom",
"go-down",
"go-first",
"go-home",
"go-jump",
"go-last",
"go-next",
"go-previous",
"go-top",
"go-up",
"help-browser",
"image-loading",
"image-missing",
"image-x-generic",
"input-gaming",
"input-keyboard",
"input-mouse",
"internet-group-chat",
"internet-mail",
"internet-news-reader",
"internet-web-browser",
"list-add",
"list-remove",
"mail-attachment",
"mail-forward",
"mail-mark-junk",
"mail-message-new",
"mail-reply-all",
"mail-reply-sender",
"mail-send-receive",
"media-eject",
"media-flash",
"media-floppy",
"media-optical",
"media-playback-pause",
"media-playback-start",
"media-playback-stop",
"media-record",
"media-seek-backward",
"media-seek-forward",
"media-skip-backward",
"media-skip-forward",
"multimedia-player",
"network-error",
"network-idle",
"network-offline",
"network-receive",
"network-server",
"network-transmit-receive",
"network-transmit",
"network-wired",
"network-wireless-encrypted",
"network-wireless",
"network-workgroup",
"office-calendar",
"package-x-generic",
"preferences-desktop-accessibility",
"preferences-desktop-assistive-technology",
"preferences-desktop-font",
"preferences-desktop-keyboard-shortcuts",
"preferences-desktop-locale",
"preferences-desktop-multimedia",
"preferences-desktop-peripherals",
"preferences-desktop-remote-desktop",
"preferences-desktop-screensaver",
"preferences-desktop-theme",
"preferences-desktop-wallpaper",
"preferences-desktop",
"preferences-system-network-proxy",
"preferences-system-session",
"preferences-system-windows",
"preferences-system",
"printer-error",
"printer",
"process-stop",
"software-update-available",
"software-update-urgent",
"start-here",
"system-file-manager",
"system-installer",
"system-lock-screen",
"system-log-out",
"system-search",
"system-shutdown",
"system-software-update",
"system-users",
"tab-new",
"text-html",
"text-x-generic-template",
"text-x-generic",
"text-x-script",
"user-desktop",
"user-home",
"user-trash-full",
"user-trash",
"utilities-system-monitor",
"utilities-terminal",
"video-display",
"video-x-generic",
"view-fullscreen",
"view-refresh",
"weather-clear-night",
"weather-clear",
"weather-few-clouds-night",
"weather-few-clouds",
"weather-overcast",
"weather-severe-alert",
"weather-showers-scattered",
"weather-showers",
"weather-snow",
"weather-storm",
"window-new",
"x-office-address-book",
"x-office-calendar",
"x-office-document-template",
"x-office-document",
"x-office-drawing-template",
"x-office-drawing",
"x-office-presentation-template",
"x-office-presentation",
"x-office-spreadsheet-template",
"x-office-spreadsheet"
            };

            foreach (string resourceKey in resourceKeys)
            {
                var command = new RichDelegateCommand<object>(resourceKey,
                                                              resourceKey,
                                                              null,
                                                              LoadTangoIcon(resourceKey),
                                                              null, obj => true);
                m_listOfIconRichCommands.Add(command);
            }

            Logger.Log("ShellPresenter.initCommands", Category.Debug, Priority.Medium);

            //
            ExitCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Menu_Exit,
                                                                      UserInterfaceStrings.Menu_Exit_,
                                                                      UserInterfaceStrings.Menu_Exit_KEYS,
                                                                      LoadTangoIcon("system-log-out"),
                                                                      //RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Exit")),
                //LoadTangoIcon("document-save"),
                                                            ExitCommand_Executed, obj => true);
            RegisterRichCommand(ExitCommand);
            //

            MagnifyUiIncreaseCommand = new RichDelegateCommand<object>(UserInterfaceStrings.UI_IncreaseMagnification,
                                                                       UserInterfaceStrings.UI_IncreaseMagnification_,
                                                                      UserInterfaceStrings.UI_IncreaseMagnification_KEYS,
                                                                      //LoadTangoIcon("mail-forward"),
                                                                      RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Zoom_In")),
                                                            obj => View.MagnificationLevel += 0.15, obj => true);
            RegisterRichCommand(MagnifyUiIncreaseCommand);
            //

            MagnifyUiDecreaseCommand = new RichDelegateCommand<object>(UserInterfaceStrings.UI_DecreaseMagnification,
                                                                      UserInterfaceStrings.UI_DecreaseMagnification_,
                                                                      UserInterfaceStrings.UI_DecreaseMagnification_KEYS,
                                                                      RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Zoom_out")),
                                                            obj => View.MagnificationLevel -= 0.15, obj => true);
            RegisterRichCommand(MagnifyUiDecreaseCommand);
            //

            ManageShortcutsCommand = new RichDelegateCommand<object>(UserInterfaceStrings.ManageShortcuts,
                                                                      UserInterfaceStrings.ManageShortcuts_,
                                                                      UserInterfaceStrings.ManageShortcuts_KEYS,
                                                                      LoadTangoIcon("preferences-desktop-keyboard-shortcuts"),
                                                            obj => manageShortcuts(), obj => true);
            RegisterRichCommand(ManageShortcutsCommand);
            //
            CutCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Cut,
                UserInterfaceStrings.Cut_,
                UserInterfaceStrings.Cut_KEYS,
                LoadTangoIcon("edit-cut"),
                obj => { throw new NotImplementedException("Functionality not implemented, sorry :("); }, obj => true);

            RegisterRichCommand(CutCommand);
            //
            CopyCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Copy,
                UserInterfaceStrings.Copy_,
                UserInterfaceStrings.Copy_KEYS,
                LoadTangoIcon("edit-copy"),
                obj => { throw new NotImplementedException("Functionality not implemented, sorry :("); }, obj => true);

            RegisterRichCommand(CopyCommand);
            //
            PasteCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Paste,
                UserInterfaceStrings.Paste_,
                UserInterfaceStrings.Paste_KEYS,
                LoadTangoIcon("edit-paste"),
                obj => { throw new NotImplementedException("Functionality not implemented, sorry :("); }, obj => true);

            RegisterRichCommand(PasteCommand);
            //
            HelpCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Help,
                UserInterfaceStrings.Help_,
                UserInterfaceStrings.Help_KEYS,
                LoadTangoIcon("help-browser"),
                obj =>
                    {
                        throw new NotImplementedException("Functionality not implemented, sorry :(",
                            new ArgumentOutOfRangeException("First Inner exception",
                                new FileNotFoundException("Third inner exception !")));
                    }, obj => true);

            RegisterRichCommand(HelpCommand);
            //
            PreferencesCommand = new RichDelegateCommand<object>(UserInterfaceStrings.Preferences,
                UserInterfaceStrings.Preferences_,
                UserInterfaceStrings.Preferences_KEYS,
                LoadTangoIcon("preferences-system"),
                obj => { throw new NotImplementedException("Functionality not implemented, sorry :("); }, obj => true);

            RegisterRichCommand(PreferencesCommand);
            //
            WebHomeCommand = new RichDelegateCommand<object>(UserInterfaceStrings.WebHome,
                UserInterfaceStrings.WebHome_,
                UserInterfaceStrings.WebHome_KEYS,
                //RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Home_icon")),
                LoadTangoIcon("go-home"),
                obj => { throw new NotImplementedException("Functionality not implemented, sorry :("); }, obj => true);

            RegisterRichCommand(WebHomeCommand);
            //
            NavNextCommand = new RichDelegateCommand<object>(UserInterfaceStrings.NavNext,
                UserInterfaceStrings.NavNext_,
                UserInterfaceStrings.NavNext_KEYS,
                RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Forward")),
                obj => { throw new NotImplementedException("Functionality not implemented, sorry :("); }, obj => true);

            RegisterRichCommand(NavNextCommand);
            //
            NavPreviousCommand = new RichDelegateCommand<object>(UserInterfaceStrings.NavPrevious,
                UserInterfaceStrings.NavPrevious_,
                UserInterfaceStrings.NavPrevious_KEYS,
                RichDelegateCommand<object>.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Back")),
                obj => { throw new NotImplementedException("Functionality not implemented, sorry :("); }, obj => true);

            RegisterRichCommand(NavPreviousCommand);
            //
        }

        private void manageShortcuts()
        {
            Logger.Log("ShellPresenter.manageShortcuts", Category.Debug, Priority.Medium);

            var windowPopup = new PopupModalWindow(this,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       UserInterfaceStrings.ManageShortcuts),
                                                   new KeyboardShortcuts(this),
                                                   PopupModalWindow.DialogButtonsSet.Ok,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 500, 600);
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

        private bool askUserConfirmExit()
        {
            Logger.Log("ShellPresenter.askUserConfirmExit", Category.Debug, Priority.Medium);

            /*
            try
            {
                throw new ArgumentException("Opps !", new ArgumentOutOfRangeException("Oops 2 !!"));
            }
            catch (Exception ex)
            {
                App.handleException(ex);
            }*/

            var label = new TextBlock
                            {
                                Text = UserInterfaceStrings.ExitConfirm,
                                Margin = new Thickness(8, 0, 8, 0),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center,
                                Focusable = false,
                            };

            var fakeCommand = new RichDelegateCommand<object>(null,
                null,
                null,
                LoadTangoIcon("help-browser"),
                null, obj => true);

            //var zoom = (Double)Resources["MagnificationLevel"]; //Application.Current.
            fakeCommand.IconDrawScale = View.MagnificationLevel;

            var panel = new StackPanel
                            {
                                Orientation = Orientation.Horizontal,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Stretch,
                            };
            panel.Children.Add(fakeCommand.IconLarge);
            panel.Children.Add(label);
            //panel.Margin = new Thickness(8, 8, 8, 0);

            var details = new TextBox
            {
                Text = "Any unsaved changes in your document will be lost !",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                TextWrapping = TextWrapping.Wrap,
                IsReadOnly = true,
                Background = SystemColors.ControlLightLightBrush,
                BorderBrush = SystemColors.ControlDarkDarkBrush,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                SnapsToDevicePixels = true
            };

            var windowPopup = new PopupModalWindow(this,
                                                   UserInterfaceStrings.EscapeMnemonic(
                                                       UserInterfaceStrings.Exit),
                                                   panel,
                                                   PopupModalWindow.DialogButtonsSet.YesNo,
                                                   PopupModalWindow.DialogButton.No,
                                                   true, 300, 160, details, 40);


            windowPopup.Show();

            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Yes)
            {
                return true;
            }

            return false;
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

        private readonly List<RichDelegateCommand<object>> m_listOfIconRichCommands =
            new List<RichDelegateCommand<object>>();
        public List<RichDelegateCommand<object>> IconRichCommands
        {
            get
            {
                return m_listOfIconRichCommands;
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
            foreach (var command in m_listOfIconRichCommands)
            {
                command.IconDrawScale = value;
            }

            Application.Current.Resources["MagnificationLevel"] = value;
        }

        public VisualBrush LoadTangoIcon(string resourceKey)
        {
            Object obj = Application.Current.TryFindResource(resourceKey);
            if (obj == null)
            {
                Object comp = Application.LoadComponent(
                    new Uri("Tobi.Infrastructure;component/tango-icons/" + resourceKey + ".xaml",
                            UriKind.Relative));
                Application.Current.Resources.MergedDictionaries.Add(
                    comp as ResourceDictionary);
            }
            return (VisualBrush)
                Application.Current.FindResource(resourceKey);
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
            var window = View as Window;
            if (window != null && inputBinding != null)
            {
                //Logger.Log("ShellPresenter.AddInputBinding", Category.Debug, Priority.Medium);

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

/*
 
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

 */