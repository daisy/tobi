using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using System.Diagnostics;
using System.IO;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using Application = System.Windows.Application;

namespace Tobi
{
    public partial class Shell
    {
        public RichDelegateCommand ExitCommand { get; private set; }

        public RichDelegateCommand MagnifyUiIncreaseCommand { get; private set; }
        public RichDelegateCommand MagnifyUiDecreaseCommand { get; private set; }
        public RichDelegateCommand MagnifyUiResetCommand { get; private set; }

        public RichDelegateCommand ManageShortcutsCommand { get; private set; }
        public RichDelegateCommand DisplayPreviewIconsDebugCommand { get; private set; }

        public RichDelegateCommand CopyCommand { get; private set; }
        public RichDelegateCommand CutCommand { get; private set; }
        public RichDelegateCommand PasteCommand { get; private set; }

        public RichDelegateCommand HelpCommand { get; private set; }
        public RichDelegateCommand PreferencesCommand { get; private set; }
        //public RichDelegateCommand WebHomeCommand { get; private set; }

        //public RichDelegateCommand NavNextCommand { get; private set; }
        //public RichDelegateCommand NavPreviousCommand { get; private set; }

        public RichDelegateCommand ShowLogFilePathCommand { get; private set; }

        private void initCommands()
        {
            m_Logger.Log(@"ShellView.initCommands", Category.Debug, Priority.Medium);

            //
            ExitCommand = new RichDelegateCommand(
                UserInterfaceStrings.Menu_Exit,
                UserInterfaceStrings.Menu_Exit_,
                UserInterfaceStrings.Menu_Exit_KEYS,
                LoadTangoIcon(@"system-log-out"),
                //ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Exit")),
                //LoadTangoIcon("document-save"),
                () =>
                {
                    m_Logger.Log(@"ShellView.ExitCommand", Category.Debug, Priority.Medium);

                    if (askUserConfirmExit())
                    {
                        exit();
                    }
                },
                () => true);

            RegisterRichCommand(ExitCommand);
            //

            MagnifyUiResetCommand = new RichDelegateCommand(
                UserInterfaceStrings.UI_ResetMagnification,
                UserInterfaceStrings.UI_ResetMagnification_,
                UserInterfaceStrings.UI_ResetMagnification_KEYS,
                LoadTangoIcon(@"weather-clear"),
                () =>
                {
                    m_Logger.Log(@"ShellView.MagnifyUiResetCommand", Category.Debug, Priority.Medium);

                    MagnificationLevel = 1;
                },
                () => true);

            RegisterRichCommand(MagnifyUiResetCommand);
            //
            MagnifyUiIncreaseCommand = new RichDelegateCommand(
                UserInterfaceStrings.UI_IncreaseMagnification,
                UserInterfaceStrings.UI_IncreaseMagnification_,
                UserInterfaceStrings.UI_IncreaseMagnification_KEYS,
                //LoadTangoIcon("mail-forward"),
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource(@"Horizon_Image_Zoom_In")),
                () =>
                {
                    m_Logger.Log(@"ShellView.MagnifyUiIncreaseCommand", Category.Debug, Priority.Medium);

                    MagnificationLevel += 0.15;
                },
                () => true);

            RegisterRichCommand(MagnifyUiIncreaseCommand);
            //

            MagnifyUiDecreaseCommand = new RichDelegateCommand(
                UserInterfaceStrings.UI_DecreaseMagnification,
                UserInterfaceStrings.UI_DecreaseMagnification_,
                UserInterfaceStrings.UI_DecreaseMagnification_KEYS,
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource(@"Horizon_Image_Zoom_out")),
                () =>
                {
                    m_Logger.Log(@"ShellView.MagnifyUiDecreaseCommand", Category.Debug, Priority.Medium);

                    MagnificationLevel -= 0.15;
                },
                () => true);

            RegisterRichCommand(MagnifyUiDecreaseCommand);
            //
#if ICONS
            DisplayPreviewIconsDebugCommand = new RichDelegateCommand(
                UserInterfaceStrings.IconsDebug,
                null,
                UserInterfaceStrings.IconsDebug_KEYS,
                null,
                DisplayPreviewIconsDebugCommand_Executed,
                () => true);

