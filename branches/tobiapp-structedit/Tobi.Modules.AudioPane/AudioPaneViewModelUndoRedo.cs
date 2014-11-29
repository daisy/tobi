using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;
using AudioLib;
using Tobi.Common;
using urakawa.command;
using urakawa.commands;
using urakawa.core;
using urakawa.events.undo;
using urakawa.media.data.audio;
using urakawa.media.timing;

namespace Tobi.Plugin.AudioPane
{
    public partial class AudioPaneViewModel
    {
        private void updateTotalDuration(Command cmd, bool done)
        {
            if (cmd is ManagedAudioMediaInsertDataCommand)
            {
                var command = (ManagedAudioMediaInsertDataCommand)cmd;

                long dur = command.ManagedAudioMediaSource.Duration.AsLocalUnits;
                if (done)
                {
                    TotalDocumentAudioDurationInLocalUnits += dur;
                    TotalSessionAudioDurationInLocalUnits += dur;
                }
                else
                {
                    TotalDocumentAudioDurationInLocalUnits -= dur;
                    TotalSessionAudioDurationInLocalUnits -= dur;
                }
            }
            else if (cmd is TreeNodeSetManagedAudioMediaCommand)
            {
                var command = (TreeNodeSetManagedAudioMediaCommand)cmd;

                long dur = command.ManagedAudioMedia.Duration.AsLocalUnits;
                if (done)
                {
                    TotalDocumentAudioDurationInLocalUnits += dur;
                    TotalSessionAudioDurationInLocalUnits += dur;
                }
                else
                {
                    TotalDocumentAudioDurationInLocalUnits -= dur;
                    TotalSessionAudioDurationInLocalUnits -= dur;
                }
            }
            else if (cmd is TreeNodeAudioStreamDeleteCommand)
            {
                var command = (TreeNodeAudioStreamDeleteCommand)cmd;

                Time timeBegin = command.SelectionData.m_LocalStreamLeftMark == -1
                    ? Time.Zero
                    : new Time(command.OriginalManagedAudioMedia.AudioMediaData.PCMFormat.Data.ConvertBytesToTime(command.SelectionData.m_LocalStreamLeftMark));

                Time timeEnd = command.SelectionData.m_LocalStreamRightMark == -1
                    ? command.OriginalManagedAudioMedia.AudioMediaData.AudioDuration
                    : new Time(command.OriginalManagedAudioMedia.AudioMediaData.PCMFormat.Data.ConvertBytesToTime(command.SelectionData.m_LocalStreamRightMark));

                Time diff = timeEnd.GetDifference(timeBegin);

                ManagedAudioMedia manMedia = command.TreeNode.GetManagedAudioMedia();
                if (manMedia == null)
                {
                    DebugFix.Assert(done);

                    DebugFix.Assert(diff.IsEqualTo(command.OriginalManagedAudioMedia.Duration));

                    DebugFix.Assert(
                        //command.OriginalManagedAudioMedia.AudioMediaData.PCMFormat.Data.
                            AudioLibPCMFormat.TimesAreEqualWithMillisecondsTolerance(
                            diff.AsLocalUnits,
                            command.OriginalManagedAudioMedia.Duration.AsLocalUnits));
                }

                long dur = diff.AsLocalUnits;
                if (done)
                {
                    TotalDocumentAudioDurationInLocalUnits -= dur;
                    TotalSessionAudioDurationInLocalUnits -= dur;
                }
                else
                {
                    TotalDocumentAudioDurationInLocalUnits += dur;
                    TotalSessionAudioDurationInLocalUnits += dur;
                }
            }
            else if (cmd is TreeNodeRemoveCommand)
            {
                var command = (TreeNodeRemoveCommand)cmd;

                Time duration = command.TreeNode.GetDurationOfManagedAudioMediaFlattened();
                long dur = duration == null ? 0 : duration.AsLocalUnits;
                if (!done)
                {
                    TotalDocumentAudioDurationInLocalUnits += dur;
                    TotalSessionAudioDurationInLocalUnits += dur;
                }
                else
                {
                    TotalDocumentAudioDurationInLocalUnits -= dur;
                    TotalSessionAudioDurationInLocalUnits -= dur;
                }
            }
            else if (cmd is TreeNodeInsertCommand)
            {
                var command = (TreeNodeInsertCommand)cmd;

                Time duration = command.TreeNode.GetDurationOfManagedAudioMediaFlattened();
                long dur = duration == null ? 0 : duration.AsLocalUnits;
                if (done)
                {
                    TotalDocumentAudioDurationInLocalUnits += dur;
                    TotalSessionAudioDurationInLocalUnits += dur;
                }
                else
                {
                    TotalDocumentAudioDurationInLocalUnits -= dur;
                    TotalSessionAudioDurationInLocalUnits -= dur;
                }
            }
        }


