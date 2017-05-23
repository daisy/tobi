using System.Windows;
using System.Windows.Media;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;

namespace Tobi.Plugin.AudioPane
{
    public partial class AudioPaneViewModel
    {
        #region Commands

        public RichDelegateCommand CommandAudioSettings { get; private set; }

#if DEBUG
        public RichDelegateCommand CommandShowAudioOptionsDialog { get; private set; }
#endif //DEBUG

        public RichDelegateCommand CommandFocus { get; private set; }
        public RichDelegateCommand CommandFocusStatusBar { get; private set; }

        public RichDelegateCommand CommandZoomSelection { get; private set; }
        public RichDelegateCommand CommandZoomFitFull { get; private set; }
        public RichDelegateCommand CommandZoom_0 { get; private set; }
        public RichDelegateCommand CommandZoom_1 { get; private set; }
        public RichDelegateCommand CommandZoom_2 { get; private set; }
        public RichDelegateCommand CommandZoom_3 { get; private set; }
        public RichDelegateCommand CommandZoom_4 { get; private set; }
        public RichDelegateCommand CommandZoom_5 { get; private set; }
        public RichDelegateCommand CommandZoom_6 { get; private set; }
        public RichDelegateCommand CommandZoom_7 { get; private set; }
        public RichDelegateCommand CommandZoom_8 { get; private set; }
        public RichDelegateCommand CommandZoom_9 { get; private set; }
        public RichDelegateCommand CommandRefresh { get; private set; }
        public RichDelegateCommand CommandStopPlayMonitorRecord { get; private set; }

        private void initializeCommands_View()
        {
            CommandStopPlayMonitorRecord = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioStopRecord_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioStopRecord_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                //null, //ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Refresh")),
                null,
                () =>
                {
                    //Logger.Log("AudioPaneViewModel.CommandRefresh", Category.Debug, Priority.Medium);

                    OnStopPlayMonitorRecord();
                },
                () => true,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StopPlayMonitorRecord));

