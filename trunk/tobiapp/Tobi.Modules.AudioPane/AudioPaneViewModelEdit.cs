using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using urakawa.core;
using urakawa.media.data.audio;
using urakawa.media.data.audio.codec;

namespace Tobi.Plugin.AudioPane
{
    public partial class AudioPaneViewModel
    {
        public RichDelegateCommand CommandOpenFile { get; private set; }
        public RichDelegateCommand CommandInsertFile { get; private set; }
        public RichDelegateCommand CommandDeleteAudioSelection { get; private set; }

        public RichDelegateCommand CopyCommand { get; private set; }
        public RichDelegateCommand CutCommand { get; private set; }
        public RichDelegateCommand PasteCommand { get; private set; }

        public ManagedAudioMedia AudioClipboard { get; private set; }

        private void initializeCommands_Edit()
        {
            CopyCommand = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.Copy,
                Tobi_Plugin_AudioPane_Lang.Copy_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"edit-copy"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CopyCommand", Category.Debug, Priority.Medium);

                    List<TreeNodeAndStreamSelection> listOfTreeNodeAndStreamSelection = getAudioSelectionData();

                    if (listOfTreeNodeAndStreamSelection.Count == 0)
                    {
                        Debug.Fail("This should never happen !");
                        return;
                    }

                    ManagedAudioMedia managedAudioMediaClipboard = listOfTreeNodeAndStreamSelection[0].m_TreeNode.Presentation.MediaFactory.CreateManagedAudioMedia();
                    var mediaDataClipboard = (WavAudioMediaData)listOfTreeNodeAndStreamSelection[0].m_TreeNode.Presentation.MediaDataFactory.CreateAudioMediaData();
                    managedAudioMediaClipboard.AudioMediaData = mediaDataClipboard;

                    foreach (var treeNodeAndStreamSelection in listOfTreeNodeAndStreamSelection)
                    {
                        ManagedAudioMedia manMedia = treeNodeAndStreamSelection.ExtractManagedAudioMedia();

                        mediaDataClipboard.MergeWith(manMedia.AudioMediaData); // The audio from the parameter gets emptied !

                        // Another way to do it:
                        //Stream streamToBackup = manMedia.AudioMediaData.OpenPcmInputStream();
                        //try
                        //{
                        //    //TimeDelta timeDelta = mediaData.AudioDuration.SubstractTimeDelta(new TimeDelta(timeBegin.TimeAsMillisecondFloat));
                        //    mediaDataClipboard.AppendPcmData(streamToBackup, null);
                        //}
                        //finally
                        //{
                        //    streamToBackup.Close();
                        //}
                    }

                    AudioClipboard = managedAudioMediaClipboard;
                },
                () => !IsWaveFormLoading
                      && !IsPlaying && !IsMonitoring && !IsRecording
                      && m_UrakawaSession.DocumentProject != null
                      && State.CurrentTreeNode != null
                      && IsAudioLoaded && IsSelectionSet,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Copy));

            m_ShellView.RegisterRichCommand(CopyCommand);
            //
            CutCommand = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.Cut,
                Tobi_Plugin_AudioPane_Lang.Cut_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"edit-cut"),
                () =>
                {
                    CopyCommand.Execute();
                    CommandDeleteAudioSelection.Execute();
                },
                () => !IsWaveFormLoading
                      && !IsPlaying && !IsMonitoring && !IsRecording
                      && m_UrakawaSession.DocumentProject != null
                      && State.CurrentTreeNode != null
                      && IsAudioLoaded && IsSelectionSet,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Cut));

            m_ShellView.RegisterRichCommand(CutCommand);
            //
            PasteCommand = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.Paste,
                Tobi_Plugin_AudioPane_Lang.Paste_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"edit-paste"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.PasteCommand", Category.Debug, Priority.Medium);

                    TreeNode treeNode = (State.CurrentSubTreeNode ?? State.CurrentTreeNode);
                    insertAudioAtCursorOrSelectionReplace(treeNode, AudioClipboard.Copy());
                },
                () => AudioClipboard != null
                      && !IsWaveFormLoading
                      && !IsPlaying && !IsMonitoring && !IsRecording
                      && m_UrakawaSession.DocumentProject != null
                      && State.CurrentTreeNode != null
                      //&& IsAudioLoaded
                      ,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Paste));

            m_ShellView.RegisterRichCommand(PasteCommand);
            //
            CommandOpenFile = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.Audio_OpenFile,
                UserInterfaceStrings.Audio_OpenFile_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("document-open"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandOpenFile", Category.Debug, Priority.Medium);

                    State.Audio.PcmFormatAlt = null;
                    openFile(null, false, false);
                },
                () => !IsWaveFormLoading && !IsMonitoring && !IsRecording,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_OpenFile));

            m_ShellView.RegisterRichCommand(CommandOpenFile);
            //
            CommandInsertFile = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.Audio_InsertFile,
                Tobi_Plugin_AudioPane_Lang.Audio_InsertFile_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("folder-open"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandInsertFile", Category.Debug, Priority.Medium);

                    State.Audio.PcmFormatAlt = null;
                    openFile(null, true, false);
                },
                () => !IsWaveFormLoading && !IsPlaying && !IsMonitoring && !IsRecording
                      && m_UrakawaSession.DocumentProject != null && State.CurrentTreeNode != null
                //&& IsAudioLoaded
                      ,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_InsertFile));

            m_ShellView.RegisterRichCommand(CommandInsertFile);
            //
            CommandDeleteAudioSelection = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.Audio_Delete,
                Tobi_Plugin_AudioPane_Lang.Audio_Delete_,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("audio-volume-muted"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandDeleteAudioSelection", Category.Debug, Priority.Medium);

                    List<TreeNodeAndStreamSelection> listOfTreeNodeAndStreamSelection = getAudioSelectionData();

                    if (listOfTreeNodeAndStreamSelection.Count == 0)
                    {
                        Debug.Fail("This should never happen !");
                        return;
                    }

                    if (AudioPlaybackStreamKeepAlive)
                    {
                        ensurePlaybackStreamIsDead();
                    }

                    if (listOfTreeNodeAndStreamSelection.Count == 1)
                    {
                        var command = m_UrakawaSession.DocumentProject.Presentations.Get(0).CommandFactory.
                                    CreateTreeNodeAudioStreamDeleteCommand(listOfTreeNodeAndStreamSelection[0], State.CurrentTreeNode);

                        m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.Execute(command);
                    }
                    else
                    {
                        m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction("Delete spanning audio portion", "Delete a portion of audio that spans across several treenodes");

                        foreach (TreeNodeAndStreamSelection selection in listOfTreeNodeAndStreamSelection)
                        {
                            var command = m_UrakawaSession.DocumentProject.Presentations.Get(0).CommandFactory.
                                        CreateTreeNodeAudioStreamDeleteCommand(selection, State.CurrentTreeNode);

                            m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.Execute(command);
                        }

                        m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();
                    }
                },
                () => !IsWaveFormLoading
                      && !IsPlaying && !IsMonitoring && !IsRecording
                      && m_UrakawaSession.DocumentProject != null
                      && State.CurrentTreeNode != null
                      && IsAudioLoaded && IsSelectionSet,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_Delete));

            m_ShellView.RegisterRichCommand(CommandDeleteAudioSelection);
            //
        }

        private List<TreeNodeAndStreamSelection> getAudioSelectionData()
        {
            long byteSelectionLeft = State.Audio.ConvertMillisecondsToBytes(State.Selection.SelectionBegin);
            long byteSelectionRight = State.Audio.ConvertMillisecondsToBytes(State.Selection.SelectionEnd);

            //long byteLastPlayHeadTime = State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime);

            var listOfTreeNodeAndStreamSelection = new List<TreeNodeAndStreamSelection>();

            long bytesToMatch = byteSelectionLeft;
            long bytesRight = 0;
            long bytesLeft = 0;
            int index = -1;
            foreach (TreeNodeAndStreamDataLength marker in State.Audio.PlayStreamMarkers)
            {
                index++;
                bytesRight += marker.m_LocalStreamDataLength;
                if (bytesToMatch < bytesRight
                || index == (State.Audio.PlayStreamMarkers.Count - 1) && bytesToMatch >= bytesRight)
                {
                    if (listOfTreeNodeAndStreamSelection.Count == 0)
                    {
                        bool rightBoundaryIsAlsoHere = (byteSelectionRight < bytesRight
                                                        ||
                                                        index == (State.Audio.PlayStreamMarkers.Count - 1) &&
                                                        byteSelectionRight >= bytesRight);

                        TreeNodeAndStreamSelection data = new TreeNodeAndStreamSelection()
                        {
                            m_TreeNode = marker.m_TreeNode,
                            m_LocalStreamLeftMark = byteSelectionLeft - bytesLeft,
                            m_LocalStreamRightMark = (rightBoundaryIsAlsoHere ? byteSelectionRight - bytesLeft : -1)
                        };
                        listOfTreeNodeAndStreamSelection.Add(data);

                        if (rightBoundaryIsAlsoHere)
                        {
                            break;
                        }
                        else
                        {
                            bytesToMatch = byteSelectionRight;
                        }
                    }
                    else
                    {
                        TreeNodeAndStreamSelection data = new TreeNodeAndStreamSelection()
                        {
                            m_TreeNode = marker.m_TreeNode,
                            m_LocalStreamLeftMark = -1,
                            m_LocalStreamRightMark = byteSelectionRight - bytesLeft
                        };

                        if (data.m_LocalStreamRightMark > 0)
                            listOfTreeNodeAndStreamSelection.Add(data);

                        break;
                    }
                }
                else if (listOfTreeNodeAndStreamSelection.Count > 0)
                {
                    TreeNodeAndStreamSelection data = new TreeNodeAndStreamSelection()
                    {
                        m_TreeNode = marker.m_TreeNode,
                        m_LocalStreamLeftMark = -1,
                        m_LocalStreamRightMark = -1
                    };
                    listOfTreeNodeAndStreamSelection.Add(data);
                }

                bytesLeft = bytesRight;
            }

            return listOfTreeNodeAndStreamSelection;
        }
    }
}