        private long getByteOffset(TreeNode treeNode, ManagedAudioMedia managedMedia)
        {
            //if (!State.IsTreeNodeShownInAudioWaveForm(treeNode))
            //{
            //    return 0;
            //}

            long byteOffset = 0;

            //Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

            if (State.Audio.PlayStreamMarkers != null)
            {
                long bytesRight;
                long bytesLeft;
                int index;
                bool match = State.Audio.FindInPlayStreamMarkers(treeNode, out index, out bytesLeft, out bytesRight);

                if (match)
                {
                    byteOffset = bytesLeft;
                }
                else
                {
                    return 0;
                }
            }

            if (managedMedia == null)
            {
                return byteOffset;
            }

#if ENABLE_SEQ_MEDIA

            SequenceMedia seqManAudioMedia = treeNode.GetManagedAudioSequenceMedia();
            if (seqManAudioMedia != null)
            {
                Debug.Fail("SequenceMedia is normally removed at import time...have you tried re-importing the DAISY book ?");

                foreach (Media media in seqManAudioMedia.ChildMedias.ContentsAs_Enumerable)
                {
                    var manMedia = (ManagedAudioMedia)media;
                    if (media == managedMedia)
                    {
                        break;
                    }
                    byteOffset += manMedia.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(manMedia.Duration.AsLocalUnits);
                }
            }
            
#endif //ENABLE_SEQ_MEDIA

            return byteOffset;
        }

        private long computeByteOffset(TreeNode targetNode1, TreeNode targetNode2)
        {
            long byteOffset = 0;

            if (State.Audio.PlayStreamMarkers != null && targetNode2 != null)
            {
                ManagedAudioMedia mediaInPlayMarkers = targetNode2.GetManagedAudioMedia();

                if (mediaInPlayMarkers == null)
                {
                    TreeNode prev = targetNode2.GetPreviousSiblingWithManagedAudio();
                    if (prev != null && prev.IsDescendantOf(targetNode1))
                    {
                        ManagedAudioMedia prevAudio = prev.GetManagedAudioMedia();
                        DebugFix.Assert(prevAudio != null);

                        byteOffset = getByteOffset(prev, prevAudio);

                        if (prevAudio != null)
                        {
                            byteOffset += prevAudio.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(prevAudio.AudioMediaData.AudioDuration.AsLocalUnits);
                        }
                    }
                }
                else
                {
                    byteOffset = getByteOffset(targetNode2, mediaInPlayMarkers);
                }
            }

            return byteOffset;
        }

