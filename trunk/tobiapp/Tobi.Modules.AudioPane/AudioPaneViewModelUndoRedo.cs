using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Logging;
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
        private void UndoRedoManagerChanged(List<TreeNodeAudioStreamDeleteCommand> list, bool done)
        {
            if (list.Count == 1)
            {
                UndoRedoManagerChanged(list[0], done);
                return;
            }

            Time timeBegin = Time.Zero;
            Time timeEnd = Time.Zero;

            foreach (var cmd in list)
            {
                if (cmd.SelectionData.m_LocalStreamLeftMark > 0)
                {
                    timeBegin.Add(new Time(cmd.OriginalManagedAudioMedia.AudioMediaData.PCMFormat.Data.ConvertBytesToTime(cmd.SelectionData.m_LocalStreamLeftMark)));
                }

                if (cmd.SelectionData.m_LocalStreamRightMark <= 0)
                {
                    timeEnd.Add(new Time(cmd.OriginalManagedAudioMedia.AudioMediaData.AudioDuration.AsTimeSpan));
                }
                else
                {
                    timeEnd.Add(new Time(
                            cmd.OriginalManagedAudioMedia.AudioMediaData.PCMFormat.Data.ConvertBytesToTime(cmd.SelectionData.m_LocalStreamRightMark)));
                }
            }

            ManagedAudioMedia audioMedia = list[0].SelectionData.m_TreeNode.GetManagedAudioMedia();

            HandleInsertDelete(list[0].CurrentTreeNode,
                               list[0].SelectionData.m_TreeNode,
                               timeBegin,
                               timeEnd.AsMilliseconds - timeBegin.AsMilliseconds,
                               audioMedia,
                               !done);
        }

        private void UndoRedoManagerChanged(TreeNodeAudioStreamDeleteCommand command, bool done)
        {
            ManagedAudioMedia audioMedia = command.SelectionData.m_TreeNode.GetManagedAudioMedia();
            if (audioMedia == null) // select ALL + delete ==> audio entirely removed
            {
                Debug.Assert(done); // can be the initial execute or the redo

                //Debug.Assert(command.SelectionData.m_LocalStreamLeftMark == -1);
                //Debug.Assert(command.SelectionData.m_LocalStreamRightMark == -1);

                Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

                //m_StateToRestore = new StateToRestore
                //{
                //    SelectionBegin = -1,
                //    SelectionEnd = -1,
                //    LastPlayHeadTime = 0
                //};
                m_StateToRestore = null;

                if (treeNodeSelection.Item1 != command.CurrentTreeNode)
                {
                    Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged",
                               Category.Debug, Priority.Medium);
                    //EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(command.CurrentTreeNode);
                    m_UrakawaSession.PerformTreeNodeSelection(command.CurrentTreeNode);
                }
                else
                {
                    if (AudioPlaybackStreamKeepAlive)
                    {
                        ensurePlaybackStreamIsDead();
                        m_CurrentAudioStreamProvider();
                    }
                }

                Tuple<TreeNode, TreeNode> treeNodeSelectionAfter = m_UrakawaSession.GetTreeNodeSelection();

                Debug.Assert(treeNodeSelectionAfter.Item1 == command.CurrentTreeNode);

                if (command.SelectionData.m_TreeNode.IsDescendantOf(command.CurrentTreeNode)
                    && treeNodeSelectionAfter.Item2 != command.SelectionData.m_TreeNode)
                {
                    Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged",
                               Category.Debug, Priority.Medium);
                    //EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(command.SelectionData.m_TreeNode);
                    m_UrakawaSession.PerformTreeNodeSelection(command.SelectionData.m_TreeNode);

                    Tuple<TreeNode, TreeNode> treeNodeSelectionAfter2 = m_UrakawaSession.GetTreeNodeSelection();
                    Debug.Assert(treeNodeSelectionAfter2.Item2 == command.SelectionData.m_TreeNode);
                }

                m_LastPlayHeadTime = -1;
                //AudioPlayer_UpdateWaveFormPlayHead();
                if (View != null)
                {
                    View.RefreshUI_WaveFormPlayHead();
                }

                RefreshWaveFormChunkMarkersForCurrentSubTreeNode(false);

                CommandRefresh.Execute();
                return;
            }

            Time timeBegin = command.SelectionData.m_LocalStreamLeftMark == -1
                ? Time.Zero
                : new Time(audioMedia.AudioMediaData.PCMFormat.Data.ConvertBytesToTime(command.SelectionData.m_LocalStreamLeftMark));

            Time timeEnd = command.SelectionData.m_LocalStreamRightMark == -1
                ? new Time(audioMedia.AudioMediaData.AudioDuration.AsTimeSpan)
                : new Time(audioMedia.AudioMediaData.PCMFormat.Data.ConvertBytesToTime(command.SelectionData.m_LocalStreamRightMark));

            HandleInsertDelete(command.CurrentTreeNode,
                               command.SelectionData.m_TreeNode,
                               timeBegin,
                               timeEnd.AsMilliseconds - timeBegin.AsMilliseconds,
                               audioMedia,
                               !done);
        }

        private void UndoRedoManagerChanged(ManagedAudioMediaInsertDataCommand command, bool done)
        {
            HandleInsertDelete(command.CurrentTreeNode,
                               command.TreeNode,
                               command.TimeInsert,
                               command.ManagedAudioMediaSource.Duration.AsMilliseconds,
                               command.TreeNode.GetManagedAudioMedia(),
                               done);
        }

        private void HandleInsertDelete(TreeNode currentTreeNode, TreeNode treeNode,
            Time timeInsert, double selectionDur,
            ManagedAudioMedia managedAudioMediaTarget,
            bool done)
        {
            Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

            m_StateToRestore = null;

            if (treeNodeSelection.Item1 != currentTreeNode)
            {
                //StateToRestore? state = new StateToRestore
                //{
                //    SelectionBegin = m_StateToRestore.Value.SelectionBegin,
                //    SelectionEnd = m_StateToRestore.Value.SelectionEnd,
                //    LastPlayHeadTime = m_StateToRestore.Value.LastPlayHeadTime
                //};

                //State.CurrentTreeNode = null;

                Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged",
                           Category.Debug, Priority.Medium);
                //EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(currentTreeNode);
                m_UrakawaSession.PerformTreeNodeSelection(currentTreeNode);

                //m_StateToRestore = state;

                //if (AudioPlaybackStreamKeepAlive)
                //{
                //    ensurePlaybackStreamIsDead();
                //}

                //State.ResetAll();
                //State.CurrentTreeNode = commandCurrentTreeNode;
                //m_CurrentAudioStreamProvider = m_AudioStreamProvider_TreeNode;

                //m_CurrentAudioStreamProvider();
            }
            else
            {
                if (AudioPlaybackStreamKeepAlive)
                {
                    ensurePlaybackStreamIsDead();
                    m_CurrentAudioStreamProvider();
                }
            }

            //bool isTreeNodeInAudioWaveForm = State.IsTreeNodeShownInAudioWaveForm(treeNode);

            //double timeOffset = isTreeNodeInAudioWaveForm && managedAudioMediaTarget != null
            //                        ? getTimeOffset(treeNode, managedAudioMediaTarget)
            //                        : 0;

            Tuple<TreeNode, TreeNode> treeNodeSelectionAfter = m_UrakawaSession.GetTreeNodeSelection();
            Debug.Assert(treeNodeSelectionAfter.Item1 == currentTreeNode);

            double timeOffset = 0;
            if (managedAudioMediaTarget == null)
            {
                TreeNode prev = treeNode.GetPreviousSiblingWithManagedAudio();
                if (prev != null && prev.IsDescendantOf(currentTreeNode))
                {
                    timeOffset = getTimeOffset(prev, managedAudioMediaTarget);
                    ManagedAudioMedia prevAudio = prev.GetManagedAudioMedia();
                    Debug.Assert(prevAudio != null);
                    if (prevAudio != null)
                    {
                        timeOffset += prevAudio.AudioMediaData.AudioDuration.AsMilliseconds;
                    }
                }
            }
            else
            {
                timeOffset = getTimeOffset(treeNode, managedAudioMediaTarget);
            }

            if (done)
            {
                double begin = timeOffset + timeInsert.AsMilliseconds;
                m_StateToRestore = new StateToRestore
                {
                    SelectionBegin = begin,
                    SelectionEnd = begin + selectionDur,
                    LastPlayHeadTime = begin
                };
            }
            else
            {
                m_StateToRestore = new StateToRestore
                {
                    SelectionBegin = -1,
                    SelectionEnd = -1,
                    LastPlayHeadTime = timeOffset + timeInsert.AsMilliseconds
                };
            }

            if (treeNodeSelectionAfter.Item2 != treeNode
                && treeNode != currentTreeNode)
            {
                Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged",
                           Category.Debug, Priority.Medium);
                //EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(treeNode);
                m_UrakawaSession.PerformTreeNodeSelection(treeNode);

                Tuple<TreeNode, TreeNode> treeNodeSelectionAfter2 = m_UrakawaSession.GetTreeNodeSelection();
                Debug.Assert(treeNodeSelectionAfter2.Item2 == treeNode);
            }

            CommandRefresh.Execute();
        }

        private void UndoRedoManagerChanged(TreeNodeSetManagedAudioMediaCommand command, bool done)
        {
            HandleInsertDelete(command.TreeNode,
                               command.TreeNode,
                               Time.Zero,
                               command.ManagedAudioMedia.Duration.AsMilliseconds,
                               command.TreeNode.GetManagedAudioMedia(),
                               done);
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

        private void OnUndoRedoManagerChanged(object sender, UndoRedoManagerEventArgs eventt)
        {
            Logger.Log("AudioPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);

            if (!(eventt is DoneEventArgs
                           || eventt is UnDoneEventArgs
                           || eventt is ReDoneEventArgs
                           || eventt is TransactionEndedEventArgs))
            {
                Debug.Fail("This should never happen !!");
                return;
            }

            if (eventt is DoneEventArgs && m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.IsTransactionActive)
            {
                Logger.Log("AudioPaneViewModel.OnUndoRedoManagerChanged (exit: ongoing TRANSACTION...)", Category.Debug, Priority.Medium);
                return;
            }

            if (!Dispatcher.CheckAccess())
            {
#if DEBUG
                Debugger.Break();
#endif
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action<object, UndoRedoManagerEventArgs>)OnUndoRedoManagerChanged, sender, eventt);
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

            AudioPlayer_Stop();

            AudioCues.PlayTockTock();

            if (View != null)
            {
                View.ResetAll();
            }

            bool done = eventt is DoneEventArgs || eventt is ReDoneEventArgs || eventt is TransactionEndedEventArgs;

            Command cmd = eventt.Command;

            if (eventt.Command is CompositeCommand)
            {
                Debug.Assert(!(eventt is DoneEventArgs)
                    && (eventt is ReDoneEventArgs || eventt is UnDoneEventArgs || eventt is TransactionEndedEventArgs)); // during a transaction every single command is executed.

                var command = (CompositeCommand)eventt.Command;
                //Debug.Assert(command.ChildCommands.Count > 1);

                var list = command.GetChildCommandsAllType<TreeNodeAudioStreamDeleteCommand>();
                if (list != null)
                {
                    UndoRedoManagerChanged(list, done);
                    return;
                }

                if (command.ChildCommands.Count > 0)
                {
                    if (done)
                    {
                        var childCmd = command.ChildCommands.Get(command.ChildCommands.Count - 1);
                        if (childCmd is ManagedAudioMediaInsertDataCommand)
                        {
                            cmd = childCmd;
                        }
                    }
                    else
                    {
                        var childCmd = command.ChildCommands.Get(0);
                        if (childCmd is CompositeCommand)
                        {
                            var list_ = ((CompositeCommand)childCmd).GetChildCommandsAllType<TreeNodeAudioStreamDeleteCommand>();
                            if (list_ != null)
                            {
                                UndoRedoManagerChanged(list_, done);
                                return;
                            }
                        }
                        else if (childCmd is TreeNodeAudioStreamDeleteCommand)
                        {
                            cmd = childCmd;
                        }
                    }
                }
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

                UndoRedoManagerChanged(command, done);
                return;
            }

            m_StateToRestore = null;
            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
            }
            CommandRefresh.Execute();
        }
    }
}
