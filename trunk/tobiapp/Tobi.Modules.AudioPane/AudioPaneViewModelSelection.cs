using System.Windows;
using System.Windows.Media;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.core;

namespace Tobi.Modules.AudioPane
{
    public partial class AudioPaneViewModel
    {
        private void initializeCommands_Selection()
        {
            var shellPresenter = Container.Resolve<IShellPresenter>();

            CommandSelectPreviousChunk = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_SelectPreviousChunk,
                UserInterfaceStrings.Audio_SelectPreviousChunk_,
                UserInterfaceStrings.Audio_SelectPreviousChunk_KEYS,
                shellPresenter.LoadTangoIcon("go-previous"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandSelectPreviousChunk", Category.Debug, Priority.Medium);

                    CommandStepBack.Execute(null);

                    SelectChunk(State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime));
                },
                obj => CommandStepBack.CanExecute(null));

            shellPresenter.RegisterRichCommand(CommandSelectPreviousChunk);
            //
            CommandSelectNextChunk = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_SelectNextChunk,
                UserInterfaceStrings.Audio_SelectNextChunk_,
                UserInterfaceStrings.Audio_SelectNextChunk_KEYS,
                shellPresenter.LoadTangoIcon("go-next"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandSelectNextChunk", Category.Debug, Priority.Medium);

                    CommandStepForward.Execute(null);

                    SelectChunk(State.Audio.ConvertMillisecondsToBytes(LastPlayHeadTime));
                },
                obj => CommandStepForward.CanExecute(null));

            shellPresenter.RegisterRichCommand(CommandSelectNextChunk);
            //
            //
            CommandEndSelection = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_EndSelection,
                UserInterfaceStrings.Audio_EndSelection_,
                UserInterfaceStrings.Audio_EndSelection_KEYS,
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Right1")),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandEndSelection", Category.Debug, Priority.Medium);

                    var presenter = Container.Resolve<IShellPresenter>();
                    presenter.PlayAudioCueTockTock();

                    if (m_SelectionBeginTmp < 0)
                    {
                        return;
                    }
                    double begin = m_SelectionBeginTmp;
                    double end = LastPlayHeadTime;

                    if (begin == end)
                    {
                        CommandClearSelection.Execute(null);
                        return;
                    }

                    if (begin > end)
                    {
                        double tmp = begin;
                        begin = end;
                        end = tmp;
                    }

                    State.Selection.SetSelectionTime(begin, end);

                    if (IsAutoPlay)
                    {
                        if (!State.Audio.HasContent)
                        {
                            return;
                        }

                        IsAutoPlay = false;
                        LastPlayHeadTime = begin;
                        IsAutoPlay = true;

                        long bytesFrom = State.Audio.ConvertMillisecondsToBytes(begin);
                        long bytesTo = State.Audio.ConvertMillisecondsToBytes(end);

                        AudioPlayer_PlayFromTo(bytesFrom, bytesTo);
                    }
                },
                obj => !IsWaveFormLoading && IsAudioLoaded && m_SelectionBeginTmp >= 0);

            shellPresenter.RegisterRichCommand(CommandEndSelection);
            //
            CommandBeginSelection = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_BeginSelection,
                UserInterfaceStrings.Audio_BeginSelection_,
                UserInterfaceStrings.Audio_BeginSelection_KEYS,
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Left1")),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandBeginSelection", Category.Debug, Priority.Medium);

                    m_SelectionBeginTmp = LastPlayHeadTime;

                    var presenter = Container.Resolve<IShellPresenter>();
                    presenter.PlayAudioCueTock();
                },
                obj => !IsWaveFormLoading && IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandBeginSelection);
            //
            CommandSelectAll = new RichDelegateCommand<object>(
                UserInterfaceStrings.SelectAll,
                UserInterfaceStrings.SelectAll_,
                UserInterfaceStrings.SelectAll_KEYS,
                shellPresenter.LoadTangoIcon("view-fullscreen"),
                obj =>
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

                    var presenter = Container.Resolve<IShellPresenter>();
                    presenter.PlayAudioCueTockTock();
                },
                obj => !IsWaveFormLoading);

            shellPresenter.RegisterRichCommand(CommandSelectAll);
            //
            CommandClearSelection = new RichDelegateCommand<object>(UserInterfaceStrings.Audio_ClearSelection,
                UserInterfaceStrings.Audio_ClearSelection_,
                UserInterfaceStrings.Audio_ClearSelection_KEYS,
                shellPresenter.LoadTangoIcon("edit-clear"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandClearSelection", Category.Debug, Priority.Medium);

                    State.Selection.ClearSelection();
                },
                obj => !IsWaveFormLoading && IsSelectionSet);

            shellPresenter.RegisterRichCommand(CommandClearSelection);
            //
        }

        private double m_SelectionBeginTmp = -1;

        public void SelectChunk(long byteOffset)
        {
            if (State.CurrentTreeNode == null || !State.Audio.HasContent)
            {
                return;
            }

            //if (PlayStreamMarkers == null || PlayStreamMarkers.Count == 1)
            //{
            //    SelectAll();
            //    return;
            //}

            long sumData = 0;
            long sumDataPrev = 0;
            int index = -1;
            foreach (TreeNodeAndStreamDataLength marker in State.Audio.PlayStreamMarkers)
            {
                index++;
                sumData += marker.m_LocalStreamDataLength;
                if (byteOffset < sumData
                    || index == (State.Audio.PlayStreamMarkers.Count - 1) && byteOffset >= sumData)
                {
                    //subTreeNode = marker.m_TreeNode;

                    State.Selection.SetSelectionBytes(sumDataPrev, sumData);

                    break;
                }
                sumDataPrev = sumData;
            }
        }

        [NotifyDependsOn("SelectionBegin")]
        [NotifyDependsOn("SelectionEnd")]
        public bool IsSelectionSet
        {
            get
            {
                if (State.Audio.HasContent)
                {
                    return State.Selection.IsSelectionSet;
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