        private void UndoRedoManagerChanged_RestoreAudioTreeNodeSelectionState(TreeNode targetNode1, TreeNode targetNode2, long byteStart, long byteDur, bool done)
        {
            if (targetNode1 == null || targetNode2 != null && !targetNode2.IsDescendantOf(targetNode1))
            {
#if DEBUG
                Debugger.Break();
#endif
                return;
            }

            m_LastSetPlayBytePosition = -1;

            //AudioPlayer_UpdateWaveFormPlayHead();
            if (View != null)
            {
                View.RefreshUI_WaveFormPlayHead(true);
            }

            //RefreshWaveFormChunkMarkersForCurrentSubTreeNode(false);


            long byteOffset = 0;

            Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

            bool waveFormNeedsForcedRefresh = treeNodeSelection.Item1 == targetNode1;
            if (waveFormNeedsForcedRefresh)
            {
                if (View != null)
                {
                    View.ResetAll();
                }

                if (AudioPlaybackStreamKeepAlive)
                {
                    ensurePlaybackStreamIsDead();
                }
                m_CurrentAudioStreamProvider();

                byteOffset = computeByteOffset(targetNode1, targetNode2);

                treeNodeSelection = m_UrakawaSession.PerformTreeNodeSelection(targetNode1, false, targetNode2);
            }
            else
            {
                m_StateToRestore = new StateToRestore
                {
                    SelectionBeginBytePosition = -1,
                    SelectionEndBytePosition = -1,
                    PlayHeadBytePosition = -1
                };

                treeNodeSelection = m_UrakawaSession.PerformTreeNodeSelection(targetNode1, false, targetNode2);

                if (View != null)
                {
                    View.CancelWaveFormLoad(true);
                }

                if (treeNodeSelection.Item1 == targetNode1)
                {
                    byteOffset = computeByteOffset(targetNode1, targetNode2);
                    //treeNodeSelection.Item2 == targetNode2
                }
            }

            if (treeNodeSelection.Item1 != targetNode1)
            {
#if DEBUG
                Debugger.Break();
#endif
                m_StateToRestore = new StateToRestore
                {
                    SelectionBeginBytePosition = -1,
                    SelectionEndBytePosition = -1,
                    PlayHeadBytePosition = 0
                };

                CommandRefresh.Execute();

                return;
            }

            if (treeNodeSelection.Item2 != targetNode2)
            {
#if DEBUG
                Debugger.Break();
#endif
            }

            long begin = byteOffset + byteStart;
            if (done)
            {
                m_StateToRestore = new StateToRestore
                {
                    SelectionBeginBytePosition = begin,
                    SelectionEndBytePosition = begin + byteDur,
                    PlayHeadBytePosition = begin
                };
            }
            else
            {
                m_StateToRestore = new StateToRestore
                {
                    SelectionBeginBytePosition = -1,
                    SelectionEndBytePosition = -1,
                    PlayHeadBytePosition = begin
                };
            }

            CommandRefresh.Execute();
        }

