using System.Windows;
using System.Windows.Media;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.core;

namespace Tobi.Plugin.AudioPane
{
    public partial class AudioPaneViewModel
    {
        public RichDelegateCommand CommandSelectAll { get; private set; }
        public RichDelegateCommand CommandClearSelection { get; private set; }
        public RichDelegateCommand CommandBeginSelection { get; private set; }
        public RichDelegateCommand CommandEndSelection { get; private set; }
        public RichDelegateCommand CommandSelectNextChunk { get; private set; }
        public RichDelegateCommand CommandSelectPreviousChunk { get; private set; }

        private void initializeCommands_Selection()
        {
            CommandSelectPreviousChunk = new RichDelegateCommand(
                UserInterfaceStrings.Audio_SelectPreviousChunk,
                UserInterfaceStrings.Audio_SelectPreviousChunk_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("go-previous"),
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandSelectPreviousChunk", Category.Debug, Priority.Medium);

                    CommandStepBack.Execute();

                    SelectChunk(State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime));
                },
                () => CommandStepBack.CanExecute(),
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_SelectPreviousChunk));

            m_ShellView.RegisterRichCommand(CommandSelectPreviousChunk);
            //
            CommandSelectNextChunk = new RichDelegateCommand(
                UserInterfaceStrings.Audio_SelectNextChunk,
                UserInterfaceStrings.Audio_SelectNextChunk_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("go-next"),
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandSelectNextChunk", Category.Debug, Priority.Medium);

                    CommandStepForward.Execute();

                    SelectChunk(State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime));
                },
                () => CommandStepForward.CanExecute(),
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_SelectNextChunk));

            m_ShellView.RegisterRichCommand(CommandSelectNextChunk);
            //
            //
            CommandEndSelection = new RichDelegateCommand(
                UserInterfaceStrings.Audio_EndSelection,
                UserInterfaceStrings.Audio_EndSelection_,
                null, // KeyGesture obtained from settings (see last parameters below)
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Right1")),
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandEndSelection", Category.Debug, Priority.Medium);

                    AudioCues.PlayTockTock();

                    if (m_SelectionBeginTmp < 0)
                    {
                        return;
                    }
                    double begin = m_SelectionBeginTmp;
                    double end = LastPlayHeadTime;

                    if (begin == end)
                    {
                        CommandClearSelection.Execute();
                        return;
                    }

                    if (begin > end)
                    {
                        double tmp = begin;
                        begin = end;
                        end = tmp;
                    }

                    State.Selection.SetSelectionTime(begin, end);

                    if (IsAutoPlay)
                    {
                        if (!State.Audio.HasContent)
                        {
                            return;
                        }

                        IsAutoPlay = false;
                        LastPlayHeadTime = begin;
                        IsAutoPlay = true;

                        long bytesFrom = State.Audio.ConvertMillisecondsToBytes(begin);
                        long bytesTo = State.Audio.ConvertMillisecondsToBytes(end);

                        AudioPlayer_PlayFromTo(bytesFrom, bytesTo);
                    }
                },
                () => !IsWaveFormLoading && IsAudioLoaded && m_SelectionBeginTmp >= 0,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_EndSelection));

            m_ShellView.RegisterRichCommand(CommandEndSelection);
            //
            CommandBeginSelection = new RichDelegateCommand(
                UserInterfaceStrings.Audio_BeginSelection,
                UserInterfaceStrings.Audio_BeginSelection_,
                null, // KeyGesture obtained from settings (see last parameters below)
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Left1")),
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandBeginSelection", Category.Debug, Priority.Medium);

                    m_SelectionBeginTmp = LastPlayHeadTime;

                    AudioCues.PlayTock();
                },
                () => !IsWaveFormLoading && IsAudioLoaded,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_BeginSelection));

            m_ShellView.RegisterRichCommand(CommandBeginSelection);
            //
            CommandSelectAll = new RichDelegateCommand(
                UserInterfaceStrings.SelectAll,
                UserInterfaceStrings.SelectAll_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("view-fullscreen"),
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandSelectAll", Category.Debug, Priority.Medium);

                    if (!State.Audio.HasContent)
                    {
                        if (View != null)
                        {
                            View.SelectAll();
                        }
                        return;
                    }

                    State.Selection.SetSelectionBytes(0, State.Audio.DataLength);
                    
                    AudioCues.PlayTockTock();
                },
                () => !IsWaveFormLoading,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_SelectAll));

            m_ShellView.RegisterRichCommand(CommandSelectAll);
            //
            CommandClearSelection = new RichDelegateCommand(UserInterfaceStrings.Audio_ClearSelection,
                UserInterfaceStrings.Audio_ClearSelection_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("edit-clear"),
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandClearSelection", Category.Debug, Priority.Medium);

                    State.Selection.ClearSelection();
                },
                () => !IsWaveFormLoading && IsSelectionSet,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_ClearSelection));

            m_ShellView.RegisterRichCommand(CommandClearSelection);
            //
        }

        private double m_SelectionBeginTmp = -1;

        public void SelectChunk(long byteOffset)
        {
            if (State.CurrentTreeNode == null || !State.Audio.HasContent)
            {
                return;
            }

            //if (PlayStreamMarkers == null || PlayStreamMarkers.Count == 1)
            //{
            //    SelectAll();
            //    return;
            //}

            long bytesRight = 0;
            long bytesLeft = 0;
            int index = -1;
            foreach (TreeNodeAndStreamDataLength marker in State.Audio.PlayStreamMarkers)
            {
                index++;
                bytesRight += marker.m_LocalStreamDataLength;
                if (byteOffset < bytesRight
                    || index == (State.Audio.PlayStreamMarkers.Count - 1) && byteOffset >= bytesRight)
                {
                    //subTreeNode = marker.m_TreeNode;

                    State.Selection.SetSelectionBytes(bytesLeft, bytesRight);

                    break;
                }
                bytesLeft = bytesRight;
            }
        }

        [NotifyDependsOnEx("SelectionBegin", typeof(SelectionStateData))]
        [NotifyDependsOnEx("SelectionEnd", typeof(SelectionStateData))]
        public bool IsSelectionSet
        {
            get
            {
                if (State.Audio.HasContent)
                {
                    return State.Selection.SelectionBegin >= 0 && State.Selection.SelectionEnd >= 0;
                }
                if (View != null)
                {
                    return View.IsSelectionSet;
                }
                return false;
            }
        }
    }
}