            RegisterRichCommand(DisplayPreviewIconsDebugCommand);
#endif
            //
            ManageShortcutsCommand = new RichDelegateCommand(
                UserInterfaceStrings.ManageShortcuts,
                UserInterfaceStrings.ManageShortcuts_,
                UserInterfaceStrings.ManageShortcuts_KEYS,
                LoadTangoIcon(@"preferences-desktop-keyboard-shortcuts"),
                () =>
                {
                    m_Logger.Log(@"ShellView.ManageShortcutsCommand_Executed", Category.Debug, Priority.Medium);

                    var windowPopup = new PopupModalWindow(this,
                                                           UserInterfaceStrings.EscapeMnemonic(
                                                               UserInterfaceStrings.ManageShortcuts),
                                                           new KeyboardShortcuts(this),
                                                           PopupModalWindow.DialogButtonsSet.Ok,
                                                           PopupModalWindow.DialogButton.Ok,
                                                           true, 500, 600);

                    windowPopup.ShowFloating(null);
                },
                () => true);

            RegisterRichCommand(ManageShortcutsCommand);
            //
            CutCommand = new RichDelegateCommand(
                UserInterfaceStrings.Cut,
                UserInterfaceStrings.Cut_,
                UserInterfaceStrings.Cut_KEYS,
                LoadTangoIcon(@"edit-cut"),
                () => Debug.Fail(@"Functionality not implemented yet."),
                () => true);

            RegisterRichCommand(CutCommand);
            //
            CopyCommand = new RichDelegateCommand(
                UserInterfaceStrings.Copy,
                UserInterfaceStrings.Copy_,
                UserInterfaceStrings.Copy_KEYS,
                LoadTangoIcon(@"edit-copy"),
                () => Debug.Fail(@"Functionality not implemented yet."),
                () => true);

            RegisterRichCommand(CopyCommand);
            //
            PasteCommand = new RichDelegateCommand(
                UserInterfaceStrings.Paste,
                UserInterfaceStrings.Paste_,
                UserInterfaceStrings.Paste_KEYS,
                LoadTangoIcon(@"edit-paste"),
                () => Debug.Fail(@"Functionality not implemented yet."),
                () => true);