        private void OnUndoRedoManagerChanged_AudioEditCommand(UndoRedoManagerEventArgs eventt, bool done, AudioEditCommand cmd, bool isTransactionEndEvent, bool isNoTransactionOrTrailingEdge)
        {
            DebugFix.Assert(!isTransactionEndEvent);

            if (cmd is ManagedAudioMediaInsertDataCommand)
            {
                var command = (ManagedAudioMediaInsertDataCommand)cmd;

                if (!done)
                {
                    DebugFix.Assert(!m_OnUndoRedoManagerChanged_wasInitByRemove);
                }

                if (!done // reverse => force scan backwards to beginning
                    || m_OnUndoRedoManagerChanged_targetNode1 == null // not reverse, first-time init
                    || m_OnUndoRedoManagerChanged_wasInitByRemove // not reverse, not first-time init, but deletion occured before addition (e.g. select waveform audio + paste over it)
                )
                {
                    m_OnUndoRedoManagerChanged_wasInitByRemove = false;
                    m_OnUndoRedoManagerChanged_wasInitByAdd = true;

                    m_OnUndoRedoManagerChanged_targetNode1 = command.CurrentTreeNode;
                    m_OnUndoRedoManagerChanged_targetNode2 = command.CurrentTreeNode == command.TreeNode ? null : command.TreeNode;

                    m_OnUndoRedoManagerChanged_byteStart = command.BytePositionInsert;

                    if (done || m_OnUndoRedoManagerChanged_targetNode1 == null)
                        m_OnUndoRedoManagerChanged_byteDur = 0;

                    m_OnUndoRedoManagerChanged_done = done;
                }

                m_OnUndoRedoManagerChanged_byteDur += command.ManagedAudioMediaSource.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(
                    command.ManagedAudioMediaSource.Duration.AsLocalUnits);
            }
            else if (cmd is TreeNodeSetManagedAudioMediaCommand)
            {
                var command = (TreeNodeSetManagedAudioMediaCommand)cmd;

                if (!done)
                {
                    DebugFix.Assert(!m_OnUndoRedoManagerChanged_wasInitByRemove);
                }

                if (!done // reverse => force scan backwards to beginning
                    || m_OnUndoRedoManagerChanged_targetNode1 == null // not reverse, first-time init
                    || m_OnUndoRedoManagerChanged_wasInitByRemove // not reverse, not first-time init, but deletion occured before addition (e.g. select waveform audio + paste over it)
                )
                {
                    m_OnUndoRedoManagerChanged_wasInitByRemove = false;
                    m_OnUndoRedoManagerChanged_wasInitByAdd = true;

                    m_OnUndoRedoManagerChanged_targetNode1 = command.CurrentTreeNode;
                    m_OnUndoRedoManagerChanged_targetNode2 = command.CurrentTreeNode == command.TreeNode ? null : command.TreeNode;

                    m_OnUndoRedoManagerChanged_byteStart = 0;

                    if (done || m_OnUndoRedoManagerChanged_targetNode1 == null)
                        m_OnUndoRedoManagerChanged_byteDur = 0;

                    m_OnUndoRedoManagerChanged_done = done;
                }

                m_OnUndoRedoManagerChanged_byteDur += command.ManagedAudioMedia.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(
                    command.ManagedAudioMedia.Duration.AsLocalUnits);
            }
            else if (cmd is TreeNodeAudioStreamDeleteCommand)
            {
                var command = (TreeNodeAudioStreamDeleteCommand)cmd;

                if (done)
                {
                    DebugFix.Assert(!m_OnUndoRedoManagerChanged_wasInitByAdd);
                }

                if (!done // reverse => force scan backwards to beginning
                    || m_OnUndoRedoManagerChanged_targetNode1 == null // not reverse, first-time init
                )
                {
                    if (done || m_OnUndoRedoManagerChanged_targetNode1 == null || m_OnUndoRedoManagerChanged_wasInitByAdd)
                        m_OnUndoRedoManagerChanged_byteDur = 0;

                    m_OnUndoRedoManagerChanged_wasInitByAdd = false;
                    m_OnUndoRedoManagerChanged_wasInitByRemove = true;

                    m_OnUndoRedoManagerChanged_targetNode1 = command.CurrentTreeNode;
                    m_OnUndoRedoManagerChanged_targetNode2 = command.CurrentTreeNode == command.TreeNode
                        ? null
                        : command.TreeNode;

                    m_OnUndoRedoManagerChanged_byteStart = 0;

                    m_OnUndoRedoManagerChanged_done = !done;
                }

                long bytesBegin = 0;
                long bytesEnd = 0;

                if (command.SelectionData.m_LocalStreamLeftMark > 0)
                {
                    bytesBegin = command.SelectionData.m_LocalStreamLeftMark;
                }

                if (command.SelectionData.m_LocalStreamRightMark <= 0)
                {
                    bytesEnd = command.OriginalManagedAudioMedia.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(command.OriginalManagedAudioMedia.AudioMediaData.AudioDuration.AsLocalUnits);
                }
                else
                {
                    bytesEnd = command.SelectionData.m_LocalStreamRightMark;
                }

                m_OnUndoRedoManagerChanged_byteStart += bytesBegin;
                m_OnUndoRedoManagerChanged_byteDur += (bytesEnd - bytesBegin);
            }
        }

        private TreeNode m_OnUndoRedoManagerChanged_targetNode1 = null;
        private TreeNode m_OnUndoRedoManagerChanged_targetNode2 = null;
        private long m_OnUndoRedoManagerChanged_byteStart = -1;
        private long m_OnUndoRedoManagerChanged_byteDur = 0;
        private bool m_OnUndoRedoManagerChanged_done = false;
        private bool m_OnUndoRedoManagerChanged_wasInitByRemove = false;
        private bool m_OnUndoRedoManagerChanged_wasInitByAdd = false;

        public void OnUndoRedoManagerChanged(UndoRedoManagerEventArgs eventt, bool done, Command command, bool isTransactionEndEvent, bool isNoTransactionOrTrailingEdge)
        {
            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.Invoke(DispatcherPriority.Normal, (Action<UndoRedoManagerEventArgs, bool, Command, bool, bool>)OnUndoRedoManagerChanged, eventt, done, command, isTransactionEndEvent, isNoTransactionOrTrailingEdge);
                return;
            }

            if (m_TTSGen)
            {
                return;
            }

            //if (isTransactionEndEvent)
            //{
            //    return;
            //}

            if (isNoTransactionOrTrailingEdge)
            {
                if (EventAggregator != null)
                {
                    EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.Ready);
                }
            }

