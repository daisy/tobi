using System;
using System.Diagnostics;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using urakawa.commands;
using urakawa.core;
using urakawa.events.undo;
using urakawa.media.data.audio;
using urakawa.media.timing;

namespace Tobi.Plugin.AudioPane
{
    public partial class AudioPaneViewModel
    {
        private void UndoRedoManagerChanged(TreeNodeAudioStreamDeleteCommand command, bool done)
        {
            ManagedAudioMedia audioMedia = command.SelectionData.m_TreeNode.GetManagedAudioMedia();
            if(audioMedia==null)
            {
                bool bCurrentTreeNodeNeedsRefresh = resetCurrentTreeNodeState(command.CurrentTreeNode);

                if (bCurrentTreeNodeNeedsRefresh)
                {
                    m_StateToRestore = null;

                    State.CurrentTreeNode = null;

                    Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged",
                               Category.Debug, Priority.Medium);
                    EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(command.CurrentTreeNode);
                }

                if (State.CurrentSubTreeNode != command.SelectionData.m_TreeNode)
                {
                    Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged",
                               Category.Debug, Priority.Medium);
                    EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(command.SelectionData.m_TreeNode);
                }

                CommandRefresh.Execute();
                return;
            }

            Time timeBegin = command.SelectionData.m_LocalStreamLeftMark == -1
                ? Time.Zero
                : new Time(audioMedia.AudioMediaData.PCMFormat.Data.ConvertBytesToTime(command.SelectionData.m_LocalStreamLeftMark));

            Time timeEnd = command.SelectionData.m_LocalStreamRightMark == -1
                ? Time.Zero.AddTimeDelta(audioMedia.AudioMediaData.AudioDuration)
                : new Time(audioMedia.AudioMediaData.PCMFormat.Data.ConvertBytesToTime(command.SelectionData.m_LocalStreamRightMark));

            //Debug.Assert(audioMedia != null); Can be null, if the deleted audio range was the entire audio media

