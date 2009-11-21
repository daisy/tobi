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

namespace Tobi.Plugin.AudioPane
{
    public partial class AudioPaneViewModel
    {
        #region Commands

        public RichDelegateCommand CommandShowOptionsDialog { get; private set; }

        public RichDelegateCommand CommandFocus { get; private set; }
        public RichDelegateCommand CommandFocusStatusBar { get; private set; }
        public RichDelegateCommand CommandOpenFile { get; private set; }
        public RichDelegateCommand CommandInsertFile { get; private set; }
        public RichDelegateCommand CommandGotoBegining { get; private set; }
        public RichDelegateCommand CommandGotoEnd { get; private set; }
        public RichDelegateCommand CommandStepBack { get; private set; }
        public RichDelegateCommand CommandStepForward { get; private set; }
        public RichDelegateCommand CommandRewind { get; private set; }
        public RichDelegateCommand CommandFastForward { get; private set; }
        public RichDelegateCommand CommandSelectAll { get; private set; }
        public RichDelegateCommand CommandClearSelection { get; private set; }
        public RichDelegateCommand CommandZoomSelection { get; private set; }
        public RichDelegateCommand CommandZoomFitFull { get; private set; }
        public RichDelegateCommand CommandRefresh { get; private set; }
        public RichDelegateCommand CommandAutoPlay { get; private set; }
        public RichDelegateCommand CommandAudioSettings { get; private set; }
        public RichDelegateCommand CommandPlay { get; private set; }
        public RichDelegateCommand CommandPlayPreviewLeft { get; private set; }
        public RichDelegateCommand CommandPlayPreviewRight { get; private set; }
        public RichDelegateCommand CommandPause { get; private set; }
        public RichDelegateCommand CommandStartRecord { get; private set; }
        public RichDelegateCommand CommandStopRecord { get; private set; }
        public RichDelegateCommand CommandStartMonitor { get; private set; }
        public RichDelegateCommand CommandStopMonitor { get; private set; }
        public RichDelegateCommand CommandBeginSelection { get; private set; }
        public RichDelegateCommand CommandEndSelection { get; private set; }
        public RichDelegateCommand CommandSelectNextChunk { get; private set; }
        public RichDelegateCommand CommandSelectPreviousChunk { get; private set; }
        public RichDelegateCommand CommandDeleteAudioSelection { get; private set; }


        public IInputBindingManager InputBindingManager
        {
            get
            {
                var shellView = Container.Resolve<IShellView>();

                return shellView;
            }
        }

        private void initializeCommands_View()
        {
            var shellView = Container.Resolve<IShellView>();

            CommandRefresh = new RichDelegateCommand(
                UserInterfaceStrings.Audio_Reload,
                UserInterfaceStrings.Audio_Reload_,
                null,
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Refresh")),
                //shellView.LoadTangoIcon("view-refresh"),
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandRefresh", Category.Debug, Priority.Medium);

                    StartWaveFormLoadTimer(500, IsAutoPlay);
                },
                ()=> !IsWaveFormLoading); //IsAudioLoaded

            shellView.RegisterRichCommand(CommandRefresh);
            //
            CommandZoomSelection = new RichDelegateCommand(
                UserInterfaceStrings.Audio_ZoomSelection,
                UserInterfaceStrings.Audio_ZoomSelection_,
                UserInterfaceStrings.Audio_ZoomSelection_KEYS,
                ScalableGreyableImageProvider.ConvertIconFormat((DrawingImage)Application.Current.FindResource("Horizon_Image_Search")),
                //shellView.LoadTangoIcon("system-search"),
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandZoomSelection", Category.Debug, Priority.Medium);

                    View.ZoomSelection();
                },
                ()=> View != null && !IsWaveFormLoading && IsSelectionSet);