            if (command is CompositeCommand)
            {
#if DEBUG
                Debugger.Break();
#endif
            }
            else if (command is TreeNodeChangeTextCommand)
            {
                if (!isTransactionEndEvent)
                {
                    var cmd = (TreeNodeChangeTextCommand)command;

                    Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                    if (selection.Item1 != null)
                    {
                        if (cmd.TreeNode == selection.Item1
                            || (cmd.TreeNode.IsDescendantOf(selection.Item1)))
                        {
                            if (State.Audio.HasContent && State.Audio.PlayStreamMarkers != null)
                            {
                                if (true
                                    //State.Audio.PlayStreamMarkers.Count <= Settings.Default.AudioWaveForm_TextCacheRenderThreshold
                                    )
                                {
                                    if (View != null)
                                    {
                                        View.InvalidateWaveFormOverlay();
                                    }
                                }
                                else
                                {
                                    CommandRefresh.Execute();
                                }
                            }
                        }
                    }
                }

                return;
            }

            if (!(command is TextNodeStructureEditCommand) && !(command is AudioEditCommand))
            {
                return;
            }

            if (!isTransactionEndEvent)
            {
                if (View != null)
                {
                    View.CancelWaveFormLoad(false);
                }
                InterruptAudioPlayerRecorder();

                if (View != null)
                {
                    View.CancelWaveFormLoad(true);
                }
                CommandPause.Execute();


                updateTotalDuration(command, done);
            }

            if (command is TextNodeStructureEditCommand)
            {
                if (isNoTransactionOrTrailingEdge)
                {
                    // TODO: this is currently brute-force => refresh waveform correctly, depending on modified tree fragment (remove / insert)

                    if (View != null)
                    {
                        View.ResetAll();
                    }

                    if (AudioPlaybackStreamKeepAlive)
                    {
                        ensurePlaybackStreamIsDead();
                    }

                    State.ResetAll();

                    m_LastSetPlayBytePosition = -1;

                    m_StateToRestore = null;

                    //if (View != null)
                    //{
                    //    View.InvalidateWaveFormOverlay();
                    //}

                    //CommandRefresh.Execute();

                    var sel = m_UrakawaSession.GetTreeNodeSelection();
                    //m_UrakawaSession.PerformTreeNodeSelection(sel.Item1, sel.Item2)
                    var selOld = new Tuple<TreeNode, TreeNode>(null, null);
                    OnTreeNodeSelectionChanged(new Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>>(selOld, sel));
                }

                return;
            }
            else if (command is AudioEditCommand)
            {
                if (!isTransactionEndEvent)
                {
                    OnUndoRedoManagerChanged_AudioEditCommand(eventt, done, (AudioEditCommand) command, isTransactionEndEvent, isNoTransactionOrTrailingEdge);
                }

                if (isNoTransactionOrTrailingEdge)
                {
                    if (m_OnUndoRedoManagerChanged_targetNode1 != null && m_OnUndoRedoManagerChanged_byteStart >= 0 && (!m_OnUndoRedoManagerChanged_done || m_OnUndoRedoManagerChanged_byteDur > 0))
                    {
                        UndoRedoManagerChanged_RestoreAudioTreeNodeSelectionState(m_OnUndoRedoManagerChanged_targetNode1, m_OnUndoRedoManagerChanged_targetNode2, m_OnUndoRedoManagerChanged_byteStart, m_OnUndoRedoManagerChanged_byteDur, m_OnUndoRedoManagerChanged_done);

                        if (command.IsInTransaction() && command.TopTransactionId() == AudioPaneViewModel.COMMAND_TRANSATION_ID__AUDIO_SPLIT_SHIFT)
                        {
                            CommandClearSelection.Execute();
                        }

                        m_OnUndoRedoManagerChanged_targetNode1 = null;
                        m_OnUndoRedoManagerChanged_targetNode2 = null;
                        m_OnUndoRedoManagerChanged_byteStart = -1;
                        m_OnUndoRedoManagerChanged_byteDur = 0;
                        m_OnUndoRedoManagerChanged_done = false;
                        m_OnUndoRedoManagerChanged_wasInitByRemove = false;
                        m_OnUndoRedoManagerChanged_wasInitByAdd = false;
                    }
                    else
                    {
#if DEBUG
                        Debugger.Break();
#endif
                    }
                }

                return;
            }


#if DEBUG
            Debugger.Break();
#endif

            if (View != null)
            {
                View.ResetAll();
            }

            m_LastSetPlayBytePosition = 0;

            m_StateToRestore = null;

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
            }

            CommandRefresh.Execute();
        }
    }
}
