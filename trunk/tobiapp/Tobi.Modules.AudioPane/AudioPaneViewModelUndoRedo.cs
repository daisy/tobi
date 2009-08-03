using System.Diagnostics;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using urakawa.commands;
using urakawa.core;
using urakawa.events;
using urakawa.events.undo;

namespace Tobi.Modules.AudioPane
{
    public partial class AudioPaneViewModel
    {
        private void UndoRedoManagerChanged(TreeNodeSetManagedAudioMediaCommand command)
        {
            TreeNode treeNode = command.TreeNode;

            if (treeNode == null)
            {
                return;
            }

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
                m_CurrentAudioStreamProvider();
            }

            double timeOffset = getTimeOffset(treeNode, command.ManagedAudioMedia);

            m_StateToRestore = new StateToRestore
            {
                SelectionBegin = -1,
                SelectionEnd = -1,
                LastPlayHeadTime = timeOffset
            };

            bool isTreeNodeInAudioWaveForm = State.IsTreeNodeShownInAudioWaveForm(treeNode);

            if (!isTreeNodeInAudioWaveForm)
            {
                Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);
                EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
            }
            else
            {
                ReloadWaveForm();
            }
        }

        private void UndoRedoManagerChanged(ManagedAudioMediaInsertDataCommand command, UndoRedoManagerEventArgs e)
        {
            TreeNode treeNode = command.TreeNode;

            if (treeNode == null)
            {
                return;
            }

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
                m_CurrentAudioStreamProvider();
            }

            double timeOffset = getTimeOffset(treeNode, command.ManagedAudioMediaTarget);

            if (e is DoneEventArgs || e is ReDoneEventArgs)
            {
                double begin = command.TimeInsert.TimeAsMillisecondFloat + timeOffset;
                m_StateToRestore = new StateToRestore
                {
                    SelectionBegin = begin,
                    SelectionEnd = begin + command.ManagedAudioMediaSource.Duration.TimeDeltaAsMillisecondDouble,
                    LastPlayHeadTime = begin
                };
            }
            else if (e is UnDoneEventArgs)
            {
                m_StateToRestore = new StateToRestore
                {
                    SelectionBegin = -1,
                    SelectionEnd = -1,
                    LastPlayHeadTime = command.TimeInsert.TimeAsMillisecondFloat + timeOffset
                };
            }

            bool isTreeNodeInAudioWaveForm = State.IsTreeNodeShownInAudioWaveForm(treeNode);

            if (!isTreeNodeInAudioWaveForm)
            {
                Logger.Log("-- PublishEvent [TreeNodeSelectedEvent] AudioPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);
                EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
            }
            else
            {
                ReloadWaveForm();
            }
        }

        private void OnUndoRedoManagerChanged(object sender, DataModelChangedEventArgs e)
        {
            Logger.Log("AudioPaneViewModel.OnUndoRedoManagerChanged", Category.Debug, Priority.Medium);

            bool refresh = e is TransactionStartedEventArgs
                           || e is TransactionEndedEventArgs
                           || e is TransactionCancelledEventArgs
                           || e is DoneEventArgs
                           || e is UnDoneEventArgs
                           || e is ReDoneEventArgs;
            if (!refresh)
            {
                Debug.Fail("This should never happen !!");
                return;
            }

            var eventt = (UndoRedoManagerEventArgs)e;

            var presenter = Container.Resolve<IShellPresenter>();
            presenter.PlayAudioCueTockTock();

            if (View != null)
            {
                View.ResetAll();
            }

            if (eventt.Command is ManagedAudioMediaInsertDataCommand)
            {
                var command = (ManagedAudioMediaInsertDataCommand)eventt.Command;

                UndoRedoManagerChanged(command, eventt);
                return;
            }
            else if (eventt.Command is TreeNodeSetManagedAudioMediaCommand)
            {
                var command = (TreeNodeSetManagedAudioMediaCommand)eventt.Command;

                UndoRedoManagerChanged(command);
                return;
            }
            // TODO: TreeNode delete command, etc. (make sure CurrentTreeNode / CurrentSubTreeNode is up to date)

            if (AudioPlaybackStreamKeepAlive)
            {
                ensurePlaybackStreamIsDead();
            }
            ReloadWaveForm();
        }
    }
}