            HandleInsertDelete(command.CurrentTreeNode, command.SelectionData.m_TreeNode, timeBegin,
                               timeEnd.TimeAsMillisecondDouble - timeBegin.TimeAsMillisecondDouble,
                               audioMedia, !done);
        }

        private void UndoRedoManagerChanged(ManagedAudioMediaInsertDataCommand command, bool done)
        {
            HandleInsertDelete(command.CurrentTreeNode, command.TreeNode, command.TimeInsert,
                               command.ManagedAudioMediaSource.Duration.TimeDeltaAsMillisecondDouble,
                               command.TreeNode.GetManagedAudioMedia(), done);
        }

        private bool resetCurrentTreeNodeState(TreeNode commandCurrentTreeNode)
        {
            bool bCurrentTreeNodeNeedsRefresh = false;
            if (State.CurrentTreeNode != commandCurrentTreeNode)
            {
                bCurrentTreeNodeNeedsRefresh = true;

                if (AudioPlaybackStreamKeepAlive)
                {
                    ensurePlaybackStreamIsDead();
                }

                State.ResetAll();
                State.CurrentTreeNode = commandCurrentTreeNode;
                m_CurrentAudioStreamProvider = m_AudioStreamProvider_TreeNode;

                m_CurrentAudioStreamProvider();
            }
            else
            {
                if (AudioPlaybackStreamKeepAlive)
                {
                    ensurePlaybackStreamIsDead();
                    m_CurrentAudioStreamProvider();
                }
            }

            return bCurrentTreeNodeNeedsRefresh;
        }

        private void HandleInsertDelete(TreeNode currentTreeNode, TreeNode treeNode, Time timeInsert,
            double selectionDur, ManagedAudioMedia managedAudioMediaTarget, bool done)
        {
            bool bCurrentTreeNodeNeedsRefresh = resetCurrentTreeNodeState(currentTreeNode);

            bool isTreeNodeInAudioWaveForm = State.IsTreeNodeShownInAudioWaveForm(treeNode);

            double timeOffset = isTreeNodeInAudioWaveForm && managedAudioMediaTarget != null
                                    ? getTimeOffset(treeNode, managedAudioMediaTarget)
                                    : 0;

            if (done)
            {
                double begin = timeOffset + timeInsert.TimeAsMillisecondDouble;
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
                    LastPlayHeadTime = timeOffset + timeInsert.TimeAsMillisecondDouble
                };
            }

            if (bCurrentTreeNodeNeedsRefresh)
            {
                StateToRestore? state = new StateToRestore
                {
                    SelectionBegin = m_StateToRestore.Value.SelectionBegin,
                    SelectionEnd = m_StateToRestore.Value.SelectionEnd,
                    LastPlayHeadTime = m_StateToRestore.Value.LastPlayHeadTime
                };

                State.CurrentTreeNode = null;

                Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged",
                           Category.Debug, Priority.Medium);
                EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(currentTreeNode);

                m_StateToRestore = state;
            }

            if (State.CurrentSubTreeNode != treeNode)
            {
                Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged",
                           Category.Debug, Priority.Medium);
                EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(treeNode);
            }

            CommandRefresh.Execute();
        }

        private void UndoRedoManagerChanged(TreeNodeSetManagedAudioMediaCommand command, bool done)
        {
            bool isTreeNodeInAudioWaveForm = State.IsTreeNodeShownInAudioWaveForm(command.TreeNode);

            if (!isTreeNodeInAudioWaveForm)
            {
                if (done)
                {
                    m_StateToRestore = new StateToRestore
                    {
                        SelectionBegin = 0,
                        SelectionEnd = command.ManagedAudioMedia.Duration.TimeDeltaAsMillisecondDouble,
                        LastPlayHeadTime = 0
                    };
                }

                Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);
                EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(command.TreeNode);
            }
            else
            {
                if (AudioPlaybackStreamKeepAlive)
                {
                    ensurePlaybackStreamIsDead();
                    m_CurrentAudioStreamProvider();
                }

                if (done)
                {
                    double timeOffset = getTimeOffset(command.TreeNode, command.ManagedAudioMedia);

                    m_StateToRestore = new StateToRestore
                    {
                        SelectionBegin = timeOffset,
                        SelectionEnd = timeOffset + command.ManagedAudioMedia.Duration.TimeDeltaAsMillisecondDouble,
                        LastPlayHeadTime = timeOffset
                    };
                }

                CommandRefresh.Execute();
            }
        }

        private void OnUndoRedoManagerChanged(object sender, UndoRedoManagerEventArgs eventt)
        {
            Logger.Log("AudioPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);

            bool refresh = eventt is DoneEventArgs
                           || eventt is UnDoneEventArgs
                           || eventt is ReDoneEventArgs;
            if (!refresh)
            {
                Debug.Fail("This should never happen !!");
                return;
            }

            AudioCues.PlayTockTock();

            if (View != null)
            {
                View.ResetAll();
            }

            bool done = eventt is DoneEventArgs || eventt is ReDoneEventArgs;

            if (eventt.Command is ManagedAudioMediaInsertDataCommand)
            {
                var command = (ManagedAudioMediaInsertDataCommand)eventt.Command;

                UndoRedoManagerChanged(command, done);
                return;
            }

            if (eventt.Command is TreeNodeSetManagedAudioMediaCommand)
            {
                var command = (TreeNodeSetManagedAudioMediaCommand)eventt.Command;

                UndoRedoManagerChanged(command, done);
                return;
            }

            if (eventt.Command is TreeNodeAudioStreamDeleteCommand)
            {
                var command = (TreeNodeAudioStreamDeleteCommand)eventt.Command;

                UndoRedoManagerChanged(command, done);
                return;
            }

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
            }
            CommandRefresh.Execute();
        }
    }
}
