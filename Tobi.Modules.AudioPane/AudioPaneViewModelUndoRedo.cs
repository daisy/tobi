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

        //private void HandleInsertDelete(TreeNode currentTreeNode, TreeNode treeNode,
        //    long byteInsert, long selectionBytes,
        //    ManagedAudioMedia managedAudioMediaTarget,
        //    bool done)
        //{
        //    if (View != null)
        //    {
        //        View.ResetAll();
        //    }

        //    Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
        //    Tuple<TreeNode, TreeNode> treeNodeSelectionAfter = treeNodeSelection;

        //    m_StateToRestore = null;

        //    if (treeNodeSelection.Item1 != currentTreeNode
        //        && treeNodeSelection.Item2 != currentTreeNode)
        //    {
        //        //StateToRestore? state = new StateToRestore
        //        //{
        //        //    SelectionBegin = m_StateToRestore.Value.SelectionBegin,
        //        //    SelectionEnd = m_StateToRestore.Value.SelectionEnd,
        //        //    LastPlayHeadTime = m_StateToRestore.Value.LastPlayHeadTime
        //        //};

        //        //State.CurrentTreeNode = null;

        //        //Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);
        //        //EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(currentTreeNode);
        //        m_UrakawaSession.PerformTreeNodeSelection(currentTreeNode);

        //        treeNodeSelectionAfter = m_UrakawaSession.GetTreeNodeSelection();
        //        if (treeNodeSelectionAfter.Item1 != currentTreeNode)
        //        {
        //            if (AudioPlaybackStreamKeepAlive)
        //            {
        //                ensurePlaybackStreamIsDead();
        //            }
        //            m_CurrentAudioStreamProvider();

        //            CommandRefresh.Execute();
        //        }

        //        //m_StateToRestore = state;

        //        //if (AudioPlaybackStreamKeepAlive)
        //        //{
        //        //    ensurePlaybackStreamIsDead();
        //        //}

        //        //State.ResetAll();
        //        //State.CurrentTreeNode = commandCurrentTreeNode;
        //        //m_CurrentAudioStreamProvider = m_AudioStreamProvider_TreeNode;

        //        //m_CurrentAudioStreamProvider();
        //    }
        //    else
        //    {
        //        if (AudioPlaybackStreamKeepAlive)
        //        {
        //            ensurePlaybackStreamIsDead();
        //        }
        //        m_CurrentAudioStreamProvider();
        //    }

        //    //bool isTreeNodeInAudioWaveForm = State.IsTreeNodeShownInAudioWaveForm(treeNode);

        //    //double timeOffset = isTreeNodeInAudioWaveForm && managedAudioMediaTarget != null
        //    //                        ? getTimeOffset(treeNode, managedAudioMediaTarget)
        //    //                        : 0;


        //    long byteOffset = 0;
        //    if (managedAudioMediaTarget == null)
        //    {
        //        TreeNode prev = treeNode.GetPreviousSiblingWithManagedAudio();
        //        if (prev != null && prev.IsDescendantOf(currentTreeNode))
        //        {
        //            byteOffset = getByteOffset(prev, managedAudioMediaTarget);
        //            ManagedAudioMedia prevAudio = prev.GetManagedAudioMedia();
        //            DebugFix.Assert(prevAudio != null);
        //            if (prevAudio != null)
        //            {
        //                byteOffset += prevAudio.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(prevAudio.AudioMediaData.AudioDuration.AsLocalUnits);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        byteOffset = getByteOffset(treeNode, managedAudioMediaTarget);
        //    }

        //    if (done)
        //    {
        //        long begin = byteOffset + byteInsert;
        //        m_StateToRestore = new StateToRestore
        //        {
        //            SelectionBeginBytePosition = begin,
        //            SelectionEndBytePosition = begin + selectionBytes,
        //            PlayHeadBytePosition = begin
        //        };
        //    }
        //    else
        //    {
        //        m_StateToRestore = new StateToRestore
        //        {
        //            SelectionBeginBytePosition = -1,
        //            SelectionEndBytePosition = -1,
        //            PlayHeadBytePosition = byteOffset + byteInsert
        //        };
        //    }

        //    if (treeNodeSelectionAfter.Item2 != treeNode
        //        && treeNode != currentTreeNode)
        //    {
        //        //Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);
        //        //EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(treeNode);
        //        m_UrakawaSession.PerformTreeNodeSelection(treeNode);

        //        Tuple<TreeNode, TreeNode> treeNodeSelectionAfter2 = m_UrakawaSession.GetTreeNodeSelection();
        //        DebugFix.Assert(treeNodeSelectionAfter2.Item2 == treeNode);
        //    }

        //    CommandRefresh.Execute();
        //}

        private void UndoRedoManagerChanged(List<TreeNodeAudioStreamDeleteCommand> list, bool done)
        {
            if (list.Count <= 0)
            {
#if DEBUG
                Debugger.Break();
#endif
            }

            //if (list.Count == 1)
            //{
            //    UndoRedoManagerChanged(list[0], done);
            //    return;
            //}

            long bytesBegin = 0;
            long bytesEnd = 0;

            foreach (var cmd in list)
            {
                if (cmd.SelectionData.m_LocalStreamLeftMark > 0)
                {
                    bytesBegin += cmd.SelectionData.m_LocalStreamLeftMark;
                }

                if (cmd.SelectionData.m_LocalStreamRightMark <= 0)
                {
                    bytesEnd += cmd.OriginalManagedAudioMedia.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(cmd.OriginalManagedAudioMedia.AudioMediaData.AudioDuration.AsLocalUnits);
                }
                else
                {
                    bytesEnd += cmd.SelectionData.m_LocalStreamRightMark;
                }
            }

            TreeNode targetNode1 = list[0].CurrentTreeNode;
            TreeNode targetNode2 = list[0].CurrentTreeNode == list[0].SelectionData.m_TreeNode ? null : list[0].SelectionData.m_TreeNode;

            long byteStart = bytesBegin;
            long byteDur = bytesEnd - bytesBegin;

            UndoRedoManagerChanged_RestoreAudioTreeNodeSelectionState(targetNode1, targetNode2, byteStart, byteDur, !done);

            //ManagedAudioMedia audioMedia = list[0].SelectionData.m_TreeNode.GetManagedAudioMedia();
            //HandleInsertDelete(list[0].CurrentTreeNode,
            //                   list[0].SelectionData.m_TreeNode,
            //                   bytesBegin,
            //                   bytesEnd - bytesBegin,
            //                   audioMedia,
            //                   !done);
        }

        //private void UndoRedoManagerChanged(TreeNodeAudioStreamDeleteCommand command, bool done)
        //{
        //    ManagedAudioMedia audioMedia = command.SelectionData.m_TreeNode.GetManagedAudioMedia();
        //    if (audioMedia == null) // select ALL + delete ==> audio entirely removed
        //    {
        //        DebugFix.Assert(done); // can be the initial execute or the redo

        //        DebugFix.Assert(command.SelectionData.m_LocalStreamLeftMark == 0);
        //        DebugFix.Assert(command.SelectionData.m_LocalStreamRightMark == -1
        //                     ||
        //                     command.SelectionData.m_LocalStreamRightMark ==
        //                     command.OriginalManagedAudioMedia.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(command.OriginalManagedAudioMedia.AudioMediaData.AudioDuration.AsLocalUnits));
        //    }

        //    //if (audioMedia == null) // select ALL + delete ==> audio entirely removed
        //    //{
        //    //    if (View != null)
        //    //    {
        //    //        View.ResetAll();
        //    //    }

        //    //    //DebugFix.Assert(command.SelectionData.m_LocalStreamLeftMark == -1);
        //    //    //DebugFix.Assert(command.SelectionData.m_LocalStreamRightMark == -1);

        //    //    Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

        //    //    //m_StateToRestore = new StateToRestore
        //    //    //{
        //    //    //    SelectionBegin = -1,
        //    //    //    SelectionEnd = -1,
        //    //    //    LastPlayHeadTime = 0
        //    //    //};
        //    //    m_StateToRestore = null;

        //    //    if (treeNodeSelection.Item1 != command.CurrentTreeNode)
        //    //    {
        //    //        //Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);
        //    //        //EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(command.CurrentTreeNode);
        //    //        m_UrakawaSession.PerformTreeNodeSelection(command.CurrentTreeNode);
        //    //    }
        //    //    else
        //    //    {
        //    //        if (AudioPlaybackStreamKeepAlive)
        //    //        {
        //    //            ensurePlaybackStreamIsDead();
        //    //        }
        //    //        m_CurrentAudioStreamProvider();
        //    //    }

        //    //    Tuple<TreeNode, TreeNode> treeNodeSelectionAfter = m_UrakawaSession.GetTreeNodeSelection();

        //    //    DebugFix.Assert(treeNodeSelectionAfter.Item1 == command.CurrentTreeNode);


        //    //    if (command.SelectionData.m_TreeNode.IsDescendantOf(command.CurrentTreeNode)
        //    //        && treeNodeSelectionAfter.Item2 != command.SelectionData.m_TreeNode)
        //    //    {
        //    //        //Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);
        //    //        //EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(command.SelectionData.m_TreeNode);
        //    //        m_UrakawaSession.PerformTreeNodeSelection(command.SelectionData.m_TreeNode);

        //    //        Tuple<TreeNode, TreeNode> treeNodeSelectionAfter2 = m_UrakawaSession.GetTreeNodeSelection();
        //    //        DebugFix.Assert(treeNodeSelectionAfter2.Item2 == command.SelectionData.m_TreeNode);
        //    //    }

        //    //    m_LastSetPlayBytePosition = -1;
        //    //    //AudioPlayer_UpdateWaveFormPlayHead();
        //    //    if (View != null)
        //    //    {
        //    //        View.RefreshUI_WaveFormPlayHead();
        //    //    }

        //    //    RefreshWaveFormChunkMarkersForCurrentSubTreeNode(false);

        //    //    CommandRefresh.Execute();
        //    //    return;
        //    //}

        //    long bytesBegin = command.SelectionData.m_LocalStreamLeftMark == -1
        //        ? 0
        //        : command.SelectionData.m_LocalStreamLeftMark;

        //    long bytesEnd = command.SelectionData.m_LocalStreamRightMark == -1
        //        ?
        //        (
        //        audioMedia != null ?
        //        audioMedia.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(audioMedia.AudioMediaData.AudioDuration.AsLocalUnits)
        //        : command.OriginalManagedAudioMedia.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(command.OriginalManagedAudioMedia.AudioMediaData.AudioDuration.AsLocalUnits)
        //        )
        //        : command.SelectionData.m_LocalStreamRightMark;

        //    TreeNode targetNode1 = command.CurrentTreeNode;
        //    TreeNode targetNode2 = command.CurrentTreeNode == command.SelectionData.m_TreeNode ? null : command.SelectionData.m_TreeNode;

        //    long byteStart = bytesBegin;
        //    long byteDur = bytesEnd - bytesBegin;

        //    UndoRedoManagerChanged_RestoreAudioTreeNodeSelectionState(targetNode1, targetNode2, byteStart, byteDur, !done);

        //    //HandleInsertDelete(command.CurrentTreeNode,
        //    //                   command.SelectionData.m_TreeNode,
        //    //                   bytesBegin,
        //    //                   bytesEnd - bytesBegin,
        //    //                   audioMedia,
        //    //                   !done);
        //}

        private void UndoRedoManagerChanged(ManagedAudioMediaInsertDataCommand command, bool done)
        {
            TreeNode targetNode1 = command.CurrentTreeNode;
            TreeNode targetNode2 = command.CurrentTreeNode == command.TreeNode ? null : command.TreeNode;

            long byteStart = command.BytePositionInsert;
            long byteDur = command.ManagedAudioMediaSource.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(command.ManagedAudioMediaSource.Duration.AsLocalUnits);

            UndoRedoManagerChanged_RestoreAudioTreeNodeSelectionState(targetNode1, targetNode2, byteStart, byteDur, done);

            //HandleInsertDelete(command.CurrentTreeNode,
            //                   command.TreeNode,
            //                   command.BytePositionInsert,
            //                   command.ManagedAudioMediaSource.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(command.ManagedAudioMediaSource.Duration.AsLocalUnits),
            //                   command.TreeNode.GetManagedAudioMedia(),
            //                   done);
        }

        private void UndoRedoManagerChanged(TreeNodeSetManagedAudioMediaCommand command, bool done)
        {
            TreeNode targetNode1 = command.CurrentTreeNode;
            TreeNode targetNode2 = command.CurrentTreeNode == command.TreeNode ? null : command.TreeNode;

            long byteStart = 0;
            long byteDur = command.ManagedAudioMedia.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(command.ManagedAudioMedia.Duration.AsLocalUnits);

            UndoRedoManagerChanged_RestoreAudioTreeNodeSelectionState(targetNode1, targetNode2, byteStart, byteDur, done);

            //HandleInsertDelete(command.CurrentTreeNode,
            //                   command.TreeNode,
            //                   0,
            //                   command.ManagedAudioMedia.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(command.ManagedAudioMedia.Duration.AsLocalUnits),
            //                   command.TreeNode.GetManagedAudioMedia(),
            //                   done);
            return;

            //Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

            //if (command.TreeNode != treeNodeSelection.Item1)
            //{
            //    if (done)
            //    {
            //        m_StateToRestore = new StateToRestore
            //        {
            //            SelectionBegin = 0,
            //            SelectionEnd = command.ManagedAudioMedia.Duration.AsMilliseconds,
            //            LastPlayHeadTime = 0
            //        };
            //    }

            //    Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);
            //    //EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(command.TreeNode);
            //    m_UrakawaSession.PerformTreeNodeSelection(command.TreeNode);
            //}
            //else
            //{
            //    if (AudioPlaybackStreamKeepAlive)
            //    {
            //        ensurePlaybackStreamIsDead();
            //        m_CurrentAudioStreamProvider();
            //    }

            //    if (done)
            //    {
            //        double timeOffset = getTimeOffset(command.TreeNode, command.ManagedAudioMedia);

            //        m_StateToRestore = new StateToRestore
            //        {
            //            SelectionBegin = timeOffset,
            //            SelectionEnd = timeOffset + command.ManagedAudioMedia.Duration.AsMilliseconds,
            //            LastPlayHeadTime = timeOffset
            //        };
            //    }

            //    CommandRefresh.Execute();
            //}
        }

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

                ManagedAudioMedia manMedia = command.SelectionData.m_TreeNode.GetManagedAudioMedia();
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
            else if (cmd is CompositeCommand)
            {
                if (done)
                {
                    foreach (var childCommand in ((CompositeCommand)cmd).ChildCommands.ContentsAs_Enumerable)
                    {
                        updateTotalDuration(childCommand, done);
                    }
                }
                else
                {
                    foreach (var childCommand in ((CompositeCommand)cmd).ChildCommands.ContentsAs_YieldEnumerableReversed)
                    {
                        updateTotalDuration(childCommand, done);
                    }
                }
            }
            else
            {
                //fine, could be other types of commands
                //Debug.Fail("This should never happen !");
            }
        }

        private void OnUndoRedoManagerChanged(object sender, UndoRedoManagerEventArgs eventt)
        {
            if (m_TTSGen)
            {
                return;
            }

            if (!TheDispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                TheDispatcher.Invoke(DispatcherPriority.Normal, (Action<object, UndoRedoManagerEventArgs>)OnUndoRedoManagerChanged, sender, eventt);
                return;
            }
            //Logger.Log("AudioPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);

            if (!(eventt is DoneEventArgs
                           || eventt is UnDoneEventArgs
                           || eventt is ReDoneEventArgs
                           || eventt is TransactionEndedEventArgs
                           || eventt is TransactionCancelledEventArgs
                           ))
            {
                Debug.Fail("This should never happen !!");
                return;
            }

            bool compCmdAudio = false;
            if (eventt.Command is CompositeCommand)
            {
                foreach (var childCmd in ((CompositeCommand)eventt.Command).ChildCommands.ContentsAs_Enumerable)
                {
                    if (childCmd is ManagedAudioMediaInsertDataCommand
                        || childCmd is TreeNodeSetManagedAudioMediaCommand
                        || childCmd is TreeNodeAudioStreamDeleteCommand)
                    {
                        compCmdAudio = true;
                        break;
                    }
                }
            }

            if (!(eventt.Command is ManagedAudioMediaInsertDataCommand)
                && !(eventt.Command is TreeNodeSetManagedAudioMediaCommand)
                && !(eventt.Command is TreeNodeAudioStreamDeleteCommand)
                && !(eventt.Command is TreeNodeChangeTextCommand)
                && !compCmdAudio
                )
            {
                return;
            }

            if (m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.IsTransactionActive)
            {
                DebugFix.Assert(eventt is DoneEventArgs || eventt is TransactionEndedEventArgs);
                //Logger.Log("AudioPaneViewModel.OnUndoRedoManagerChanged (exit: ongoing TRANSACTION...)", Category.Debug, Priority.Medium);
                return;
            }

            if (eventt is DoneEventArgs)
            {
                DebugFix.Assert(!(eventt.Command is CompositeCommand));
            }
            if (eventt.Command is CompositeCommand)
            {
                DebugFix.Assert(eventt is ReDoneEventArgs || eventt is UnDoneEventArgs || eventt is TransactionEndedEventArgs || eventt is TransactionCancelledEventArgs);
            }

            if (EventAggregator != null)
            {
                EventAggregator.GetEvent<StatusBarMessageUpdateEvent>().Publish(Tobi_Plugin_AudioPane_Lang.Ready);
            }

            if (eventt.Command is TreeNodeChangeTextCommand)
            {
                Tuple<TreeNode, TreeNode> selection = m_UrakawaSession.GetTreeNodeSelection();
                if (selection.Item1 != null)
                {
                    if (((TreeNodeChangeTextCommand)eventt.Command).TreeNode == selection.Item1
                        || ((TreeNodeChangeTextCommand)eventt.Command).TreeNode.IsDescendantOf(selection.Item1))
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
                return;
            }

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


            bool done = eventt is DoneEventArgs || eventt is ReDoneEventArgs || eventt is TransactionEndedEventArgs;
            DebugFix.Assert(done == !(eventt is UnDoneEventArgs || eventt is TransactionCancelledEventArgs));


            updateTotalDuration(eventt.Command, done);

            bool deselect = false;

            Command cmd = eventt.Command;

            Command firstInsertOrSetAudioCmd = null;
            Command lastInsertOrSetAudioCmd = null;

            if (cmd is CompositeCommand)
            {
                DebugFix.Assert(!(eventt is DoneEventArgs)
                    && (eventt is ReDoneEventArgs || eventt is UnDoneEventArgs || eventt is TransactionEndedEventArgs || eventt is TransactionCancelledEventArgs)); // during a transaction every single command is executed.

                var command = (CompositeCommand)cmd;

                //DebugFix.Assert(command.ChildCommands.Count > 0);
                if (command.ChildCommands.Count == 0) return;

                if (command.ChildCommands.Count == 1)
                {
                    cmd = command.ChildCommands.Get(0);
                }
                else
                {
                    var list = command.GetChildCommandsAllType<TreeNodeAudioStreamDeleteCommand>();
                    if (list != null)
                    {
                        UndoRedoManagerChanged(list, done);
                        return;
                    }
                    if (done)
                    {
                        var childCmd = command.ChildCommands.Get(command.ChildCommands.Count - 1);
                        if (childCmd is ManagedAudioMediaInsertDataCommand
                            || childCmd is TreeNodeSetManagedAudioMediaCommand)
                        {
                            cmd = childCmd;
                        }

                        foreach (var childCommand in command.ChildCommands.ContentsAs_Enumerable)
                        {
                            if (childCommand is ManagedAudioMediaInsertDataCommand
                               || childCommand is TreeNodeSetManagedAudioMediaCommand)
                            {
                                if (firstInsertOrSetAudioCmd == null)
                                {
                                    firstInsertOrSetAudioCmd = childCommand;
                                }
                                else
                                {
                                    lastInsertOrSetAudioCmd = childCommand;
                                }
                            }
                        }

                        if (firstInsertOrSetAudioCmd == lastInsertOrSetAudioCmd)
                        {
                            firstInsertOrSetAudioCmd = null;
                            lastInsertOrSetAudioCmd = null;
                        }
                    }
                    else
                    {
                        var childCmd = command.ChildCommands.Get(0);
                        if (childCmd is CompositeCommand)
                        {
                            var list_ =
                                ((CompositeCommand)childCmd).GetChildCommandsAllType<TreeNodeAudioStreamDeleteCommand>();
                            if (list_ != null)
                            {
                                UndoRedoManagerChanged(list_, done);
                                return;
                            }
                        }
                        else if (childCmd is TreeNodeAudioStreamDeleteCommand)
                        {
                            if (command.ChildCommands.Count == 2 &&
                                (command.ChildCommands.Get(1) is ManagedAudioMediaInsertDataCommand
                                || command.ChildCommands.Get(1) is TreeNodeSetManagedAudioMediaCommand))
                            {
                                // split + shift
                                deselect = true;
                            }
                            cmd = childCmd;
                        }
                        else if (childCmd is TreeNodeSetManagedAudioMediaCommand)
                        {
                            cmd = childCmd;
                        }
                    }
                }
            }

            if ((cmd is ManagedAudioMediaInsertDataCommand || cmd is TreeNodeSetManagedAudioMediaCommand)
                && firstInsertOrSetAudioCmd != null && lastInsertOrSetAudioCmd != null)
            {
                TreeNode targetNode1 = null;
                TreeNode targetNode2 = null;

                long byteStart = 0;
                long byteDur = 0;

                if (firstInsertOrSetAudioCmd is ManagedAudioMediaInsertDataCommand)
                {
                    var command = (ManagedAudioMediaInsertDataCommand)firstInsertOrSetAudioCmd;

                    targetNode1 = command.CurrentTreeNode;
                    targetNode2 = command.CurrentTreeNode == command.TreeNode ? null : command.TreeNode;

                    byteStart = command.BytePositionInsert;

                    //UndoRedoManagerChanged(command, done);
                }
                else if (firstInsertOrSetAudioCmd is TreeNodeSetManagedAudioMediaCommand)
                {
                    var command = (TreeNodeSetManagedAudioMediaCommand)firstInsertOrSetAudioCmd;

                    targetNode1 = command.CurrentTreeNode;
                    targetNode2 = command.CurrentTreeNode == command.TreeNode ? null : command.TreeNode;
                    
                    byteStart = 0;

                    //UndoRedoManagerChanged(command, done);
                }
                else
                {
#if DEBUG
                    Debugger.Break();
#endif
                }

                foreach (var childCommand in ((CompositeCommand)eventt.Command).ChildCommands.ContentsAs_Enumerable)
                {
                    if (childCommand is TreeNodeSetManagedAudioMediaCommand)
                    {
                        var ccmd = ((TreeNodeSetManagedAudioMediaCommand) childCommand);
                        byteDur += ccmd.ManagedAudioMedia.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(ccmd.ManagedAudioMedia.Duration.AsLocalUnits);
                    }
                    if (childCommand is ManagedAudioMediaInsertDataCommand)
                    {
                        var ccmd = ((ManagedAudioMediaInsertDataCommand)childCommand);
                        byteDur += ccmd.ManagedAudioMediaSource.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(ccmd.ManagedAudioMediaSource.Duration.AsLocalUnits);
                    }
                }

                UndoRedoManagerChanged_RestoreAudioTreeNodeSelectionState(targetNode1, targetNode2, byteStart, byteDur, done);
                return;
            }

            if (cmd is ManagedAudioMediaInsertDataCommand)
            {
                var command = (ManagedAudioMediaInsertDataCommand)cmd;

                UndoRedoManagerChanged(command, done);
                return;
            }

            if (cmd is TreeNodeSetManagedAudioMediaCommand)
            {
                var command = (TreeNodeSetManagedAudioMediaCommand)cmd;

                UndoRedoManagerChanged(command, done);
                return;
            }

            if (cmd is TreeNodeAudioStreamDeleteCommand)
            {
                var command = (TreeNodeAudioStreamDeleteCommand)cmd;

                UndoRedoManagerChanged(new List<TreeNodeAudioStreamDeleteCommand>(new[] { command }), done);

                if (deselect)
                {
                    CommandClearSelection.Execute();
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
