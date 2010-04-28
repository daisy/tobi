using System.Media;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using urakawa.core;
using System.Diagnostics;
namespace Tobi.Plugin.AudioPane
{
    public partial class AudioPaneViewModel
    {
        public RichDelegateCommand CommandGotoBegining { get; private set; }
        public RichDelegateCommand CommandGotoEnd { get; private set; }
        public RichDelegateCommand CommandStepBack { get; private set; }
        public RichDelegateCommand CommandStepForward { get; private set; }
        public RichDelegateCommand CommandRewind { get; private set; }
        public RichDelegateCommand CommandFastForward { get; private set; }

        private void initializeCommands_Navigation()
        {
            CommandGotoBegining = new RichDelegateCommand(
                   Tobi_Plugin_AudioPane_Lang.CmdAudioGotoBegin_ShortDesc,
                   Tobi_Plugin_AudioPane_Lang.CmdAudioGotoBegin_LongDesc,
                   null, // KeyGesture obtained from settings (see last parameters below)
                   m_ShellView.LoadTangoIcon("go-first"),
                   () =>
                   {
                       Logger.Log("AudioPaneViewModel.CommandGotoBegining", Category.Debug, Priority.Medium);

                       CommandPause.Execute();

                       if (PlayBytePosition == 0)
                       {
                           AudioCues.PlayBeep();
                       }
                       else
                       {
                           if (IsAutoPlay)
                           {
                               State.Selection.ClearSelection();
                           }

                           PlayBytePosition = 0;
                       }
                   },
                   () => !IsWaveFormLoading && State.Audio.HasContent && !IsRecording && !IsMonitoring,
                   Settings_KeyGestures.Default,
                   PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_GotoBegin));

            m_ShellView.RegisterRichCommand(CommandGotoBegining);
            //
            CommandGotoEnd = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioGotoEnd_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioGotoEnd_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("go-last"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandGotoEnd", Category.Debug, Priority.Medium);

                    CommandPause.Execute();

                    if (PlayBytePosition == State.Audio.DataLength)
                    {
                        AudioCues.PlayBeep();
                    }
                    else
                    {
                        if (IsAutoPlay)
                        {
                            State.Selection.ClearSelection();
                        }

                        PlayBytePosition = State.Audio.DataLength;
                    }
                },
                () => !IsWaveFormLoading && State.Audio.HasContent && !IsRecording && !IsMonitoring,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_GotoEnd));

            m_ShellView.RegisterRichCommand(CommandGotoEnd);
            //
            CommandStepBack = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioStepBack_ShortDesc,
                 Tobi_Plugin_AudioPane_Lang.CmdAudioStepBack_LongDesc,
                 null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-skip-backward"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStepBack", Category.Debug, Priority.Medium);

                    CommandPause.Execute();

                    long bytesLeftPrevious = -1;

                    State.Audio.FindInPlayStreamMarkersAndDo(PlayBytePosition,
                       (bytesLeft, bytesRight, markerTreeNode, index)
                       =>
                       {
                           if (bytesLeftPrevious == -1)
                           {
                               if (IsAutoPlay)
                               {
                                   State.Selection.ClearSelection();
                               }

                               PlayBytePosition = bytesLeft;
                               AudioCues.PlayBeep();
                               return -1;
                           }

                           if (IsAutoPlay)
                           {
                               State.Selection.ClearSelection();
                           }

                           PlayBytePosition = bytesLeftPrevious;

                           return -1;
                       }
                        ,
                       (bytesToMatch_, bytesLeft, bytesRight, markerTreeNode)
                       =>
                       {
                           bytesLeftPrevious = bytesLeft;
                           return bytesToMatch_;
                       }
                        );

                },
                () => !IsWaveFormLoading && !IsRecording && !IsMonitoring
                    && State.Audio.HasContent && State.Audio.PlayStreamMarkers != null && State.Audio.PlayStreamMarkers.Count > 1,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StepBack));

            m_ShellView.RegisterRichCommand(CommandStepBack);
            //
            CommandStepForward = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioStepForward_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioStepForward_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-skip-forward"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStepForward", Category.Debug, Priority.Medium);

                    CommandPause.Execute();

                    long bytesRight;
                    long bytesLeft;
                    int index;
                    TreeNode subTreeNode;
                    bool match = State.Audio.FindInPlayStreamMarkers(PlayBytePosition, out subTreeNode, out index, out bytesLeft, out bytesRight);

                    if (match)
                    {
                        if (index == State.Audio.PlayStreamMarkers.Count - 1)
                        {
                            AudioCues.PlayBeep();
                            return;
                        }

                        if (IsAutoPlay)
                        {
                            State.Selection.ClearSelection();
                        }

                        PlayBytePosition = bytesRight;
                    }
                    else
                    {
                        Debug.Fail("audio chunk not found ??");

                        AudioCues.PlayBeep();
                    }
                },
                () => CommandStepBack.CanExecute(),
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StepForward));

            m_ShellView.RegisterRichCommand(CommandStepForward);
            //
            CommandFastForward = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioFastForward_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioFastForward_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-seek-forward"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandFastForward", Category.Debug, Priority.Medium);

                    CommandPause.Execute();

                    long newBytesPosition = PlayBytePosition + State.Audio.GetCurrentPcmFormat().Data.ConvertTimeToBytes((long)Settings.Default.AudioWaveForm_TimeStep * AudioLibPCMFormat.TIME_UNIT);
                    if (newBytesPosition > State.Audio.DataLength)
                    {
                        newBytesPosition = State.Audio.DataLength;
                        AudioCues.PlayBeep();
                    }

                    if (IsAutoPlay)
                    {
                        State.Selection.ClearSelection();
                    }

                    PlayBytePosition = newBytesPosition;
                },
                () => !IsWaveFormLoading && State.Audio.HasContent && !IsRecording && !IsMonitoring,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_GoForward));

            m_ShellView.RegisterRichCommand(CommandFastForward);
            //
            CommandRewind = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioRewind_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioRewind_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-seek-backward"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandRewind", Category.Debug, Priority.Medium);

                    CommandPause.Execute();

                    long newTimeBytePosition = PlayBytePosition - State.Audio.GetCurrentPcmFormat().Data.ConvertTimeToBytes((long)Settings.Default.AudioWaveForm_TimeStep * AudioLibPCMFormat.TIME_UNIT);
                    if (newTimeBytePosition < 0)
                    {
                        newTimeBytePosition = 0;
                        AudioCues.PlayBeep();
                    }

                    if (IsAutoPlay)
                    {
                        State.Selection.ClearSelection();
                    }

                    PlayBytePosition = newTimeBytePosition;
                },
                () => !IsWaveFormLoading && State.Audio.HasContent && !IsRecording && !IsMonitoring,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_GoBack));

            m_ShellView.RegisterRichCommand(CommandRewind);
        }
    }
}
