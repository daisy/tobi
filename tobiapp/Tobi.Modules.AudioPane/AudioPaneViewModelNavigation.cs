using System.Media;
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
                   Tobi_Plugin_AudioPane_Lang.Audio_GotoBegin,
                   Tobi_Plugin_AudioPane_Lang.Audio_GotoBegin_,
                   null, // KeyGesture obtained from settings (see last parameters below)
                   m_ShellView.LoadTangoIcon("go-first"),
                   () =>
                   {
                       Logger.Log("AudioPaneViewModel.CommandGotoBegining", Category.Debug, Priority.Medium);

                       AudioPlayer_Stop();

                       if (LastPlayHeadTime == 0)
                       {
                           AudioCues.PlayBeep();
                       }
                       else
                       {
                           if (IsAutoPlay)
                           {
                               if (View != null)
                               {
                                   View.ClearSelection();
                               }
                           }

                           LastPlayHeadTime = 0;
                       }
                   },
                   () => !IsWaveFormLoading && IsAudioLoaded && !IsRecording && !IsMonitoring,
                   Settings_KeyGestures.Default,
                   PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_GotoBegin));

            m_ShellView.RegisterRichCommand(CommandGotoBegining);
            //
            CommandGotoEnd = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.Audio_GotoEnd,
                Tobi_Plugin_AudioPane_Lang.Audio_GotoEnd_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("go-last"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandGotoEnd", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    double end = State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength);

                    if (LastPlayHeadTime == end)
                    {
                        AudioCues.PlayBeep();
                    }
                    else
                    {
                        if (IsAutoPlay)
                        {
                            if (View != null)
                            {
                                View.ClearSelection();
                            }
                        }

                        LastPlayHeadTime = end;
                    }
                },
                () => !IsWaveFormLoading && IsAudioLoaded && !IsRecording && !IsMonitoring,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_GotoEnd));

            m_ShellView.RegisterRichCommand(CommandGotoEnd);
            //
            CommandStepBack = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.Audio_StepBack,
                 Tobi_Plugin_AudioPane_Lang.Audio_StepBack_,
                 null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-skip-backward"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStepBack", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    long bytesPlayHead = State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime);

                    long bytesLeftPrevious = -1;

                    State.Audio.FindInPlayStreamMarkersAndDo(bytesPlayHead,
                       (bytesLeft, bytesRight, markerTreeNode, index)
                       =>
                       {
                           if (bytesLeftPrevious == -1)
                           {
                               if (IsAutoPlay)
                               {
                                   if (View != null)
                                   {
                                       View.ClearSelection();
                                   }
                               }

                               LastPlayHeadTime = State.Audio.ConvertBytesToMilliseconds(bytesLeft);
                               AudioCues.PlayBeep();
                               return -1;
                           }

                           if (IsAutoPlay)
                           {
                               if (View != null)
                               {
                                   View.ClearSelection();
                               }
                           }

                           LastPlayHeadTime = State.Audio.ConvertBytesToMilliseconds(bytesLeftPrevious);

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
                () => !IsWaveFormLoading && IsAudioLoadedWithSubTreeNodes && !IsRecording && !IsMonitoring,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StepBack));

            m_ShellView.RegisterRichCommand(CommandStepBack);
            //
            CommandStepForward = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.Audio_StepForward,
                Tobi_Plugin_AudioPane_Lang.Audio_StepForward_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-skip-forward"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStepForward", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    long bytesPlayHead = State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime);

                    long bytesRight;
                    long bytesLeft;
                    int index;
                    TreeNode subTreeNode;
                    bool match = State.Audio.FindInPlayStreamMarkers(bytesPlayHead, out subTreeNode, out index, out bytesLeft, out bytesRight);

                    if (match)
                    {
                        if (IsAutoPlay)
                        {
                            if (View != null)
                            {
                                View.ClearSelection();
                            }
                        }

                        LastPlayHeadTime = State.Audio.ConvertBytesToMilliseconds(bytesRight);
                        return;
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
                Tobi_Plugin_AudioPane_Lang.Audio_FastForward,
                Tobi_Plugin_AudioPane_Lang.Audio_FastForward_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-seek-forward"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandFastForward", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    double newTime = LastPlayHeadTime + m_TimeStepForwardRewind;
                    double max = State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength);
                    if (newTime > max)
                    {
                        newTime = max;
                        AudioCues.PlayBeep();
                    }

                    if (IsAutoPlay)
                    {
                        if (View != null)
                        {
                            View.ClearSelection();
                        }
                    }

                    LastPlayHeadTime = newTime;
                },
                () => !IsWaveFormLoading && IsAudioLoaded && !IsRecording && !IsMonitoring,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_GoForward));

            m_ShellView.RegisterRichCommand(CommandFastForward);
            //
            CommandRewind = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.Audio_Rewind,
                Tobi_Plugin_AudioPane_Lang.Audio_Rewind_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-seek-backward"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandRewind", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    double newTime = LastPlayHeadTime - m_TimeStepForwardRewind;
                    if (newTime < 0)
                    {
                        newTime = 0;
                        AudioCues.PlayBeep();
                    }

                    if (IsAutoPlay)
                    {
                        if (View != null)
                        {
                            View.ClearSelection();
                        }
                    }

                    LastPlayHeadTime = newTime;
                },
                () => !IsWaveFormLoading && IsAudioLoaded && !IsRecording && !IsMonitoring,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_GoBack));

            m_ShellView.RegisterRichCommand(CommandRewind);
        }
    }
}