            shellView.RegisterRichCommand(CommandZoomSelection);
            //
            CommandZoomFitFull = new RichDelegateCommand(
                UserInterfaceStrings.Audio_FitFull,
                UserInterfaceStrings.Audio_FitFull_,
                UserInterfaceStrings.Audio_FitFull_KEYS,
                shellView.LoadTangoIcon("utilities-system-monitor"),
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandZoomFitFull", Category.Debug, Priority.Medium);

                    View.ZoomFitFull();
                },
                ()=> View != null && !IsWaveFormLoading);

            shellView.RegisterRichCommand(CommandZoomFitFull);
            //
            //
            CommandAudioSettings = new RichDelegateCommand(
                UserInterfaceStrings.Audio_Settings,
                UserInterfaceStrings.Audio_Settings_,
                null,
                shellView.LoadTangoIcon("audio-card"),
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandAudioSettings", Category.Debug, Priority.Medium);

                    var windowPopup = new PopupModalWindow(shellView,
                                                           UserInterfaceStrings.EscapeMnemonic(
                                                               UserInterfaceStrings.Audio_Settings),
                                                           new AudioSettings(this),
                                                           PopupModalWindow.DialogButtonsSet.Close,
                                                           PopupModalWindow.DialogButton.Close,
                                                           true, 500, 600);

                    windowPopup.ShowFloating(null);
                },
                ()=> true);

            shellView.RegisterRichCommand(CommandAudioSettings);
            //
            CommandShowOptionsDialog = new RichDelegateCommand(
                UserInterfaceStrings.Audio_ShowOptions,
                null,
                UserInterfaceStrings.Audio_ShowOptions_KEYS,
                null,
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandShowOptionsDialog", Category.Debug, Priority.Medium);

                    var shellView_ = Container.Resolve<IShellView>();
                    //var window = shellView.View as Window;

                    var pane = new AudioOptions { DataContext = this };

                    var windowPopup = new PopupModalWindow(shellView_,
                                                           UserInterfaceStrings.EscapeMnemonic(UserInterfaceStrings.Audio_ShowOptions),
                                                           pane,
                                                           PopupModalWindow.DialogButtonsSet.Close,
                                                           PopupModalWindow.DialogButton.Close,
                                                           true, 400, 500);
                    windowPopup.Show();
                },
                ()=> true);

            shellView.RegisterRichCommand(CommandShowOptionsDialog);
            //
            CommandFocus = new RichDelegateCommand(
                UserInterfaceStrings.Audio_Focus,
                null,
                UserInterfaceStrings.Audio_Focus_KEYS,
                null,
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandFocus", Category.Debug, Priority.Medium);

                    View.BringIntoFocus();
                },
                ()=> View != null);

            shellView.RegisterRichCommand(CommandFocus);
            //
            CommandFocusStatusBar = new RichDelegateCommand(
                UserInterfaceStrings.Audio_FocusStatusBar,
                null,
                UserInterfaceStrings.Audio_FocusStatusBar_KEYS,
                null,
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandFocusStatusBar", Category.Debug, Priority.Medium);

                    View.BringIntoFocusStatusBar();
                },
                ()=> View != null);

            shellView.RegisterRichCommand(CommandFocusStatusBar);
            //
        }

        private void initializeCommands_Edit()
        {
            var shellView = Container.Resolve<IShellView>();

            CommandOpenFile = new RichDelegateCommand(
                UserInterfaceStrings.Audio_OpenFile,
                UserInterfaceStrings.Audio_OpenFile_,
                UserInterfaceStrings.Audio_OpenFile_KEYS,
                shellView.LoadTangoIcon("document-open"),
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandOpenFile", Category.Debug, Priority.Medium);

                    State.Audio.PcmFormatAlt = null;
                    openFile(null, false, false);
                },
                ()=> !IsWaveFormLoading && !IsMonitoring && !IsRecording);

            shellView.RegisterRichCommand(CommandOpenFile);
            //
            CommandInsertFile = new RichDelegateCommand(
                UserInterfaceStrings.Audio_InsertFile,
                UserInterfaceStrings.Audio_InsertFile_,
                UserInterfaceStrings.Audio_InsertFile_KEYS,
                shellView.LoadGnomeNeuIcon("Neu_go-jump"),
                ()=>
                {
                    Logger.Log("AudioPaneViewModel.CommandInsertFile", Category.Debug, Priority.Medium);

                    State.Audio.PcmFormatAlt = null;
                    openFile(null, true, false);
                },
                ()=>
                {
                    var session = Container.Resolve<IUrakawaSession>();

                    return !IsWaveFormLoading && !IsPlaying && !IsMonitoring && !IsRecording
                        && session.DocumentProject != null && State.CurrentTreeNode != null
                        && IsAudioLoaded;
                });

            shellView.RegisterRichCommand(CommandInsertFile);
            //
            CommandDeleteAudioSelection = new RichDelegateCommand(
                UserInterfaceStrings.Audio_Delete,
                UserInterfaceStrings.Audio_Delete_,
                UserInterfaceStrings.Audio_Delete_KEYS,
                shellView.LoadGnomeNeuIcon("Neu_dialog-cancel"),
                ()=>
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
                ()=>
                {
                    var session = Container.Resolve<IUrakawaSession>();

                    return !IsWaveFormLoading
                        && !IsPlaying && !IsMonitoring && !IsRecording
                        && session.DocumentProject != null
                        && State.CurrentTreeNode != null
                        && IsAudioLoaded && IsSelectionSet;
                });

            shellView.RegisterRichCommand(CommandDeleteAudioSelection);
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
