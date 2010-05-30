using System;
using System.Diagnostics;
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

        public RichDelegateCommand CommandSelectLeft { get; private set; }
        public RichDelegateCommand CommandSelectRight { get; private set; }

        public RichDelegateCommand CommandClearSelection { get; private set; }

        public RichDelegateCommand CommandBeginSelection { get; private set; }
        public RichDelegateCommand CommandEndSelection { get; private set; }

        public RichDelegateCommand CommandSelectNextChunk { get; private set; }
        public RichDelegateCommand CommandSelectPreviousChunk { get; private set; }

        private void initializeCommands_Selection()
        {
            CommandSelectPreviousChunk = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioSelectPreviousChunk_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioSelectPreviousChunk_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("go-previous"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandSelectPreviousChunk", Category.Debug, Priority.Medium);

                    CommandStepBack.Execute();

                    SelectChunk(PlayBytePosition);
                },
                () => CommandStepBack.CanExecute(),
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_SelectPreviousChunk));

            m_ShellView.RegisterRichCommand(CommandSelectPreviousChunk);
            //
            CommandSelectNextChunk = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioSelectNextChunk_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioSelectNextChunk_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("go-next"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandSelectNextChunk", Category.Debug, Priority.Medium);

                    CommandStepForward.Execute();

                    SelectChunk(PlayBytePosition);
                },
                () => CommandStepForward.CanExecute(),
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_SelectNextChunk));

            m_ShellView.RegisterRichCommand(CommandSelectNextChunk);
            //
            //
            CommandEndSelection = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioEndSelection_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioEndSelection_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Right1")),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandEndSelection", Category.Debug, Priority.Medium);

                    if (m_SelectionBeginTmpBytePosition < 0)
                    {
                        return;
                    }

                    CommandPause.Execute();

                    long begin = m_SelectionBeginTmpBytePosition;
                    long end = PlayBytePosition;

                    //AudioCues.PlayTockTock();

                    if (begin == end)
                    {
                        CommandClearSelection.Execute();
                        return;
                    }

                    if (begin > end)
                    {
                        long tmp = begin;
                        begin = end;
                        end = tmp;
                    }

                    State.Selection.SetSelectionBytes(begin, end);

                    //if (IsAutoPlay)
                    //{
                    //    //if (!State.Audio.HasContent)
                    //    //{
                    //    //    return;
                    //    //}

                    //    //IsAutoPlay = false;
                    //    //LastPlayHeadTime = begin;
                    //    //IsAutoPlay = true;

                    //    //long bytesFrom = State.Audio.ConvertMillisecondsToBytes(begin);
                    //    //long bytesTo = State.Audio.ConvertMillisecondsToBytes(end);

                    //    //AudioPlayer_PlayFromTo(bytesFrom, bytesTo);
                    //}
                },
                () => CommandSelectAll.CanExecute()
                    //CanManipulateWaveForm
                    // //&& !IsWaveFormLoading && !IsRecording && !IsMonitoring
                    // && State.Audio.HasContent
                     && m_SelectionBeginTmpBytePosition >= 0,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_EndSelection));

            m_ShellView.RegisterRichCommand(CommandEndSelection);
            //
            CommandBeginSelection = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioBeginSelection_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioBeginSelection_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Left1")),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandBeginSelection", Category.Debug, Priority.Medium);

                    CommandPause.Execute();

                    m_SelectionBeginTmpBytePosition = PlayBytePosition;

                    //AudioCues.PlayTock();
                },
                () => CommandSelectLeft.CanExecute(),
                //CommandSelectAll.CanExecute()
                //         //CanManipulateWaveForm
                //         ////&& !IsWaveFormLoading && !IsRecording && !IsMonitorin
                //         //&& State.Audio.HasContent,
                //         && PlayBytePosition >= 0,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_BeginSelection));

            m_ShellView.RegisterRichCommand(CommandBeginSelection);
            //
            CommandSelectLeft = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioSelectLeft_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioSelectLeft_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("format-indent-less"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandSelectLeft", Category.Debug, Priority.Medium);

                    if (PlayBytePosition <= 0)
                    {
                        AudioCues.PlayBeep();
                        return;
                    }

                    State.Selection.SetSelectionBytes(0, PlayBytePosition);
                    //AudioCues.PlayTock();
                },
                () => CommandSelectAll.CanExecute()
                    //CanManipulateWaveForm
                    // //&& !IsWaveFormLoading && !IsRecording && !IsMonitoring
                    // && State.Audio.HasContent
                     && PlayBytePosition >= 0,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_SelectLeft));

            m_ShellView.RegisterRichCommand(CommandSelectLeft);
            //
            CommandSelectRight = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioSelectRight_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioSelectRight_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("format-indent-more"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandSelectRight", Category.Debug, Priority.Medium);

                    if (PlayBytePosition >= State.Audio.DataLength)
                    {
                        AudioCues.PlayBeep();
                        return;
                    }

                    State.Selection.SetSelectionBytes(PlayBytePosition, State.Audio.DataLength);
                    //AudioCues.PlayTock();
                },
                () => CommandSelectLeft.CanExecute(),
                //CanManipulateWaveForm
                // //&& !IsWaveFormLoading && !IsRecording && !IsMonitoring
                // && State.Audio.HasContent && PlayBytePosition >= 0,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_SelectRight));

            m_ShellView.RegisterRichCommand(CommandSelectRight);
            //
            CommandSelectAll = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdSelectAll_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdSelectAll_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("view-fullscreen"),
                () =>
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

                    //AudioCues.PlayTockTock();
                },
                () => CanManipulateWaveForm
                    //&& !IsWaveFormLoading && !IsRecording && !IsMonitoring
                     && State.Audio.HasContent,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_SelectAll));

            m_ShellView.RegisterRichCommand(CommandSelectAll);
            //
            CommandClearSelection = new RichDelegateCommand(Tobi_Plugin_AudioPane_Lang.CmdAudioClearSelection_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioClearSelection_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("edit-clear"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandClearSelection", Category.Debug, Priority.Medium);

                    State.Selection.ClearSelection();
                },
                () => CommandSelectAll.CanExecute()
                    //CanManipulateWaveForm
                    // //&& !IsWaveFormLoading && !IsRecording && !IsMonitoring
                    // && State.Audio.HasContent
                     && IsSelectionSet,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_ClearSelection));

            m_ShellView.RegisterRichCommand(CommandClearSelection);
            //
        }

        private long m_SelectionBeginTmpBytePosition = -1;

        public void SelectChunk(long byteOffset)
        {
            Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
            if (treeNodeSelection.Item1 == null || !State.Audio.HasContent)
            {
                return;
            }

            //if (PlayStreamMarkers == null || PlayStreamMarkers.Count == 1)
            //{
            //    SelectAll();
            //    return;
            //}

            long bytesRight;
            long bytesLeft;
            int index;
            TreeNode subTreeNode;
            bool match = State.Audio.FindInPlayStreamMarkers(byteOffset, out subTreeNode, out index, out bytesLeft, out bytesRight);

            if (match)
            {
                State.Selection.SetSelectionBytes(bytesLeft, bytesRight);
            }
            else
            {
                Debug.Fail("audio chunk not found ??");
            }
        }

        [NotifyDependsOnEx("SelectionBeginBytePosition", typeof(SelectionStateData))]
        [NotifyDependsOnEx("SelectionEndBytePosition", typeof(SelectionStateData))]
        public bool IsSelectionSet
        {
            get
            {
                if (State.Audio.HasContent)
                {
                    return State.Selection.SelectionBeginBytePosition >= 0 && State.Selection.SelectionEndBytePosition >= 0;
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
