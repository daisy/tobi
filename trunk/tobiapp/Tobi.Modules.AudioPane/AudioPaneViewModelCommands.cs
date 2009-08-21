using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.core;

namespace Tobi.Modules.AudioPane
{
    public partial class AudioPaneViewModel
    {
        #region Commands

        public RichDelegateCommand<object> CommandShowOptionsDialog { get; private set; }

        public RichDelegateCommand<object> CommandFocus { get; private set; }
        public RichDelegateCommand<object> CommandOpenFile { get; private set; }
        public RichDelegateCommand<object> CommandInsertFile { get; private set; }
        public RichDelegateCommand<object> CommandGotoBegining { get; private set; }
        public RichDelegateCommand<object> CommandGotoEnd { get; private set; }
        public RichDelegateCommand<object> CommandStepBack { get; private set; }
        public RichDelegateCommand<object> CommandStepForward { get; private set; }
        public RichDelegateCommand<object> CommandRewind { get; private set; }
        public RichDelegateCommand<object> CommandFastForward { get; private set; }
        public RichDelegateCommand<object> CommandSelectAll { get; private set; }
        public RichDelegateCommand<object> CommandClearSelection { get; private set; }
        public RichDelegateCommand<object> CommandZoomSelection { get; private set; }
        public RichDelegateCommand<object> CommandZoomFitFull { get; private set; }
        public RichDelegateCommand<object> CommandRefresh { get; private set; }
        public RichDelegateCommand<object> CommandAutoPlay { get; private set; }
        public RichDelegateCommand<object> CommandPlay { get; private set; }
        public RichDelegateCommand<object> CommandPlayPreviewLeft { get; private set; }
        public RichDelegateCommand<object> CommandPlayPreviewRight { get; private set; }
        public RichDelegateCommand<object> CommandPause { get; private set; }
        public RichDelegateCommand<object> CommandStartRecord { get; private set; }
        public RichDelegateCommand<object> CommandStopRecord { get; private set; }
        public RichDelegateCommand<object> CommandStartMonitor { get; private set; }
        public RichDelegateCommand<object> CommandStopMonitor { get; private set; }
        public RichDelegateCommand<object> CommandBeginSelection { get; private set; }
        public RichDelegateCommand<object> CommandEndSelection { get; private set; }
        public RichDelegateCommand<object> CommandSelectNextChunk { get; private set; }
        public RichDelegateCommand<object> CommandSelectPreviousChunk { get; private set; }
        public RichDelegateCommand<object> CommandDeleteAudioSelection { get; private set; }


        public IInputBindingManager InputBindingManager
        {
            get
            {
                var shellPresenter = Container.Resolve<IShellPresenter>();

                return shellPresenter;
            }
        }

        private void initializeCommands_View()
        {
            var shellPresenter = Container.Resolve<IShellPresenter>();

            CommandRefresh = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_Reload,
                UserInterfaceStrings.Audio_Reload_,
                null,
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Refresh")),
                //shellPresenter.LoadTangoIcon("view-refresh"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandRefresh", Category.Debug, Priority.Medium);

                    StartWaveFormLoadTimer(500, IsAutoPlay);
                },
                obj => !IsWaveFormLoading && IsAudioLoaded);

            shellPresenter.RegisterRichCommand(CommandRefresh);
            //
            CommandZoomSelection = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_ZoomSelection,
                UserInterfaceStrings.Audio_ZoomSelection_,
                UserInterfaceStrings.Audio_ZoomSelection_KEYS,
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Search")),
                //shellPresenter.LoadTangoIcon("system-search"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandZoomSelection", Category.Debug, Priority.Medium);

