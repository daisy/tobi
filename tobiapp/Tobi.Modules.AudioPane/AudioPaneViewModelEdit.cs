using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using urakawa.command;
using urakawa.core;
using urakawa.data;
using urakawa.media;
using urakawa.media.data.audio;
using urakawa.media.data.audio.codec;
using urakawa.media.timing;

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
                Tobi_Plugin_AudioPane_Lang.CmdCopy_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdCopy_LongDesc,
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
                        //    //Time timeDelta = mediaData.AudioDuration.Substract(new Time(timeBegin.TimeAsMillisecondFloat));
                        //    mediaDataClipboard.AppendPcmData(streamToBackup, null);
                        //}
                        //finally
                        //{
                        //    streamToBackup.Close();
                        //}
                    }

                    AudioClipboard = managedAudioMediaClipboard;
                },
                () =>
                {
                    Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

                    return !IsWaveFormLoading
                           && !IsPlaying && !IsMonitoring && !IsRecording
                           && m_UrakawaSession.DocumentProject != null
                           && treeNodeSelection.Item1 != null
                           && IsAudioLoaded && IsSelectionSet;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Copy));

            m_ShellView.RegisterRichCommand(CopyCommand);
            //
            CutCommand = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdCut_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdCut_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"edit-cut"),
                () =>
                {
                    CopyCommand.Execute();
                    CommandDeleteAudioSelection.Execute();
                },
                () =>
                {
                    Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

                    return !IsWaveFormLoading
                           && !IsPlaying && !IsMonitoring && !IsRecording
                           && m_UrakawaSession.DocumentProject != null
                           && treeNodeSelection.Item1 != null
                           && IsAudioLoaded && IsSelectionSet;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Cut));

            m_ShellView.RegisterRichCommand(CutCommand);
            //
            PasteCommand = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdPaste_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdPaste_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"edit-paste"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.PasteCommand", Category.Debug, Priority.Medium);

                    insertAudioAtCursorOrSelectionReplace(AudioClipboard.Copy());
                },
                () =>
                {
                    Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

                    return AudioClipboard != null && !IsWaveFormLoading && !IsPlaying && !IsMonitoring && !IsRecording
                           && m_UrakawaSession.DocumentProject != null
                           &&
                           (
                           State.Audio.PlayStreamMarkers != null
                           ||
                           treeNodeSelection.Item1 != null
                           && treeNodeSelection.Item1.GetXmlElementQName() != null
                           && treeNodeSelection.Item1.GetFirstAncestorWithManagedAudio() == null
                           );
                }
                //&& IsAudioLoaded
                      ,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Paste));

            m_ShellView.RegisterRichCommand(PasteCommand);
            //
            CommandOpenFile = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioOpenFile_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioOpenFile_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("document-open"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandOpenFile", Category.Debug, Priority.Medium);

                    openFile(null, false, false, null);
                },
                () => !IsWaveFormLoading && !IsMonitoring && !IsRecording,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_OpenFile));

            m_ShellView.RegisterRichCommand(CommandOpenFile);
            //
            CommandInsertFile = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioInsertFile_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioInsertFile_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("folder-open"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandInsertFile", Category.Debug, Priority.Medium);

                    openFile(null, true, false, null);
                },
                () =>
                {
                    Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

                    return !IsWaveFormLoading && !IsPlaying && !IsMonitoring && !IsRecording
                           && m_UrakawaSession.DocumentProject != null
                           &&
                           (
                           State.Audio.PlayStreamMarkers != null
                           ||
                           treeNodeSelection.Item1 != null
                           && treeNodeSelection.Item1.GetXmlElementQName() != null
                           && treeNodeSelection.Item1.GetFirstAncestorWithManagedAudio() == null
                           );
                }
                //&& IsAudioLoaded
                      ,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_InsertFile));

            m_ShellView.RegisterRichCommand(CommandInsertFile);
            //
            CommandDeleteAudioSelection = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioDelete_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioDelete_LongDesc,
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

                    State.Selection.ClearSelection();

                    Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

                    if (listOfTreeNodeAndStreamSelection.Count == 1)
                    {
                        var command = m_UrakawaSession.DocumentProject.Presentations.Get(0).CommandFactory.
                                    CreateTreeNodeAudioStreamDeleteCommand(listOfTreeNodeAndStreamSelection[0], treeNodeSelection.Item1);

                        m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.Execute(command);
                    }
                    else
                    {
                        m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction(Tobi_Plugin_AudioPane_Lang.CmdTransaction_DeleteAudio, Tobi_Plugin_AudioPane_Lang.CmdTransaction_DeleteAudioInSeveraTreenodes); // TODO LOCALIZE CmdTransaction_DeleteAudio, CmdTransaction_DeleteAudioInSeveraTreenodes

                        foreach (TreeNodeAndStreamSelection selection in listOfTreeNodeAndStreamSelection)
                        {
                            var command = m_UrakawaSession.DocumentProject.Presentations.Get(0).CommandFactory.
                                        CreateTreeNodeAudioStreamDeleteCommand(selection, treeNodeSelection.Item1);

                            m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.Execute(command);
                        }

                        m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();
                    }
                },
                () =>
                {
                    //Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
                    return !IsWaveFormLoading
                           && !IsPlaying && !IsMonitoring && !IsRecording
                           && m_UrakawaSession.DocumentProject != null
                        //&& treeNodeSelection.Item1 != null
                           && State.Audio.PlayStreamMarkers != null
                           && IsAudioLoaded && IsSelectionSet;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_Delete));

            m_ShellView.RegisterRichCommand(CommandDeleteAudioSelection);
            //
        }



        private void openFile(String str, bool insert, bool deleteAfterInsert, PCMFormatInfo pcmInfo)
        {
            Logger.Log("AudioPaneViewModel.OpenFile", Category.Debug, Priority.Medium);

            AudioPlayer_Stop();

            string filePath = str;

            if (String.IsNullOrEmpty(filePath) && View != null)
            {
                filePath = View.OpenFileDialog();
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }

            bool isWav = Path.GetExtension(filePath).ToLower() == ".wav";

            AudioLibPCMFormat wavFormat = (pcmInfo != null ? pcmInfo.Copy().Data : null);
            if (isWav && wavFormat == null)
            {
                Stream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    uint dataLength;
                    wavFormat = AudioLibPCMFormat.RiffHeaderParse(fileStream, out dataLength);
                }
                finally
                {
                    fileStream.Close();
                }
            }

            Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

            TreeNode treeNode = (treeNodeSelection.Item2 ?? treeNodeSelection.Item1);

            if (insert && m_UrakawaSession.DocumentProject != null && treeNode != null)
            {
                string originalFilePath = null;

                Debug.Assert(m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.EnforceSinglePCMFormat);

                bool wavNeedsConversion = false;
                if (wavFormat != null)
                {
                    if (wavFormat.IsCompatibleWith(m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat.Data))
                    {
                        State.Audio.PcmFormat = new PCMFormatInfo(wavFormat);
                        //RaisePropertyChanged(() => State.Audio.PcmFormat);
                    }
                    else
                    {
                        wavNeedsConversion = true;
                    }
                }

                if (!isWav || wavNeedsConversion)
                {
                    originalFilePath = filePath;

                    filePath = m_AudioFormatConvertorSession.ConvertAudioFileFormat(filePath);

                    Logger.Log(string.Format("Converted audio {0} to {1}", originalFilePath, filePath),
                               Category.Debug, Priority.Medium);

                    Stream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    try
                    {
                        uint dataLength;
                        wavFormat = AudioLibPCMFormat.RiffHeaderParse(fileStream, out dataLength);
                        State.Audio.PcmFormat = new PCMFormatInfo(wavFormat);
                        //RaisePropertyChanged(() => State.Audio.PcmFormat);
                    }
                    finally
                    {
                        fileStream.Close();
                    }
                }

                //AudioCues.PlayTockTock();

                ManagedAudioMedia managedAudioMedia = treeNode.Presentation.MediaFactory.CreateManagedAudioMedia();

                var mediaData = (WavAudioMediaData)treeNode.Presentation.MediaDataFactory.CreateAudioMediaData();

                managedAudioMedia.AudioMediaData = mediaData;

                //Directory.GetParent(filePath).FullName
                //bool recordedFileIsInDataDir = Path.GetDirectoryName(filePath) == nodeRecord.Presentation.DataProviderManager.DataFileDirectoryFullPath;

                if (deleteAfterInsert)
                {
                    FileDataProvider dataProv = (FileDataProvider)treeNode.Presentation.DataProviderFactory.Create(DataProviderFactory.AUDIO_WAV_MIME_TYPE);
                    dataProv.InitByMovingExistingFile(filePath);
                    mediaData.AppendPcmData(dataProv);
                }
                else
                {
                    // TODO: progress ! (time consuming file copy)
                    mediaData.AppendPcmData_RiffHeader(filePath);
                }

                if (deleteAfterInsert && File.Exists(filePath)) //check exist just in case file adopted by DataProviderManager
                {
                    File.Delete(filePath);
                }

                if (!string.IsNullOrEmpty(originalFilePath)
                    && deleteAfterInsert && File.Exists(originalFilePath)) //check exist just in case file adopted by DataProviderManager
                {
                    File.Delete(originalFilePath);
                }

                insertAudioAtCursorOrSelectionReplace(managedAudioMedia);

                return;
            }

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
            }

            if (!isWav)
            {
                string originalFilePath = filePath;

                filePath = m_AudioFormatConvertorSession_NoProject.ConvertAudioFileFormat(filePath);

                Logger.Log(string.Format("Converted audio {0} to {1}", originalFilePath, filePath),
                           Category.Debug, Priority.Medium);

                Stream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    uint dataLength;
                    wavFormat = AudioLibPCMFormat.RiffHeaderParse(fileStream, out dataLength);
                }
                finally
                {
                    fileStream.Close();
                }
            }

            State.Audio.PcmFormat = new PCMFormatInfo(wavFormat);
            //RaisePropertyChanged(() => State.Audio.PcmFormat);

            AudioPlayer_LoadAndPlayFromFile(filePath);
        }

        private void insertAudioAtCursorOrSelectionReplace(ManagedAudioMedia manMedia)
        {
            Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode treeNode = (treeNodeSelection.Item2 ?? treeNodeSelection.Item1);
            if (treeNode == null) return;

            if (View != null)
            {
                View.ResetAll();
            }

            if (State.Audio.PlayStreamMarkers == null)
            {
                //Debug.Assert(State.Audio.PlayStream == null);
                var media = treeNode.GetManagedAudioMediaOrSequenceMedia();
                if (media != null)
                {
                    Debug.Fail("Waht ??");
                    return;
                }
                Debug.Assert(treeNode.GetFirstDescendantWithManagedAudio() == null);
                Debug.Assert(treeNode.GetFirstAncestorWithManagedAudio() == null);

                var command_ = treeNode.Presentation.CommandFactory.CreateTreeNodeSetManagedAudioMediaCommand(treeNode, manMedia);
                treeNode.Presentation.UndoRedoManager.Execute(command_);

                return;
            }

            double timeInsert = 0;
            bool transaction = false;
            List<TreeNodeAndStreamSelection> selData = null;
            if (IsSelectionSet)
            {
                timeInsert = State.Selection.SelectionBegin;

                transaction = true;
                treeNode.Presentation.UndoRedoManager.StartTransaction("Replace audio selection", "delete the current audio selection and insert new audio");

                selData = getAudioSelectionData();

                CommandDeleteAudioSelection.Execute();
            }
            else
            {
                timeInsert = LastPlayHeadTime;

                if (timeInsert < 0)
                {
                    var media = treeNode.GetManagedAudioMediaOrSequenceMedia();
                    if (media != null)
                    {
                        Debug.Fail("Waht ??");
                        return;
                    }
                    Debug.Assert(treeNode.GetFirstDescendantWithManagedAudio() == null);
                    Debug.Assert(treeNode.GetFirstAncestorWithManagedAudio() == null);

                    if (AudioPlaybackStreamKeepAlive)
                    {
                        ensurePlaybackStreamIsDead();
                    }

                    var command_ = treeNode.Presentation.CommandFactory.CreateTreeNodeSetManagedAudioMediaCommand(treeNode, manMedia);
                    treeNode.Presentation.UndoRedoManager.Execute(command_);

                    return;
                }
            }

            long byteOffset = State.Audio.ConvertMillisecondsToBytes(timeInsert);
            double timeOffset = timeInsert;
            TreeNode treeNodeTarget;
            long bytesRight;
            long bytesLeft;
            int index;
            bool match = State.Audio.FindInPlayStreamMarkers(byteOffset, out treeNodeTarget, out index, out bytesLeft, out bytesRight);
            if (!match)
            {
                Debug.Fail("Waht ??");

                if (transaction)
                {
                    treeNode.Presentation.UndoRedoManager.EndTransaction();
                }
                return;
            }
            
            timeOffset = State.Audio.ConvertBytesToMilliseconds(byteOffset - bytesLeft);

            if (selData != null && selData.Count > 0)
            {
            }

            Media treeNodeAudio = treeNode.GetManagedAudioMedia();
            Debug.Assert(treeNodeAudio != null);

            Command command = treeNode.Presentation.CommandFactory.
                   CreateManagedAudioMediaInsertDataCommand(
                       treeNode, manMedia,
                       new Time(timeOffset),
                       treeNodeSelection.Item1);

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
            }

            treeNode.Presentation.UndoRedoManager.Execute(command);

            if (transaction)
            {
                treeNode.Presentation.UndoRedoManager.EndTransaction();
            }


            //if (audioMedia is ManagedAudioMedia)
            //{
            //    var managedAudioMedia = (ManagedAudioMedia)audioMedia;

            //    double timeOffset = LastPlayHeadTime;
            //    if (treeNodeSelection.Item2 != null)
            //    {
            //        var byteOffset = State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime);

            //        long bytesRight;
            //        long bytesLeft;
            //        int index;
            //        TreeNode subTreeNode;
            //        bool match = State.Audio.FindInPlayStreamMarkers(byteOffset, out subTreeNode, out index, out bytesLeft, out bytesRight);

            //        if (match)
            //        {
            //            if (treeNodeSelection.Item2 != subTreeNode
            //                       && !treeNodeSelection.Item2.IsDescendantOf(subTreeNode))
            //            {
            //                //recordingStream.Close();
            //                Debug.Fail("This should never happen !!!");
            //                return;
            //            }
            //            timeOffset = State.Audio.ConvertBytesToMilliseconds(byteOffset - bytesLeft);
            //        }
            //        else
            //        {
            //            Debug.Fail("audio chunk not found ??");
            //            return;
            //        }
            //    }


            //    if (!managedAudioMedia.HasActualAudioMediaData)
            //    {
            //        Debug.Fail("This should never happen !!!");
            //        //recordingStream.Close();
            //        return;
            //    }



            //    //managedAudioMedia.AudioMediaData.InsertAudioData(recordingStream, new Time(timeOffset), new Time(recordingDuration));
            //    //recordingStream.Close();
            //}
            //else if (audioMedia is SequenceMedia)
            //{
            //    Debug.Fail("SequenceMedia is normally removed at import time...have you tried re-importing the DAISY book ?");

            //    var seqManAudioMedia = (SequenceMedia)audioMedia;

            //    var byteOffset = State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime);

            //    double timeOffset = 0;
            //    long sumData = 0;
            //    long sumDataPrev = 0;
            //    foreach (Media media in seqManAudioMedia.ChildMedias.ContentsAs_YieldEnumerable)
            //    {
            //        var manangedMediaSeqItem = (ManagedAudioMedia)media;
            //        if (!manangedMediaSeqItem.HasActualAudioMediaData)
            //        {
            //            continue;
            //        }

            //        AudioMediaData audioData = manangedMediaSeqItem.AudioMediaData;
            //        sumData += audioData.PCMFormat.Data.ConvertTimeToBytes(audioData.AudioDuration.AsMilliseconds);
            //        if (byteOffset < sumData)
            //        {
            //            timeOffset = State.Audio.ConvertBytesToMilliseconds(byteOffset - sumDataPrev);

            //            if (AudioPlaybackStreamKeepAlive)
            //            {
            //                ensurePlaybackStreamIsDead();
            //            }

            //            if (!manangedMediaSeqItem.HasActualAudioMediaData)
            //            {
            //                Debug.Fail("This should never happen !!!");
            //                //recordingStream.Close();
            //                return;
            //            }

            //            var command = treeNode.Presentation.CommandFactory.
            //                CreateManagedAudioMediaInsertDataCommand(
            //                treeNode, manMedia,
            //                new Time(timeOffset),
            //                treeNodeSelection.Item1); //manangedMediaSeqItem

            //            treeNode.Presentation.UndoRedoManager.Execute(command);

            //            //manangedMediaSeqItem.AudioMediaData.InsertAudioData(recordingStream, new Time(timeOffset), new Time(recordingDuration));
            //            //recordingStream.Close();
            //            break;
            //        }
            //        sumDataPrev = sumData;
            //    }
            //}






            //SelectionBegin = (LastPlayHeadTime < 0 ? 0 : LastPlayHeadTime);
            //SelectionEnd = SelectionBegin + recordingManagedAudioMedia.Duration.AsMilliseconds;

            //ReloadWaveForm(); UndoRedoManager.Changed callback will take care of that.
        }
    }
}