            RegisterRichCommand(PasteCommand);
            //
            ShowLogFilePathCommand = new RichDelegateCommand(
                UserInterfaceStrings.ShowLogFilePath,
                UserInterfaceStrings.ShowLogFilePath_,
                UserInterfaceStrings.ShowLogFilePath_KEYS,
                null, //LoadTangoIcon(@"help-browser"),
                () =>
                {
                    m_Logger.Log(@"ShellView.ShowLogFilePathCommand", Category.Debug, Priority.Medium);


                    var label = new TextBlock
                    {
                        Text = UserInterfaceStrings.ShowLogFilePath_,
                        Margin = new Thickness(8, 0, 8, 0),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Focusable = true,
                        TextWrapping = TextWrapping.Wrap
                    };

                    var iconProvider = new ScalableGreyableImageProvider(LoadTangoIcon("edit-find"), MagnificationLevel);
                    //var zoom = (Double)Resources["MagnificationLevel"]; //Application.Current.

                    var panel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Stretch,
                    };
                    panel.Children.Add(iconProvider.IconLarge);
                    panel.Children.Add(label);
                    //panel.Margin = new Thickness(8, 8, 8, 0);


                    var details = new TextBoxReadOnlyCaretVisible(UserInterfaceStrings.LOG_FILE_PATH)
                    {
                    };

                    var windowPopup = new PopupModalWindow(this,
                                                           UserInterfaceStrings.EscapeMnemonic(
                                                               UserInterfaceStrings.ShowLogFilePath),
                                                           panel,
                                                           PopupModalWindow.DialogButtonsSet.Close,
                                                           PopupModalWindow.DialogButton.Close,
                                                           true, 300, 160, details, 40);

                    windowPopup.ShowModal();

                },
                 () => true);

            RegisterRichCommand(ShowLogFilePathCommand);
            //
            HelpCommand = new RichDelegateCommand(
                UserInterfaceStrings.Help,
                UserInterfaceStrings.Help_,
                UserInterfaceStrings.Help_KEYS,
                LoadTangoIcon("help-browser"),
                () =>
                {
                    m_Logger.Log("ShellView.HelpCommand", Category.Debug, Priority.Medium);

                    throw new NotImplementedException("Functionality not implemented, sorry :(",
                        new ArgumentOutOfRangeException("First Inner exception",
                            new FileNotFoundException("Second inner exception !")));
                },
                 () => true);

            RegisterRichCommand(HelpCommand);
            //
            PreferencesCommand = new RichDelegateCommand(
                UserInterfaceStrings.Preferences,
                UserInterfaceStrings.Preferences_,
                UserInterfaceStrings.Preferences_KEYS,
                LoadTangoIcon("preferences-system"),
                () => Debug.Fail("Functionality not implemented yet."),
                () => true);

            RegisterRichCommand(PreferencesCommand);
            //
            //WebHomeCommand = new RichDelegateCommand(UserInterfaceStrings.WebHome,
            //    UserInterfaceStrings.WebHome_,
            //    UserInterfaceStrings.WebHome_KEYS,
            //    //ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Home_icon")),
            //    LoadTangoIcon("go-home"),
            //    ()=> { throw new NotImplementedException("Functionality not implemented, sorry :("); }, ()=> true);

            //RegisterRichCommand(WebHomeCommand);
            ////
            //NavNextCommand = new RichDelegateCommand(UserInterfaceStrings.NavNext,
            //    UserInterfaceStrings.NavNext_,
            //    UserInterfaceStrings.NavNext_KEYS,
            //    ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Forward")),
            //    ()=> { throw new NotImplementedException("Functionality not implemented, sorry :("); }, ()=> true);

            //RegisterRichCommand(NavNextCommand);
            ////
            //NavPreviousCommand = new RichDelegateCommand(UserInterfaceStrings.NavPrevious,
            //    UserInterfaceStrings.NavPrevious_,
            //    UserInterfaceStrings.NavPrevious_KEYS,
            //    ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Back")),
            //    ()=> { throw new NotImplementedException("Functionality not implemented, sorry :("); }, ()=> true);

            //RegisterRichCommand(NavPreviousCommand);

        }

        public VisualBrush LoadIcon(string path, string resourceKey)
        {
            Object obj = Application.Current.TryFindResource(resourceKey);
            if (obj == null)
            {
                Object comp = Application.LoadComponent(
                    new Uri(path + resourceKey + ".xaml", UriKind.Relative));

                Application.Current.Resources.MergedDictionaries.Add(comp as ResourceDictionary);

                var brush = (VisualBrush)Application.Current.FindResource(resourceKey);

                return brush;
            }
            else
            {
                return (VisualBrush)obj;
            }
        }

        public VisualBrush LoadTangoIcon(string resourceKey)
        {
            return LoadIcon("Tobi.Infrastructure;component/tango-icons/", resourceKey);
        }

        public VisualBrush LoadGnomeNeuIcon(string resourceKey)
        {
            return LoadIcon("Tobi.Infrastructure;component/gnome-extra-icons/Neu/", resourceKey);
        }

        public VisualBrush LoadGnomeGionIcon(string resourceKey)
        {
            return LoadIcon("Tobi.Infrastructure;component/gnome-extra-icons/Gion/", resourceKey);
        }

        public VisualBrush LoadGnomeFoxtrotIcon(string resourceKey)
        {
            return LoadIcon("Tobi.Infrastructure;component/gnome-extra-icons/Foxtrot/", resourceKey);
        }

        private readonly List<RichDelegateCommand> m_listOfRegisteredRichCommands =
            new List<RichDelegateCommand>();

        public List<RichDelegateCommand> RegisteredRichCommands
        {
            get
            {
                return m_listOfRegisteredRichCommands;
            }
        }

        public void RegisterRichCommand(RichDelegateCommand command)
        {
            m_listOfRegisteredRichCommands.Add(command);
            AddInputBinding(command.KeyBinding);
        }

        public void UnRegisterRichCommand(RichDelegateCommand command)
        {
            m_listOfRegisteredRichCommands.Remove(command);
            RemoveInputBinding(command.KeyBinding);
        }

        public bool AddInputBinding(InputBinding inputBinding)
        {
            var window = this as Window;
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
            var window = this as Window;
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
                m_Logger.Log(
                    "KeyBinding (" +
                    ((KeyGesture)(inputBinding.Gesture)).GetDisplayStringForCulture(CultureInfo.CurrentCulture) + ")",
                    Category.Debug, Priority.Medium);
            }
            else
            {
                m_Logger.Log(
                       "InputBinding (" +
                       inputBinding.Gesture + ")",
                       Category.Debug, Priority.Medium);
            }
        }

        public void updateIconDrawScales(double value)
        {
            /*if (EventAggregator == null)
            {
                return;
            }
            EventAggregator.GetEvent<UserInterfaceScaledEvent>().Publish(value);
             */

            foreach (var command in m_listOfRegisteredRichCommands)
            {
                command.SetIconProviderDrawScale(value);
            }
#if ICONS
            foreach (var command in m_listOfIconRichCommands)
            {
                command.SetIconProviderDrawScale(value);
            }
            foreach (var command in m_listOfIconRichCommands2)
            {
                command.SetIconProviderDrawScale(value);
            }
            foreach (var command in m_listOfIconRichCommands3)
            {
                command.SetIconProviderDrawScale(value);
            }
            foreach (var command in m_listOfIconRichCommands4)
            {
                command.SetIconProviderDrawScale(value);
            }
#endif
        }

