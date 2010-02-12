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
                   UserInterfaceStrings.Audio_GotoBegin,
                   UserInterfaceStrings.Audio_GotoBegin_,
                   null, // KeyGesture obtained from settings (see last parameters below)
                   m_ShellView.LoadTangoIcon("go-first"),
                   () =>
                   {
                       Logger.Log("AudioPaneViewModel.CommandGotoBegining", Category.Debug, Priority.Medium);

                       AudioPlayer_Stop();

                       if (LastPlayHeadTime == 0)
                       {
                           SystemSounds.Beep.Play();
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
                UserInterfaceStrings.Audio_GotoEnd,
                UserInterfaceStrings.Audio_GotoEnd_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("go-last"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandGotoEnd", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    double end = State.Audio.ConvertBytesToMilliseconds(State.Audio.DataLength);

                    if (LastPlayHeadTime == end)
                    {
                        SystemSounds.Beep.Play();
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
                UserInterfaceStrings.Audio_StepBack,
                 UserInterfaceStrings.Audio_StepBack_,
                 null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-skip-backward"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStepBack", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    long bytesLeftPrevious = -1;

                    long bytesLeft = 0;
                    long bytesRight = 0;

                    long bytesPlayHead = State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime);

                    int index = -1;

                    foreach (TreeNodeAndStreamDataLength marker in State.Audio.PlayStreamMarkers)
                    {
                        index++;

                        bytesRight = (bytesLeft + marker.m_LocalStreamDataLength);
                        if (bytesPlayHead >= bytesLeft && bytesPlayHead < bytesRight
                            || index == (State.Audio.PlayStreamMarkers.Count - 1) && bytesPlayHead >= bytesRight)
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
                                SystemSounds.Beep.Play();
                                break;
                            }

                            if (IsAutoPlay)
                            {
                                if (View != null)
                                {
                                    View.ClearSelection();
                                }
                            }

                            LastPlayHeadTime = State.Audio.ConvertBytesToMilliseconds(bytesLeftPrevious);
                            break;
                        }
                        bytesLeftPrevious = bytesLeft;
                        bytesLeft += marker.m_LocalStreamDataLength;
                    }
                },
                () => !IsWaveFormLoading && IsAudioLoadedWithSubTreeNodes && !IsRecording && !IsMonitoring,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StepBack));

            m_ShellView.RegisterRichCommand(CommandStepBack);
            //
            CommandStepForward = new RichDelegateCommand(
                UserInterfaceStrings.Audio_StepForward,
                UserInterfaceStrings.Audio_StepForward_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("media-skip-forward"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStepForward", Category.Debug, Priority.Medium);

                    AudioPlayer_Stop();

                    long bytesLeft = 0;
                    long bytesRight = 0;

                    long bytesPlayHead = State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime);

                    bool found = false;

                    int index = -1;
                    foreach (TreeNodeAndStreamDataLength marker in State.Audio.PlayStreamMarkers)
                    {
                        index++;

                        if (found)
                        {
                            if (IsAutoPlay)
                            {
                                if (View != null)
                                {
                                    View.ClearSelection();
                                }
                            }

                            LastPlayHeadTime = State.Audio.ConvertBytesToMilliseconds(bytesLeft);
                            return;
                        }

                        bytesRight = (bytesLeft + marker.m_LocalStreamDataLength);
                        if (bytesPlayHead >= bytesLeft && bytesPlayHead < bytesRight
                            || index == (State.Audio.PlayStreamMarkers.Count - 1) && bytesPlayHead >= bytesRight)
                        {
                            found = true;
                        }

                        bytesLeft += marker.m_LocalStreamDataLength;
                    }

                    if (!found)
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }

                    SystemSounds.Beep.Play();
                },
                () => CommandStepBack.CanExecute(),
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_StepForward));

            m_ShellView.RegisterRichCommand(CommandStepForward);
            //
            CommandFastForward = new RichDelegateCommand(
                UserInterfaceStrings.Audio_FastForward,
                UserInterfaceStrings.Audio_FastForward_,
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
                        SystemSounds.Beep.Play();
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
                UserInterfaceStrings.Audio_Rewind,
                UserInterfaceStrings.Audio_Rewind_,
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
                        SystemSounds.Beep.Play();
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
