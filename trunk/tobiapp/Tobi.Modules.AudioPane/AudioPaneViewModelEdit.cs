using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using urakawa.command;
using urakawa.core;
using urakawa.daisy.import;
using urakawa.data;
using urakawa.media;
using urakawa.media.data.audio;
using urakawa.media.data.audio.codec;

namespace Tobi.Plugin.AudioPane
{
    public partial class AudioPaneViewModel
    {
        // non-public feature, unless you know the keyboard shortcut
        public RichDelegateCommand CommandOpenFile { get; private set; }

        public RichDelegateCommand CommandInsertFile { get; private set; }
        public RichDelegateCommand CommandGenTTS { get; private set; }
        public RichDelegateCommand CommandDeleteAudioSelection { get; private set; }

        public RichDelegateCommand CommandSplitShift { get; private set; }

        public RichDelegateCommand CopyCommand { get; private set; }
        public RichDelegateCommand CutCommand { get; private set; }
        public RichDelegateCommand PasteCommand { get; private set; }

        public RichDelegateCommand CommandResetSessionCounter { get; private set; }

        public ManagedAudioMedia AudioClipboard { get; private set; }

        private void initializeCommands_Edit()
        {
            CommandResetSessionCounter = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioResetSessionCounter_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioResetSessionCounter_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("appointment-new"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandResetSessionCounter", Category.Debug, Priority.Medium);

                    TotalSessionAudioDurationInLocalUnits = 0;
                },
                () => true,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_ResetSessionCounter));

            m_ShellView.RegisterRichCommand(CommandResetSessionCounter);
            //
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
                    AudioMediaData mediaDataClipboard = listOfTreeNodeAndStreamSelection[0].m_TreeNode.Presentation.MediaDataFactory.CreateAudioMediaData();
                    managedAudioMediaClipboard.AudioMediaData = mediaDataClipboard;