                    View.ZoomSelection();
                },
                obj => View != null && !IsWaveFormLoading && IsSelectionSet);

            shellPresenter.RegisterRichCommand(CommandZoomSelection);
            //
            CommandZoomFitFull = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_FitFull,
                UserInterfaceStrings.Audio_FitFull_,
                UserInterfaceStrings.Audio_FitFull_KEYS,
                shellPresenter.LoadTangoIcon("utilities-system-monitor"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandZoomFitFull", Category.Debug, Priority.Medium);

                    View.ZoomFitFull();
                },
                obj => View != null && !IsWaveFormLoading);

            shellPresenter.RegisterRichCommand(CommandZoomFitFull);
            //
            CommandShowOptionsDialog = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_ShowOptions,
                null,
                UserInterfaceStrings.Audio_ShowOptions_KEYS,
                null,
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandShowOptionsDialog", Category.Debug, Priority.Medium);

                    var shellPresenter_ = Container.Resolve<IShellPresenter>();
                    //var window = shellPresenter.View as Window;

                    var pane = new AudioOptions { DataContext = this };

                    var windowPopup = new PopupModalWindow(shellPresenter_,
                                                           UserInterfaceStrings.EscapeMnemonic(UserInterfaceStrings.Audio_ShowOptions),
                                                           pane,
                                                           PopupModalWindow.DialogButtonsSet.Close,
                                                           PopupModalWindow.DialogButton.Close,
                                                           true, 400, 500);
                    windowPopup.Show();
                },
                obj => true);

            shellPresenter.RegisterRichCommand(CommandShowOptionsDialog);
            //
            CommandFocus = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_Focus,
                null,
                UserInterfaceStrings.Audio_Focus_KEYS,
                null,
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandFocus", Category.Debug, Priority.Medium);

                    View.BringIntoFocus();
                },
                obj => View != null);

            shellPresenter.RegisterRichCommand(CommandFocus);
            //
        }

        private void initializeCommands_Edit()
        {
            var shellPresenter = Container.Resolve<IShellPresenter>();

            CommandOpenFile = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_OpenFile,
                UserInterfaceStrings.Audio_OpenFile_,
                UserInterfaceStrings.Audio_OpenFile_KEYS,
                shellPresenter.LoadTangoIcon("document-open"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandOpenFile", Category.Debug, Priority.Medium);

                    State.Audio.PcmFormatAlt = null;
                    openFile(obj as String, false, false);
                },
                obj => !IsWaveFormLoading && !IsMonitoring && !IsRecording);

            shellPresenter.RegisterRichCommand(CommandOpenFile);
            //
            CommandInsertFile = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_InsertFile,
                UserInterfaceStrings.Audio_InsertFile_,
                UserInterfaceStrings.Audio_InsertFile_KEYS,
                shellPresenter.LoadGnomeNeuIcon("Neu_go-jump"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandInsertFile", Category.Debug, Priority.Medium);

                    State.Audio.PcmFormatAlt = null;
                    openFile(obj as String, true, false);
                },
                obj =>
                {
                    var session = Container.Resolve<IUrakawaSession>();

                    return !IsWaveFormLoading && !IsPlaying && !IsMonitoring && !IsRecording
                        && session.DocumentProject != null && State.CurrentTreeNode != null
                        && IsAudioLoaded;
                });

            shellPresenter.RegisterRichCommand(CommandInsertFile);
            //
            CommandDeleteAudioSelection = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_Delete,
                UserInterfaceStrings.Audio_Delete_,
                UserInterfaceStrings.Audio_Delete_KEYS,
                shellPresenter.LoadGnomeNeuIcon("Neu_dialog-cancel"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandDeleteAudioSelection", Category.Debug, Priority.Medium);

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

                    if (listOfTreeNodeAndStreamSelection.Count == 0)
                    {
                        Debug.Fail("This should never happen !");
                        return;
                    }

                    var session = Container.Resolve<IUrakawaSession>();

                    if (listOfTreeNodeAndStreamSelection.Count == 1)
                    {
                        var command = session.DocumentProject.Presentations.Get(0).CommandFactory.
                                    CreateTreeNodeAudioStreamDeleteCommand(listOfTreeNodeAndStreamSelection[0]);

                        session.DocumentProject.Presentations.Get(0).UndoRedoManager.Execute(command);
                    }
                    else
                    {
                        session.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction("Delete spanning audio portion", "Delete a portion of audio that spans across several treenodes");

                        foreach (TreeNodeAndStreamSelection selection in listOfTreeNodeAndStreamSelection)
                        {
                            var command = session.DocumentProject.Presentations.Get(0).CommandFactory.
                                        CreateTreeNodeAudioStreamDeleteCommand(selection);

                            session.DocumentProject.Presentations.Get(0).UndoRedoManager.Execute(command);
                        }

                        session.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();
                    }
                },
                obj =>
                {
                    var session = Container.Resolve<IUrakawaSession>();

                    return !IsWaveFormLoading
                        && !IsPlaying && !IsMonitoring && !IsRecording
                        && session.DocumentProject != null
                        && State.CurrentTreeNode != null
                        && IsAudioLoaded && IsSelectionSet;
                });

            shellPresenter.RegisterRichCommand(CommandDeleteAudioSelection);
            //
        }

        private void initializeCommands()
        {
            Logger.Log("AudioPaneViewModel.initializeCommands", Category.Debug, Priority.Medium);

            initializeCommands_Selection();
            initializeCommands_Recorder();
            initializeCommands_Player();
            initializeCommands_View();
            initializeCommands_Edit();

            if (View != null)
            {
                View.InitGraphicalCommandBindings();
            }
        }

        #endregion Commands
    }
}
