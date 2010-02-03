using System.Diagnostics;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using urakawa.commands;
using urakawa.events.undo;

namespace Tobi.Plugin.AudioPane
{
    public partial class AudioPaneViewModel
    {
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

        private void UndoRedoManagerChanged(ManagedAudioMediaInsertDataCommand command, bool done)
        {
            bool bCurrentTreeNodeNeedsRefresh = false;
            if (State.CurrentTreeNode != command.CurrentTreeNode)
            {
                bCurrentTreeNodeNeedsRefresh = true;

                if (AudioPlaybackStreamKeepAlive)
                {
                    ensurePlaybackStreamIsDead();
                }

                State.ResetAll();
                State.CurrentTreeNode = command.CurrentTreeNode;
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

            bool isTreeNodeInAudioWaveForm = State.IsTreeNodeShownInAudioWaveForm(command.TreeNode);

            double timeOffset = isTreeNodeInAudioWaveForm ? getTimeOffset(command.TreeNode, command.ManagedAudioMediaTarget) : 0;

            if (done)
            {
                double begin = timeOffset + command.TimeInsert.TimeAsMillisecondFloat;
                m_StateToRestore = new StateToRestore
                {
                    SelectionBegin = begin,
                    SelectionEnd = begin + command.ManagedAudioMediaSource.Duration.TimeDeltaAsMillisecondDouble,
                    LastPlayHeadTime = begin
                };
            }
            else
            {
                m_StateToRestore = new StateToRestore
                {
                    SelectionBegin = -1,
                    SelectionEnd = -1,
                    LastPlayHeadTime = timeOffset + command.TimeInsert.TimeAsMillisecondFloat
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
                EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(command.CurrentTreeNode);

                m_StateToRestore = state;
            }

            if (State.CurrentSubTreeNode != command.TreeNode)
            {
                Logger.Log("-- PublishEvent [SubTreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged",
                    Category.Debug, Priority.Medium);
                EventAggregator.GetEvent<SubTreeNodeSelectedEvent>().Publish(command.TreeNode);
            }
            else
            {
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
            // TODO: TreeNode delete command, etc. (make sure CurrentTreeNode / CurrentSubTreeNode is up to date)

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
            }
            CommandRefresh.Execute();
        }
    }
}