            m_ShellView.RegisterRichCommand(CommandStopPlayMonitorRecord);
            //
            CommandRefresh = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioReload_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioReload_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                //null, //ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Refresh")),
                m_ShellView.LoadTangoIcon("view-refresh"),
                () =>
                {
                    //Logger.Log("AudioPaneViewModel.CommandRefresh", Category.Debug, Priority.Medium);

                    //StartWaveFormLoadTimer(0);

                    AudioPlayer_LoadWaveForm(false);
                },
                () => CanManipulateWaveForm,
                //!IsWaveFormLoading,
                null, null); //IsAudioLoaded

            m_ShellView.RegisterRichCommand(CommandRefresh);
            //
            CommandZoomSelection = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioZoomSelection_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioZoomSelection_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Search")),
                //shellView.LoadTangoIcon("system-search"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandZoomSelection", Category.Debug, Priority.Medium);

                    View.ZoomSelection();
                },
                () => View != null
                    && State.Audio.HasContent
                    && CanManipulateWaveForm
                    //&&!IsWaveFormLoading
                    && IsSelectionSet,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_ZoomSelection));

            m_ShellView.RegisterRichCommand(CommandZoomSelection);
            //
            CommandZoomFitFull = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioFitFull_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioFitFull_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeNeuIcon("Neu_utilities-system-monitor"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandZoomFitFull", Category.Debug, Priority.Medium);

                    View.ZoomFitFull();
                },
                () => View != null
                    && State.Audio.HasContent
                    && CanManipulateWaveForm,
                //&& !IsWaveFormLoading,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_ZoomFitFull));
            //Settings_KeyGestures.Default.Keyboard_Audio_Zoom_0

            m_ShellView.RegisterRichCommand(CommandZoomFitFull);
            //
            CommandZoom_0 = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioFitFull_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioFitFull_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadGnomeNeuIcon("Neu_utilities-system-monitor"),
                () =>
                {
                    //Logger.Log("AudioPaneViewModel.CommandZoom_0", Category.Debug, Priority.Medium);
                    //View.ZoomFitFull();

                    CommandZoomFitFull.Execute();
                },
                () => CommandZoomFitFull.CanExecute(),
                //&& !IsWaveFormLoading,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_Zoom_0));

            m_ShellView.RegisterRichCommand(CommandZoom_0);
            //
            CommandZoom_1 = new RichDelegateCommand(
                "Zoom 1",
                "Zoom 1",
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadGnomeNeuIcon("Neu_utilities-system-monitor"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandZoom_1", Category.Debug, Priority.Medium);

                    View.Zoom_1();
                },
                () => CommandZoomFitFull.CanExecute(),
                //&& !IsWaveFormLoading,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_Zoom_1));

            m_ShellView.RegisterRichCommand(CommandZoom_1);
            //
            CommandZoom_2 = new RichDelegateCommand(
                "Zoom 2",
                "Zoom 2",
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadGnomeNeuIcon("Neu_utilities-system-monitor"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandZoom_2", Category.Debug, Priority.Medium);

                    View.Zoom_2();
                },
                () => CommandZoomFitFull.CanExecute(),
                //&& !IsWaveFormLoading,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_Zoom_2));

            m_ShellView.RegisterRichCommand(CommandZoom_2);
            //
            CommandZoom_3 = new RichDelegateCommand(
                "Zoom 3",
                "Zoom 3",
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadGnomeNeuIcon("Neu_utilities-system-monitor"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandZoom_3", Category.Debug, Priority.Medium);

                    View.Zoom_3();
                },
                () => CommandZoomFitFull.CanExecute(),
                //&& !IsWaveFormLoading,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_Zoom_3));

            m_ShellView.RegisterRichCommand(CommandZoom_3);
            //
            CommandZoom_4 = new RichDelegateCommand(
                "Zoom 4",
                "Zoom 4",
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadGnomeNeuIcon("Neu_utilities-system-monitor"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandZoom_4", Category.Debug, Priority.Medium);

                    View.Zoom_4();
                },
                () => CommandZoomFitFull.CanExecute(),
                //&& !IsWaveFormLoading,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_Zoom_4));

            m_ShellView.RegisterRichCommand(CommandZoom_4);
            //
            CommandZoom_5 = new RichDelegateCommand(
                "Zoom 5",
                "Zoom 5",
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadGnomeNeuIcon("Neu_utilities-system-monitor"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandZoom_5", Category.Debug, Priority.Medium);

                    View.Zoom_5();
                },
                () => CommandZoomFitFull.CanExecute(),
                //&& !IsWaveFormLoading,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_Zoom_5));

            m_ShellView.RegisterRichCommand(CommandZoom_5);
            //
            CommandZoom_6 = new RichDelegateCommand(
                "Zoom 6",
                "Zoom 6",
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadGnomeNeuIcon("Neu_utilities-system-monitor"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandZoom_6", Category.Debug, Priority.Medium);

                    View.Zoom_6();
                },
                () => CommandZoomFitFull.CanExecute(),
                //&& !IsWaveFormLoading,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_Zoom_6));

            m_ShellView.RegisterRichCommand(CommandZoom_6);
            //
            CommandZoom_7 = new RichDelegateCommand(
                "Zoom 7",
                "Zoom 7",
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadGnomeNeuIcon("Neu_utilities-system-monitor"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandZoom_7", Category.Debug, Priority.Medium);

                    View.Zoom_7();
                },
                () => CommandZoomFitFull.CanExecute(),
                //&& !IsWaveFormLoading,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_Zoom_7));

            m_ShellView.RegisterRichCommand(CommandZoom_7);
            //
            CommandZoom_8 = new RichDelegateCommand(
                "Zoom 8",
                "Zoom 8",
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadGnomeNeuIcon("Neu_utilities-system-monitor"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandZoom_8", Category.Debug, Priority.Medium);

                    View.Zoom_8();
                },
                () => CommandZoomFitFull.CanExecute(),
                //&& !IsWaveFormLoading,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_Zoom_8));

            m_ShellView.RegisterRichCommand(CommandZoom_8);
            //
            CommandZoom_9 = new RichDelegateCommand(
                "Zoom 9",
                "Zoom 9",
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadGnomeNeuIcon("Neu_utilities-system-monitor"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandZoom_9", Category.Debug, Priority.Medium);

                    View.Zoom_9();
                },
                () => CommandZoomFitFull.CanExecute(),
                //&& !IsWaveFormLoading,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_Zoom_9));

            m_ShellView.RegisterRichCommand(CommandZoom_9);
            //
            CommandAudioSettings = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioSettings_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioSettings_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeNeuIcon("Neu_audio-x-generic"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandAudioSettings", Category.Debug, Priority.Medium);

                    var windowPopup = new PopupModalWindow(m_ShellView,
                                                           UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_AudioPane_Lang.CmdAudioSettings_ShortDesc),
                                                           new AudioSettings(this),
                                                           PopupModalWindow.DialogButtonsSet.Close,
                                                           PopupModalWindow.DialogButton.Close,
                                                           true, 420, 220, null, 0,null);
                    windowPopup.EnableEnterKeyDefault = true;
                    windowPopup.ShowFloating(()=>
                        {
                            m_SpeechSynthesizer.SpeakAsyncCancelAll();
                        });
                },
                () => !IsRecording,
                Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_ShowOptions)
                );

            m_ShellView.RegisterRichCommand(CommandAudioSettings);
            //
#if DEBUG
            CommandShowAudioOptionsDialog = new RichDelegateCommand(
                @"Show audio options",
                null,
                null, // KeyGesture obtained from settings (see last parameters below)
                null,
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandShowOptionsDialog", Category.Debug, Priority.Medium);

                    //var window = shellView.View as Window;

                    var pane = new AudioOptions { DataContext = this };

                    var windowPopup = new PopupModalWindow(m_ShellView,
                                                           UserInterfaceStrings.EscapeMnemonic(@"Show audio options"),
                                                           pane,
                                                           PopupModalWindow.DialogButtonsSet.Close,
                                                           PopupModalWindow.DialogButton.Close,
                                                           true, 400, 500, null, 0,null);
                    windowPopup.EnableEnterKeyDefault = true;
                    windowPopup.Show();
                },
                () => true,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_ExpertOptions)
                );

            m_ShellView.RegisterRichCommand(CommandShowAudioOptionsDialog);
#endif //DEBUG
            //
            CommandFocus = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioFocus_ShortDesc,
                null,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("audio-volume-low"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandFocus", Category.Debug, Priority.Medium);

                    View.BringIntoFocus();
                },
                () => View != null,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Focus_Audio));

            m_ShellView.RegisterRichCommand(CommandFocus);
            //
            CommandFocusStatusBar = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioFocusStatusBar_ShortDesc,
                null,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadGnomeNeuIcon("Neu_utilities-terminal"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandFocusStatusBar", Category.Debug, Priority.Medium);

                    View.BringIntoFocusStatusBar();
                },
                () => View != null,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Focus_StatusBar));

            m_ShellView.RegisterRichCommand(CommandFocusStatusBar);
            //
        }

        private void initializeCommands()
        {
            Logger.Log("AudioPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);

            initializeCommands_Selection();
            initializeCommands_Recorder();
            initializeCommands_Player();
            initializeCommands_Edit();
            initializeCommands_Navigation();

            initializeCommands_View();

            if (View != null)
            {
                View.InitGraphicalCommandBindings();
            }
        }

        #endregion Commands
    }
}