#if ICONS
        private readonly List<RichDelegateCommand> m_listOfIconRichCommands =
            new List<RichDelegateCommand>();
        public List<RichDelegateCommand> IconRichCommands
        {
            get
            {
                return m_listOfIconRichCommands;
            }
        }

        private readonly List<RichDelegateCommand> m_listOfIconRichCommands2 =
            new List<RichDelegateCommand>();
        public List<RichDelegateCommand> IconRichCommands2
        {
            get
            {
                return m_listOfIconRichCommands2;
            }
        }
        private readonly List<RichDelegateCommand> m_listOfIconRichCommands3 =
            new List<RichDelegateCommand>();
        public List<RichDelegateCommand> IconRichCommands3
        {
            get
            {
                return m_listOfIconRichCommands3;
            }
        }
        private readonly List<RichDelegateCommand> m_listOfIconRichCommands4 =
            new List<RichDelegateCommand>();
        public List<RichDelegateCommand> IconRichCommands4
        {
            get
            {
                return m_listOfIconRichCommands4;
            }
        }

        private void DisplayPreviewIconsDebugCommand_Executed()
        {
            m_Logger.Log("ShellView.DisplayPreviewIconsDebugCommand_Executed", Category.Debug, Priority.Medium);

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
"edit-delete_",
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
"mail-mark-not-junk",
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

            var resourceKeys2 = new[]
            {"Neu_accessories-archiver",
"Neu_accessories-character-map",
"Neu_accessories-text-editor",
"Neu_address-book-new",
"Neu_application-certificate",
"Neu_application-x-executable",
"Neu_applications-accessories",
"Neu_applications-development",
"Neu_applications-games",
"Neu_applications-graphics",
"Neu_applications-internet",
"Neu_applications-multimedia",
"Neu_applications-office",
"Neu_applications-other",
"Neu_applications-system",
"Neu_appointment-new",
"Neu_audio-volume-high",
"Neu_audio-volume-low",
"Neu_audio-volume-medium",
"Neu_audio-volume-muted",
"Neu_audio-volume-zero",
"Neu_audio-x-generic",
"Neu_battery-caution",
"Neu_battery",
"Neu_bookmark-new",
"Neu_computer",
"Neu_contact-new",
"Neu_dialog-cancel",
"Neu_dialog-close",
"Neu_dialog-error",
"Neu_dialog-information",
"Neu_dialog-ok",
"Neu_dialog-password",
"Neu_dialog-question",
"Neu_dialog-warning",
"Neu_document-new",
"Neu_document-open",
"Neu_document-print-preview",
"Neu_document-print",
"Neu_document-properties",
"Neu_document-save-as",
"Neu_document-save",
"Neu_drive-cdrom",
"Neu_drive-harddisk",
"Neu_drive-removable-media",
"Neu_edit-clear",
"Neu_edit-copy",
"Neu_edit-cut",
"Neu_edit-delete",
"Neu_edit-find-replace",
"Neu_edit-find",
"Neu_edit-paste",
"Neu_edit-redo",
"Neu_edit-select-all",
"Neu_edit-undo",
"Neu_emblem-important",
"Neu_emblem-pictures",
"Neu_emblem-readonly",
"Neu_emblem-symbolic-link",
"Neu_emblem-system",
"Neu_emblem-unreadable",
"Neu_emblem-web",
"Neu_empty",
"Neu_epiphany-bookmarks",
"Neu_evolution",
"Neu_folder-drag-accept",
"Neu_folder-new",
"Neu_folder-open",
"Neu_folder-remote",
"Neu_folder-saved-search",
"Neu_folder-visiting",
"Neu_folder",
"Neu_font-x-generic",
"Neu_format-indent-less",
"Neu_format-indent-more",
"Neu_format-justify-center",
"Neu_format-justify-fill",
"Neu_format-justify-left",
"Neu_format-justify-right",
"Neu_format-text-bold",
"Neu_format-text-italic",
"Neu_format-text-underline",
"Neu_gaim",
"Neu_gimp",
"Neu_go-bottom",
"Neu_go-down",
"Neu_go-first",
"Neu_go-home",
"Neu_go-jump",
"Neu_go-last",
"Neu_go-next",
"Neu_go-previous",
"Neu_go-top",
"Neu_go-up",
"Neu_graphics-image-editor",
"Neu_graphics-image-viewer",
"Neu_graphics-svg-editor",
"Neu_help-about",
"Neu_help-browser",
"Neu_image-loading",
"Neu_image-missing",
"Neu_image-x-generic",
"Neu_input-keyboard",
"Neu_input-mouse",
"Neu_internet-ftp-client",
"Neu_internet-group-chat",
"Neu_internet-mail",
"Neu_internet-web-browser",
"Neu_list-add",
"Neu_list-remove",
"Neu_mail-forward",
"Neu_mail-message-new",
"Neu_mail-reply-all",
"Neu_mail-reply-sender",
"Neu_mail-send-receive",
"Neu_media-cdrom-audio",
"Neu_media-cdrom",
"Neu_media-cdrw",
"Neu_media-dvd",
"Neu_media-dvdrw",
"Neu_media-floppy",
"Neu_misc-cd-image",
"Neu_multimedia-volume-control",
"Neu_network-error",
"Neu_network-idle",
"Neu_network-offline",
"Neu_network-receive",
"Neu_network-server",
"Neu_network-transmit-receive",
"Neu_network-transmit",
"Neu_network-workgroup",
"Neu_package-x-generic",
"Neu_preferences-desktop-accessibility",
"Neu_preferences-desktop-assistive-technology",
"Neu_preferences-desktop-font",
"Neu_preferences-desktop-peripherals",
"Neu_preferences-desktop-remote-desktop",
"Neu_preferences-desktop-screensaver",
"Neu_preferences-desktop-wallpaper",
"Neu_preferences-desktop",
"Neu_preferences-system-network-proxy",
"Neu_preferences-system-session",
"Neu_preferences-system-windows",
"Neu_preferences-system",
"Neu_preferences-user-information",
"Neu_printer-error",
"Neu_printer",
"Neu_process-stop",
"Neu_sound-juicer",
"Neu_start-here",
"Neu_system-file-manager",
"Neu_system-installer",
"Neu_system-lock-screen",
"Neu_system-log-out",
"Neu_system-search",
"Neu_system-shutdown",
"Neu_system-software-update",
"Neu_system-users",
"Neu_text-html",
"Neu_text-x-generic",
"Neu_text-x-script",
"Neu_text-x-source",
"Neu_user-desktop",
"Neu_user-home",
"Neu_user-trash-full",
"Neu_user-trash",
"Neu_utilities-system-monitor",
"Neu_utilities-terminal",
"Neu_video-display",
"Neu_video-x-generic",
"Neu_view-refresh",
"Neu_window-new",
"Neu_x-office-address-book",
"Neu_x-office-document",
"Neu_x-office-spreadsheet"
            };


            var resourceKeys3 = new[]
            {
                "Gion_accessories-archiver",
"Gion_application-certificate",
"Gion_applications-internet",
"Gion_audio-x-generic",
"Gion_bookmark-new",
"Gion_computer",
"Gion_document-open",
"Gion_drive-harddisk",
"Gion_drive-removable-media",
"Gion_evolution",
"Gion_folder-drag-accept",
"Gion_folder-open",
"Gion_folder-remote",
"Gion_folder-saved-search",
"Gion_folder-visiting",
"Gion_folder",
"Gion_go-down",
"Gion_go-next",
"Gion_go-previous",
"Gion_go-up",
"Gion_image-x-generic",
"Gion_internet-mail",
"Gion_internet-web-browser",
"Gion_media-cdrom-audio",
"Gion_media-cdrom",
"Gion_media-cdrw",
"Gion_media-dvd",
"Gion_media-dvdrw",
"Gion_music-player",
"Gion_package-x-generic",
"Gion_process-stop",
"Gion_text-html",
"Gion_text-x-authors",
"Gion_text-x-changelog",
"Gion_text-x-copying",
"Gion_text-x-generic",
"Gion_text-x-install",
"Gion_text-x-readme",
"Gion_text-x-script",
"Gion_text-x-source",
"Gion_user-desktop",
"Gion_user-home",
"Gion_user-trash-full",
"Gion_user-trash",
"Gion_utilities-terminal",
"Gion_view-refresh",
"Gion_x-office-document",
"Gion_x-office-spreadsheet"
            };


            var resourceKeys4 = new[]
            {
                "Foxtrot_computer",
"Foxtrot_document-open",
"Foxtrot_folder-accept",
"Foxtrot_folder-drag-accept",
"Foxtrot_folder-new",
"Foxtrot_folder-open",
"Foxtrot_folder-remote",
"Foxtrot_folder",
"Foxtrot_gnome-fs-accept",
"Foxtrot_gnome-fs-desktop",
"Foxtrot_gnome-fs-trash-full",
"Foxtrot_go-bottom",
"Foxtrot_go-down",
"Foxtrot_go-first",
"Foxtrot_go-home",
"Foxtrot_go-last",
"Foxtrot_go-next",
"Foxtrot_go-previous",
"Foxtrot_go-top",
"Foxtrot_go-up",
"Foxtrot_list-add",
"Foxtrot_list-remove",
"Foxtrot_network-workgroup",
"Foxtrot_preferences-system",
"Foxtrot_system-search",
"Foxtrot_user-desktop",
"Foxtrot_user-home-folder",
"Foxtrot_user-home",
"Foxtrot_user-trash-full",
"Foxtrot_user-trash",
"Foxtrot_video-display",
"Foxtrot_view-refresh",
"Foxtrot_x-directory-drag-accept",
"Foxtrot_x-directory-normal-accept",
"Foxtrot_x-directory-normal-open",
"Foxtrot_x-directory-normal",
"Foxtrot_x-directory-remote"
            };

            if (m_listOfIconRichCommands.Count == 0)
            {
                foreach (string resourceKey in resourceKeys)
                {
                    var command = new RichDelegateCommand(resourceKey,
                                                                  resourceKey,
                                                                  null,
                                                                  LoadTangoIcon(resourceKey),
                                                                  null, () => true);
                    m_listOfIconRichCommands.Add(command);

                    command.SetIconProviderDrawScale((double)Application.Current.Resources["MagnificationLevel"]);
                }
            }

            if (m_listOfIconRichCommands2.Count == 0)
            {
                foreach (string resourceKey2 in resourceKeys2)
                {
                    var command = new RichDelegateCommand(resourceKey2,
                                                                  resourceKey2,
                                                                  null,
                                                                  LoadGnomeNeuIcon(resourceKey2),
                                                                  null, () => true);
                    m_listOfIconRichCommands2.Add(command);

                    command.SetIconProviderDrawScale((double)Application.Current.Resources["MagnificationLevel"]);
                }
            }



            if (m_listOfIconRichCommands3.Count == 0)
            {
                foreach (string resourceKey3 in resourceKeys3)
                {
                    var command = new RichDelegateCommand(resourceKey3,
                                                                  resourceKey3,
                                                                  null,
                                                                  LoadGnomeGionIcon(resourceKey3),
                                                                  null, () => true);
                    m_listOfIconRichCommands3.Add(command);

                    command.SetIconProviderDrawScale((double)Application.Current.Resources["MagnificationLevel"]);
                }
            }

            if (m_listOfIconRichCommands4.Count == 0)
            {
                foreach (string resourceKey4 in resourceKeys4)
                {
                    var command = new RichDelegateCommand(resourceKey4,
                                                                  resourceKey4,
                                                                  null,
                                                                  LoadGnomeFoxtrotIcon(resourceKey4),
                                                                  null, () => true);
                    m_listOfIconRichCommands4.Add(command);

                    command.SetIconProviderDrawScale((double)Application.Current.Resources["MagnificationLevel"]);
                }
            }



            var windowPopup = new PopupModalWindow(this,
                                                   UserInterfaceStrings.IconsDebug,
                                                   new IconsPreviewDebug(this),
                                                   PopupModalWindow.DialogButtonsSet.Ok,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 500, 600);

            windowPopup.ShowFloating(() =>
            {
                m_listOfIconRichCommands.Clear();
                m_listOfIconRichCommands2.Clear();
                m_listOfIconRichCommands3.Clear();
                m_listOfIconRichCommands4.Clear();
            });
        }
#endif
    }
}