                    foreach (var treeNodeAndStreamSelection in listOfTreeNodeAndStreamSelection)
                    {
                        ManagedAudioMedia manMedia = treeNodeAndStreamSelection.ExtractManagedAudioMedia();

                        // WARNING: WavAudioMediaData implementation differs from AudioMediaData:
                        // the latter is naive and performs a stream binary copy, the latter is optimized and re-uses existing WavClips. 
                        //  WARNING 2: The audio data from the given parameter gets emptied !
                        mediaDataClipboard.MergeWith(manMedia.AudioMediaData);

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

                    return CommandDeleteAudioSelection.CanExecute();
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
                    //Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

                    return CommandDeleteAudioSelection.CanExecute();
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
                    return AudioClipboard != null
                        && CommandInsertFile.CanExecute();
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
                    return !IsPlaying && CommandStartRecord.CanExecute();
                }
                //&& IsAudioLoaded
                      ,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_InsertFile));

            m_ShellView.RegisterRichCommand(CommandInsertFile);
            //
            //
            CommandGenTTS = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioGenTTS_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioGenTTS_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("audio-x-generic"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandGenTTS", Category.Debug, Priority.Medium);

                    CommandGenTTS_Execute();
                },
                () =>
                {
                    Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

                    bool okay = treeNodeSelection.Item1 != null
                        && (
                        IsSimpleMode
                        ? TreeNode.GetLengthStringChunks(treeNodeSelection.Item1.GetTextFlattened_()) > 0
                        : true
                            );

                    return okay
                       && !IsMonitoring && !IsRecording && !IsWaveFormLoading && !IsPlaying
                       && m_UrakawaSession.DocumentProject != null
                       && (IsSimpleMode || treeNodeSelection.Item1.GetXmlElementQName() != null)
                       && treeNodeSelection.Item1.GetFirstAncestorWithManagedAudio() == null
                        //&& treeNodeSelection.Item1.GetFirstDescendantWithManagedAudio() == null
                       ;

                    //// because we change the selection in the execute code
                    ////return CommandInsertFile.CanExecute();

                    // Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
                    // TreeNode node = treeNodeSelection.Item2 ?? treeNodeSelection.Item1;

                    // return !IsMonitoring && !IsRecording && !IsWaveFormLoading && !IsPlaying
                    //     && m_UrakawaSession.DocumentProject != null
                    //&& node != null
                    //&& node.GetXmlElementQName() != null
                    //&& node.GetFirstAncestorWithManagedAudio() == null
                    //&& node.GetFirstDescendantWithManagedAudio() == null;
                }
                      ,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_GenTTS));

            m_ShellView.RegisterRichCommand(CommandGenTTS);
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

                    //if (listOfTreeNodeAndStreamSelection.Count == 1)
                    //{
                    //    var command = m_UrakawaSession.DocumentProject.Presentations.Get(0).CommandFactory.
                    //                CreateTreeNodeAudioStreamDeleteCommand(listOfTreeNodeAndStreamSelection[0], treeNodeSelection.Item1);

                    //    m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.Execute(command);
                    //}
                    //else
                    //{
                    /// NOTE: WE NEED TO USE A WRAPPING TRANSACTION, EVEN IF COUNT == 1 ! (otherwise a super-transaction may prevent the processing of undo/redo refresh here in our client code)
                    m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction(Tobi_Plugin_AudioPane_Lang.TransactionDeleteAudio_ShortDesc, Tobi_Plugin_AudioPane_Lang.TransactionDeleteAudio_LongDesc);

                    foreach (TreeNodeAndStreamSelection selection in listOfTreeNodeAndStreamSelection)
                    {
                        var command = m_UrakawaSession.DocumentProject.Presentations.Get(0).CommandFactory.
                                    CreateTreeNodeAudioStreamDeleteCommand(selection, treeNodeSelection.Item1);

                        m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.Execute(command);
                    }

                    m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();
                    //}
                },
                () =>
                {
                    return !IsWaveFormLoading && !IsPlaying && !IsMonitoring && !IsRecording
                         && State.Audio.PlayStreamMarkers != null && IsSelectionSet && State.Audio.HasContent;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_Delete));

            m_ShellView.RegisterRichCommand(CommandDeleteAudioSelection);
            //
            CommandSplitShift = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdSplit_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdSplit_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("applications-accessories"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandSplitShift", Category.Debug, Priority.Medium);

                    bool isAutoPlay = IsAutoPlay;

                    OnEscape(null); //  CHANGES IsAutoPlay to FALSE
                    //CommandPause.Execute();

                    TreeNode next = null;
                    {
                        Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
                        TreeNode treeNode = treeNodeSelection.Item2 ?? treeNodeSelection.Item1;

                    tryNext:
                        next = treeNode.GetNextSiblingWithText();
                        while (next != null && (next.GetXmlElementQName() == null
                                                || TreeNode.TextOnlyContainsPunctuation(next.GetText())
                                               ))
                        {
                            next = next.GetNextSiblingWithText();
                        }
                        next =
                            TreeNode.EnsureTreeNodeHasNoSignificantTextOnlySiblings(
                                m_UrakawaSession.DocumentProject.Presentations.Get(0).RootNode, next);

                        if (next != null)
                        {
                            if (Settings.Default.Audio_EnableSkippability && isTreeNodeSkippable(next))
                            {
                                treeNode = next;
                                goto tryNext;
                            }
                        }
                    }

                    if (next == null)
                    {
                        AudioCues.PlayBeep();
                        return;
                    }

                    CommandClearSelection.Execute();
                    CommandSelectRight.Execute();
                    CopyCommand.Execute();

                    m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction(Tobi_Plugin_AudioPane_Lang.TransactionSplitAudio_ShortDesc, Tobi_Plugin_AudioPane_Lang.TransactionSplitAudio_LongDesc);

                    CommandDeleteAudioSelection.Execute();
                    //CutCommand.Execute();
                    OnEscape(null);

                    m_UrakawaSession.PerformTreeNodeSelection(next);
                    OnEscape(null);

                    Tuple<TreeNode, TreeNode> treeNodeSelectionNew = m_UrakawaSession.GetTreeNodeSelection();
                    TreeNode treeNodeNew = treeNodeSelectionNew.Item2 ?? treeNodeSelectionNew.Item1;

//#if DEBUG
//                    DebugFix.Assert(treeNodeNew == next);
//#endif //DEBUG

                    if (treeNodeNew.GetManagedAudioMedia() != null
                                || treeNodeNew.GetFirstDescendantWithManagedAudio() != null)
                    {
                        //TODO: special treatment for node already containing audio?
                    }

                    PasteCommand.Execute();

                    m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();

                    // pasted audio should be selected at this point, so playback only this audio selection
                    if (isAutoPlay)
                    {
                        OnEscape(null);
                        IsAutoPlay = true;
                        CommandPlay.Execute();
                    }
                },
                () =>
                {
                    return !IsWaveFormLoading && !IsMonitoring && !IsRecording
                         && State.Audio.PlayStreamMarkers != null && State.Audio.HasContent;
                    //IsSelectionSet   !IsPlaying
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_Split));

            m_ShellView.RegisterRichCommand(CommandSplitShift);
            //
        }

        private bool canDeleteInsertReplaceAudio()
        {
            Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode node = treeNodeSelection.Item2 ?? treeNodeSelection.Item1;

            return CommandDeleteAudioSelection.CanExecute()
                ||
                m_UrakawaSession.DocumentProject != null
                   && node != null
                   && (IsSimpleMode || node.GetXmlElementQName() != null)
                   && node.GetFirstAncestorWithManagedAudio() == null
                   && node.GetFirstDescendantWithManagedAudio() == null
                //&& node.GetManagedAudioMedia() == null
                   ;
        }


        internal void openFile(String str, bool insert, bool deleteAfterInsert, PCMFormatInfo pcmInfo)
        {
            Logger.Log("AudioPaneViewModel.OpenFile", Category.Debug, Priority.Medium);

            CommandPause.Execute();

            string filePath = str;

            if (String.IsNullOrEmpty(filePath) && View != null)
            {
                filePath = View.OpenFileDialog();
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }

            string ext = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(ext))
            {
                return;
            }

            bool isWav = String.Equals(ext, DataProviderFactory.AUDIO_WAV_EXTENSION, StringComparison.OrdinalIgnoreCase);

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

                DebugFix.Assert(m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.EnforceSinglePCMFormat);

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

                    //filePath = m_AudioFormatConvertorSession.ConvertAudioFileFormat(filePath);

                    bool cancelled = false;

                    var converter = new AudioClipConverter(m_AudioFormatConvertorSession, filePath);

                    bool result = m_ShellView.RunModalCancellableProgressTask(true,
                        Tobi_Plugin_AudioPane_Lang.ProcessingAudioClip,
                        converter,
                        () =>
                        {
                            Logger.Log(@"Audio conversion CANCELLED", Category.Debug, Priority.Medium);
                            cancelled = true;
                        },
                        () =>
                        {
                            Logger.Log(@"Audio conversion DONE", Category.Debug, Priority.Medium);
                            cancelled = false;
                        });

                    if (cancelled)
                    {
                        DebugFix.Assert(!result);
                        return;
                    }

                    filePath = converter.ConvertedFilePath;
                    if (string.IsNullOrEmpty(filePath))
                    {
                        return;
                    }

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

                if (managedAudioMedia.Duration.AsTimeSpan.TotalMilliseconds > 500)
                    insertAudioAtCursorOrSelectionReplace(managedAudioMedia);
                else
                    Console.WriteLine(@"audio clip too short to be inserted ! " + managedAudioMedia.Duration.AsLocalUnits / AudioLibPCMFormat.TIME_UNIT + @"ms");

                return;
            }

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
            }

            if (!isWav)
            {
                string originalFilePath = filePath;

                //filePath = m_AudioFormatConvertorSession_NoProject.ConvertAudioFileFormat(filePath);

                bool cancelled = false;

                var converter = new AudioClipConverter(m_AudioFormatConvertorSession_NoProject, filePath);

                bool result = m_ShellView.RunModalCancellableProgressTask(true,
                    Tobi_Plugin_AudioPane_Lang.ProcessingAudioClip,
                    converter,
                    () =>
                    {
                        Logger.Log(@"Audio conversion CANCELLED", Category.Debug, Priority.Medium);
                        cancelled = true;
                    },
                    () =>
                    {
                        Logger.Log(@"Audio conversion DONE", Category.Debug, Priority.Medium);
                        cancelled = false;
                    });

                if (cancelled)
                {
                    DebugFix.Assert(!result);
                    return;
                }

                filePath = converter.ConvertedFilePath;
                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

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
                if (AudioPlaybackStreamKeepAlive)
                {
                    ensurePlaybackStreamIsDead();
                }

                //DebugFix.Assert(State.Audio.PlayStream == null);
                var media = treeNode.GetManagedAudioMediaOrSequenceMedia();
                if (media != null)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    return;
                }
                DebugFix.Assert(treeNode.GetFirstDescendantWithManagedAudio() == null);
                DebugFix.Assert(treeNode.GetFirstAncestorWithManagedAudio() == null);

                var command_ = treeNode.Presentation.CommandFactory.CreateTreeNodeSetManagedAudioMediaCommand(treeNode, manMedia, treeNodeSelection.Item1);
                treeNode.Presentation.UndoRedoManager.Execute(command_);

                return;
            }

            bool transaction = false;
            List<TreeNodeAndStreamSelection> selData = null;

            long bytePositionInsert = 0;

            if (IsSelectionSet)
            {
                bytePositionInsert = State.Selection.SelectionBeginBytePosition;

                transaction = true;
                treeNode.Presentation.UndoRedoManager.StartTransaction(Tobi_Plugin_AudioPane_Lang.TransactionReplaceAudio_ShortDesc, Tobi_Plugin_AudioPane_Lang.TransactionReplaceAudio_LongDesc);

                selData = getAudioSelectionData();

                CommandDeleteAudioSelection.Execute();

                bool allWasDeleted = false;
                if (selData != null)
                {
                    bool atLeastOneWasNotDeleted = false;
                    foreach (var treeNodeAndStreamSelection in selData)
                    {
                        bool deleted = treeNodeAndStreamSelection.m_TreeNode.GetManagedAudioMediaOrSequenceMedia() == null;
                        if (!deleted)
                        {
                            atLeastOneWasNotDeleted = true;
                            break;
                        }
                    }
                    if (!atLeastOneWasNotDeleted)
                    {
                        allWasDeleted = true;
                    }
                }
                else
                {
#if DEBUG
                    Debugger.Break();
#endif
                }

                if (allWasDeleted) //!State.Audio.HasContent)
                {
                    if (selData != null && selData.Count > 0)
                    {
                        TreeNode treeNode_ = selData[0].m_TreeNode;
                        var command_ =
                            treeNode.Presentation.CommandFactory.CreateTreeNodeSetManagedAudioMediaCommand(treeNode_, manMedia, treeNodeSelection.Item1);
                        treeNode.Presentation.UndoRedoManager.Execute(command_);
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }

                    treeNode.Presentation.UndoRedoManager.EndTransaction();
                    return;
                }


                if (AudioPlaybackStreamKeepAlive)
                {
                    ensurePlaybackStreamIsDead();
                }
                m_CurrentAudioStreamProvider = m_AudioStreamProvider_TreeNode;
                m_CurrentAudioStreamProvider();

                //CommandRefresh.Execute();
                m_LastSetPlayBytePosition = State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(bytePositionInsert);
            }
            else
            {
                bytePositionInsert = PlayBytePosition;

                if (bytePositionInsert < 0)
                {
                    if (AudioPlaybackStreamKeepAlive)
                    {
                        ensurePlaybackStreamIsDead();
                    }

                    var media = treeNode.GetManagedAudioMediaOrSequenceMedia();
                    if (media != null)
                    {
#if DEBUG
                        Debugger.Break();
#endif
                        return;
                    }
                    DebugFix.Assert(treeNode.GetFirstDescendantWithManagedAudio() == null);
                    DebugFix.Assert(treeNode.GetFirstAncestorWithManagedAudio() == null);

                    var command_ = treeNode.Presentation.CommandFactory.CreateTreeNodeSetManagedAudioMediaCommand(treeNode, manMedia, treeNodeSelection.Item1);
                    treeNode.Presentation.UndoRedoManager.Execute(command_);

                    return;
                }
            }

            TreeNode treeNodeTarget;
            long bytesRight;
            long bytesLeft;
            int index;
            bool match = State.Audio.FindInPlayStreamMarkers(bytePositionInsert, out treeNodeTarget, out index, out bytesLeft, out bytesRight);
            if (!match)
            {
#if DEBUG
                Debugger.Break();
#endif
                if (transaction)
                {
                    treeNode.Presentation.UndoRedoManager.EndTransaction();
                }
                return;
            }

            if (selData != null && selData.Count > 0)
            {
            }

            Media treeNodeAudio = treeNodeTarget.GetManagedAudioMedia();

            if (treeNodeAudio == null)
            {
#if DEBUG
                Debugger.Break();
#endif
                return;
            }

            Command command = treeNode.Presentation.CommandFactory.
                   CreateManagedAudioMediaInsertDataCommand(
                       treeNodeTarget, manMedia,
                       bytePositionInsert - bytesLeft,
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
