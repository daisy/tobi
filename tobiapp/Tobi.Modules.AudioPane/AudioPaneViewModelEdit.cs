using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.command;
using urakawa.core;
using urakawa.daisy.import;
using urakawa.data;
using urakawa.media;
using urakawa.media.data.audio;
using urakawa.media.data.audio.codec;
using urakawa.media.timing;

namespace Tobi.Plugin.AudioPane
{

    public partial class AudioPaneViewModel
    {
        protected enum AudioSelectionOverwriteMode : ushort
        {
            DeleteAndInsertAtLeftEdge = 0,
            DeleteAndReplaceConsecutiveChunks = 1,
            DeleteAndMapStretchConsecutiveChunks = 2
        }

        // non-public feature, unless you know the keyboard shortcut
        public RichDelegateCommand CommandOpenFile { get; private set; }

        public RichDelegateCommand CommandEditWavExternally { get; private set; }
        public RichDelegateCommand CommandEditWavEffect1 { get; private set; }
        public RichDelegateCommand CommandEditWavEffect2 { get; private set; }

        public RichDelegateCommand CommandInsertFile { get; private set; }
        public RichDelegateCommand CommandGenTTS { get; private set; }
        public RichDelegateCommand CommandGenTTS_All { get; private set; }
        public RichDelegateCommand CommandDeleteAudioSelection { get; private set; }

        public RichDelegateCommand CommandSplitShift { get; private set; }

        public RichDelegateCommand CopyCommand { get; private set; }
        public RichDelegateCommand CutCommand { get; private set; }
        public RichDelegateCommand PasteCommand { get; private set; }

        public RichDelegateCommand CommandResetSessionCounter { get; private set; }

        public ManagedAudioMedia AudioClipboard { get; private set; }

        private void doEditAudio(int effect)
        {
            IsAutoPlay = false;

            if (!IsSelectionSet)
            {
                CommandSelectAll.Execute();
            }

            ManagedAudioMedia clipboard = AudioClipboard; // backup

            CopyCommand.Execute();

            ManagedAudioMedia selectedAudio = AudioClipboard;

            AudioClipboard = clipboard; // restore

            if (selectedAudio == null || !selectedAudio.HasActualAudioMediaData)
            {
                return;
            }

            DataProvider dataProvider = ((WavAudioMediaData)selectedAudio.AudioMediaData).ForceSingleDataProvider(false);
            //foreach (var dataProvider in selectedAudio.AudioMediaData.UsedDataProviders)
            //{


            //dataProvider.ExportDataStreamToFile(destinationPath, true);
            //File.Copy(fullPath, destinationPath);

            var dataFolderPath = selectedAudio.Presentation.DataProviderManager.DataFileDirectoryFullPath;

            string fullPath = ((FileDataProvider)dataProvider).DataFileFullPath;

            string destinationFolder = Path.Combine(dataFolderPath, Path.GetFileNameWithoutExtension(fullPath) + Path.DirectorySeparatorChar);
            if (!Directory.Exists(destinationFolder))
            {
                FileDataProvider.CreateDirectory(destinationFolder);
            }

            string destinationFile = Path.Combine(destinationFolder, Path.GetFileName(fullPath));
            if (File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
            }

            ((FileDataProvider)dataProvider).DeleteByMovingToFolder(destinationFolder);


            if (effect == 0)
            {
                var details = new TextBoxReadOnlyCaretVisible
                {
                    FocusVisualStyle = (Style) Application.Current.Resources["MyFocusVisualStyle"],

                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(6),
                    TextReadOnly = Tobi_Plugin_AudioPane_Lang.CmdAudioEditExternally_LongDesc
                };

                var label = new TextBlock
                {
                    Text = Path.GetFileName(destinationFile),
                    Margin = new Thickness(8, 0, 8, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Focusable = true,
                    TextWrapping = TextWrapping.Wrap
                };

                var iconProvider =
                    new ScalableGreyableImageProvider(m_ShellView.LoadTangoIcon("software-update-available"),
                        m_ShellView.MagnificationLevel);

                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                };
                panel.Children.Add(iconProvider.IconLarge);
                panel.Children.Add(label);
                panel.Margin = new Thickness(0, 0, 0, 16);

                var button = new Button()
                {
                    Content = Tobi_Plugin_AudioPane_Lang.OpenFileExplorer
                };
                //button.FocusVisualStyle = (Style)Application.Current.Resources["MyFocusVisualStyle"];
                button.Click += (s, e) =>
                {
                    m_ShellView.ExecuteShellProcess(destinationFolder);
                };

                var panel2 = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                };
                panel2.Children.Add(panel);
                panel2.Children.Add(button);

                //var details = new TextBoxReadOnlyCaretVisible
                //                  {
                //    TextReadOnly = Tobi_Lang.ExitConfirm
                //};

                var windowPopup = new PopupModalWindow(m_ShellView,
                    UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_AudioPane_Lang.CmdAudioEditExternally_ShortDesc),
                    panel2,
                    PopupModalWindow.DialogButtonsSet.OkCancel,
                    PopupModalWindow.DialogButton.Ok,
                    true, 300, 180, details, 40, null);

                windowPopup.ShowModal();

                if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
                {
                    if (File.Exists(destinationFile))
                    {
                        //var pcmFormat = m_viewModel.m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat;
                        //pcmFormat = selectedAudio.AudioMediaData.PCMFormat;
                        openFile(destinationFile, true, false, null);
                    }
                }
            }
            else if (effect == 1)
            {
                var effect1 = new WavSoundTouch(destinationFile, 0.8f
                    //, selectedAudio.AudioMediaData.PCMFormat.Data
                    );


                bool error = m_ShellView.RunModalCancellableProgressTask(true,
                    Tobi_Plugin_AudioPane_Lang.ProcessingAudio,
                    effect1,
                    () =>
                    {
                        Logger.Log(@"Audio WavSoundTouch CANCELED", Category.Debug, Priority.Medium);

                        m_ShellView.ExecuteShellProcess(destinationFolder);
                    },
                    () =>
                    {
                        Logger.Log(@"Audio WavSoundTouch DONE", Category.Debug, Priority.Medium);

                        Application.Current.MainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                            (Action)(() =>
                            {
                                try
                                {
                                    if (File.Exists(destinationFile))
                                    {
                                        openFile(destinationFile, true, false, null);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    m_ShellView.ExecuteShellProcess(destinationFolder);
                                }
                            }
                            ));
                    });
            }
            else if (effect == 2)
            {
                var effect2 = new WavSoundTouch(destinationFile, 1.5f
                    //, selectedAudio.AudioMediaData.PCMFormat.Data
                    );


                bool error = m_ShellView.RunModalCancellableProgressTask(true,
                    Tobi_Plugin_AudioPane_Lang.ProcessingAudio,
                    effect2,
                    () =>
                    {
                        Logger.Log(@"Audio WavSoundTouch CANCELED", Category.Debug, Priority.Medium);

                        m_ShellView.ExecuteShellProcess(destinationFolder);
                    },
                    () =>
                    {
                        Logger.Log(@"Audio WavSoundTouch DONE", Category.Debug, Priority.Medium);

                        Application.Current.MainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                            (Action)(() =>
                            {
                                try
                                {
                                    if (File.Exists(destinationFile))
                                    {
                                        openFile(destinationFile, true, false, null);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    m_ShellView.ExecuteShellProcess(destinationFolder);
                                }
                            }
                            ));
                    });
            }

            if (false) // will be cleaned-up at cleanup stage.
            {
                if (File.Exists(destinationFile))
                {
                    File.Delete(destinationFile);
                }
                if (Directory.Exists(destinationFolder))
                {
                    FileDataProvider.DeleteDirectory(destinationFolder);
                }
            }

            //    break;
            //}
        }

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
                () => !IsWaveFormLoading
                    && (!IsMonitoring || IsMonitoringAlways)
                    && !IsRecording,
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
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_FileInsert));

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
                       && (!IsMonitoring || IsMonitoringAlways)
                       && !IsRecording
                       && !IsWaveFormLoading
                       && !IsPlaying
                       && m_UrakawaSession.DocumentProject != null
                       && (IsSimpleMode || treeNodeSelection.Item1.HasXmlProperty)
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
                    //&& node.HasXmlProperty
                    //&& node.GetFirstAncestorWithManagedAudio() == null
                    //&& node.GetFirstDescendantWithManagedAudio() == null;
                }
                      ,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_GenTTS));

            m_ShellView.RegisterRichCommand(CommandGenTTS);
            //
            //
            CommandGenTTS_All = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioGenTTS_All_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioGenTTS_All_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("audio-x-generic"),
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandGenTTS_All", Category.Debug, Priority.Medium);

                    CommandGenTTS_All_Execute();
                },
                () =>
                {
                    //Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();

                    //bool okay = treeNodeSelection.Item1 != null
                    //    && (
                    //    IsSimpleMode
                    //    ? TreeNode.GetLengthStringChunks(treeNodeSelection.Item1.GetTextFlattened_()) > 0
                    //    : true
                    //        )
                    //        ;

                    return
                        //okay &&
                        (!IsMonitoring || IsMonitoringAlways)
                       && !IsRecording
                       && !IsWaveFormLoading
                       && !IsPlaying
                       && m_UrakawaSession.DocumentProject != null
                        //&& (IsSimpleMode || treeNodeSelection.Item1.HasXmlProperty)
                        //&& treeNodeSelection.Item1.GetFirstAncestorWithManagedAudio() == null
                        //&& treeNodeSelection.Item1.GetFirstDescendantWithManagedAudio() == null
                       ;

                    //// because we change the selection in the execute code
                    ////return CommandInsertFile.CanExecute();

                    // Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
                    // TreeNode node = treeNodeSelection.Item2 ?? treeNodeSelection.Item1;

                    // return !IsMonitoring && !IsRecording && !IsWaveFormLoading && !IsPlaying
                    //     && m_UrakawaSession.DocumentProject != null
                    //&& node != null
                    //&& node.HasXmlProperty
                    //&& node.GetFirstAncestorWithManagedAudio() == null
                    //&& node.GetFirstDescendantWithManagedAudio() == null;
                }
                      ,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_GenTTS_All));

            m_ShellView.RegisterRichCommand(CommandGenTTS_All);
            //
            CommandDeleteAudioSelection = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioDelete_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioDelete_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("emblem-unreadable"), //edit-delete  audio-volume-muted
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
                    return !IsWaveFormLoading
                        && !IsPlaying
                        && (!IsMonitoring || IsMonitoringAlways)
                        && !IsRecording
                         && State.Audio.PlayStreamMarkers != null && IsSelectionSet && State.Audio.HasContent;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Audio_Delete));

            m_ShellView.RegisterRichCommand(CommandDeleteAudioSelection);
            //
            //
            CommandEditWavExternally = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioEditExternally_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioEditExternally_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon("software-update-available"), //mail-send-receive
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandEditWavExternally", Category.Debug, Priority.Medium);

                    doEditAudio(0);
                },
                () =>
                {
                    return !IsWaveFormLoading
                        && !IsPlaying
                        && (!IsMonitoring || IsMonitoringAlways)
                        && !IsRecording
                         && State.Audio.PlayStreamMarkers != null
                        //&& IsSelectionSet
                         && State.Audio.HasContent;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Edit_Audio_Externally));

            m_ShellView.RegisterRichCommand(CommandEditWavExternally);
            ////
            CommandEditWavEffect1 = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioEditEffect1_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioEditEffect1_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadTangoIcon("software-update-available"), //mail-send-receive
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandEditWavEffect1", Category.Debug, Priority.Medium);

                    doEditAudio(1);
                },
                () =>
                {
                    return !IsWaveFormLoading
                        && !IsPlaying
                        && (!IsMonitoring || IsMonitoringAlways)
                        && !IsRecording
                         && State.Audio.PlayStreamMarkers != null
                        //&& IsSelectionSet
                         && State.Audio.HasContent;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Edit_Audio_Effect1));

            m_ShellView.RegisterRichCommand(CommandEditWavEffect1);
            //
            CommandEditWavEffect2 = new RichDelegateCommand(
                Tobi_Plugin_AudioPane_Lang.CmdAudioEditEffect2_ShortDesc,
                Tobi_Plugin_AudioPane_Lang.CmdAudioEditEffect2_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                null, //m_ShellView.LoadTangoIcon("software-update-available"), //mail-send-receive
                () =>
                {
                    Logger.Log("AudioPaneViewModel.CommandEditWavEffect2", Category.Debug, Priority.Medium);

                    doEditAudio(2);
                },
                () =>
                {
                    return !IsWaveFormLoading
                        && !IsPlaying
                        && (!IsMonitoring || IsMonitoringAlways)
                        && !IsRecording
                         && State.Audio.PlayStreamMarkers != null
                        //&& IsSelectionSet
                         && State.Audio.HasContent;
                },
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Edit_Audio_Effect2));

            m_ShellView.RegisterRichCommand(CommandEditWavEffect2);
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

                    OnInterruptAudioPlayerRecorder(); //  CHANGES IsAutoPlay to FALSE
                    //CommandPause.Execute();

                    //TreeNode nested;
                    TreeNode next = null;
                    {
                        Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
                        TreeNode treeNode = treeNodeSelection.Item2 ?? treeNodeSelection.Item1;

                    tryNext:

                        //next = TreeNode.GetNextTreeNodeWithNoSignificantTextOnlySiblings(false, treeNode, out nested);
                        next = TreeNode.NavigatePreviousNextSignificantText(false, treeNode);
                        if (next != null)
                        {
                            TreeNode math = next.GetFirstAncestorWithXmlElement("math");
                            if (math != null)
                            {
                                next = math;
                            }
                            else
                            {
                                TreeNode svg = next.GetFirstAncestorWithXmlElement("svg");
                                if (svg != null)
                                {
                                    next = svg;
                                }
                                else
                                {
                                    TreeNode candidate = m_UrakawaSession.AdjustTextSyncGranularity(next, treeNode);
                                    if (candidate != null)
                                    {
                                        next = candidate;
                                    }
                                }
                            }

                            if (Settings.Default.Audio_EnableSkippability && m_UrakawaSession.isTreeNodeSkippable(next))
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
                    OnInterruptAudioPlayerRecorder();

                    m_UrakawaSession.PerformTreeNodeSelection(next, false, null); //nested);
                    OnInterruptAudioPlayerRecorder();

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
                    if (isAutoPlay || Settings.Default.Audio_AutoPlayAfterSplit)
                    {
                        OnInterruptAudioPlayerRecorder(); //  CHANGES IsAutoPlay to FALSE
                        IsAutoPlay = true;
                        CommandPlay.Execute();
                    }
                },
                () =>
                {
                    return !IsWaveFormLoading
                        && (!IsMonitoring || IsMonitoringAlways)
                        && !IsRecording
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
                   && (IsSimpleMode || node.HasXmlProperty)
                   && node.GetFirstAncestorWithManagedAudio() == null
                   && node.GetFirstDescendantWithManagedAudio() == null
                //&& node.GetManagedAudioMedia() == null
                   ;
        }


        internal void openFile(TreeNode treeNode, FileDataProvider dataProv, long byteBegin, long byteEnd, AudioLibPCMFormat pcmInfo, long pcmDataLength)
        {
            Logger.Log("AudioPaneViewModel.OpenFile_FileDataProvider", Category.Debug, Priority.Medium);

            CommandPause.Execute();

            if (byteBegin < 0) byteBegin = 0;
            if (byteEnd <= 0) byteEnd = pcmDataLength;

            ManagedAudioMedia managedAudioMedia = treeNode.Presentation.MediaFactory.CreateManagedAudioMedia();

            var mediaData = (WavAudioMediaData)treeNode.Presentation.MediaDataFactory.CreateAudioMediaData();

            managedAudioMedia.AudioMediaData = mediaData;

            if (byteBegin == 0 && byteEnd == pcmDataLength)
            {
                mediaData.AppendPcmData(dataProv);
            }
            else
            {
                mediaData.AppendPcmData(
                    dataProv,
                new Time(pcmInfo.ConvertBytesToTime(byteBegin)),
                (byteEnd == pcmDataLength ? null : new Time(pcmInfo.ConvertBytesToTime(byteEnd)))
                );
            }

            if (managedAudioMedia.Duration.AsMilliseconds > 500)
                insertAudioAtCursorOrSelectionReplace(managedAudioMedia);
            else
                Console.WriteLine(@"audio clip too short to be inserted ! " + managedAudioMedia.Duration.AsLocalUnits / AudioLibPCMFormat.TIME_UNIT + @"ms");

        }



        internal bool? openFile(String str, bool insert, bool deleteAfterInsert, PCMFormatInfo pcmInfo)
        {
            Logger.Log("AudioPaneViewModel.OpenFile", Category.Debug, Priority.Medium);

            CommandPause.Execute();

            string filePath = str;

            if (String.IsNullOrEmpty(filePath) && View != null)
            {
                string[] filePaths = View.OpenFileDialog();

                if (filePaths == null || filePaths.Length == 0)
                {
                    filePath = null;
                }
                else if (filePaths.Length == 1 || !insert)
                {
                    filePath = filePaths[0];
                }
                else
                {
                    DebugFix.Assert(insert);

                    bool audioCues = Tobi.Common.Settings.Default.EnableAudioCues;
                    Tobi.Common.Settings.Default.EnableAudioCues = false;

                    long selectionBegin = -1;
                    long selectionEnd = -1;

                    bool? ok = true;

                    if (false) // reverse insert
                    {
                        for (int i = filePaths.Length - 1; i >= 0; i--)
                        {
                            string path = filePaths[i];

                            ok = openFile(path, insert, deleteAfterInsert, pcmInfo);
                            if (ok == null) // cancelled
                            {
                                break;
                            }

                            //TODO adjust selection and playback cursor
                        }
                    }
                    else
                    {
                        IsAutoPlay = false;

                        for (int i = 0; i < filePaths.Length; i++)
                        {
                            string path = filePaths[i];

                            ok = openFile(path, insert, deleteAfterInsert, pcmInfo);
                            if (ok == null) // cancelled
                            {
                                break;
                            }

                            if (selectionBegin == -1)
                            {
                                if (State.Selection.SelectionBeginBytePosition >= 0)
                                {
                                    selectionBegin = State.Selection.SelectionBeginBytePosition;
                                }
                                else
                                {
                                    selectionBegin = 0;
                                }
                            }

                            if (State.Selection.SelectionBeginBytePosition >= 0 && State.Selection.SelectionEndBytePosition > 0)
                            {
                                PlayBytePosition = State.Selection.SelectionEndBytePosition;
                            }
                            else
                            {
                                DebugFix.Assert(false);
                                CommandGotoEnd.Execute();
                            }

                            selectionEnd = PlayBytePosition;

                            CommandClearSelection.Execute();
                        }
                    }

                    if (selectionBegin >= 0 && selectionEnd > 0)
                    {
                        State.Selection.SetSelectionBytes(selectionBegin, selectionEnd);
                    }
                    else
                    {
                        DebugFix.Assert(false);
                        CommandSelectAll.Execute();
                    }

                    if (State.Selection.SelectionBeginBytePosition >= 0 && State.Selection.SelectionEndBytePosition > 0)
                    {
                        PlayBytePosition = State.Selection.SelectionEndBytePosition;
                    }

                    Tobi.Common.Settings.Default.EnableAudioCues = audioCues;
                    return ok;
                }
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            string ext = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(ext))
            {
                return false;
            }

            bool isWav = ext.Equals(DataProviderFactory.AUDIO_WAV_EXTENSION, StringComparison.OrdinalIgnoreCase);

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

                    bool error = m_ShellView.RunModalCancellableProgressTask(true,
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
                        //DebugFix.Assert(!result);
                        return null;
                    }

                    filePath = converter.ConvertedFilePath;
                    if (string.IsNullOrEmpty(filePath))
                    {
                        return false;
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

                if (managedAudioMedia.Duration.AsMilliseconds > 140)
                {
                    insertAudioAtCursorOrSelectionReplace(managedAudioMedia);

                    IsAutoPlay = false;

                    if (State.Audio.HasContent)
                    {
                        if (State.Selection.SelectionBeginBytePosition >= 0 && State.Selection.SelectionEndBytePosition > 0)
                        {
                            PlayBytePosition = State.Selection.SelectionEndBytePosition;
                        }
                        else
                        {
                            DebugFix.Assert(false);
                            CommandGotoEnd.Execute();
                        }
                    }

                    return true;
                }
                else
                {
                    Console.WriteLine(@"audio clip too short to be inserted ! " + managedAudioMedia.Duration.AsLocalUnits / AudioLibPCMFormat.TIME_UNIT + @"ms");

                    return false;
                }
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

                bool error = m_ShellView.RunModalCancellableProgressTask(true,
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
                    //DebugFix.Assert(!result);
                    return null;
                }

                filePath = converter.ConvertedFilePath;
                if (string.IsNullOrEmpty(filePath))
                {
                    return false;
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

            return true;
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

            if (State.Audio.PlayStreamMarkers == null
                || m_TTSGen)
            {
                if (AudioPlaybackStreamKeepAlive)
                {
                    ensurePlaybackStreamIsDead();
                }

                //DebugFix.Assert(State.Audio.PlayStream == null);

#if ENABLE_SEQ_MEDIA
                var media = treeNode.GetManagedAudioMediaOrSequenceMedia();
#else
                var media = treeNode.GetManagedAudioMedia();
#endif
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

            //TODO: should this be configurable?
            AudioSelectionOverwriteMode audioSelectionOverwriteMode =
                AudioSelectionOverwriteMode.DeleteAndMapStretchConsecutiveChunks;

            if (IsSelectionSet)
            {
                bytePositionInsert = State.Selection.SelectionBeginBytePosition;

                transaction = true;
                treeNode.Presentation.UndoRedoManager.StartTransaction(Tobi_Plugin_AudioPane_Lang.TransactionReplaceAudio_ShortDesc, Tobi_Plugin_AudioPane_Lang.TransactionReplaceAudio_LongDesc);

                selData = getAudioSelectionData();

                List<Time> selDataCachedDurations = null;
                if (selData != null && selData.Count > 0)
                {
                    selDataCachedDurations = new List<Time>(selData.Count);
                    foreach (var treeNodeAndStreamSelection in selData)
                    {
                        ManagedAudioMedia mm = treeNodeAndStreamSelection.m_TreeNode.GetManagedAudioMedia();
                        if (mm == null)
                        {
#if DEBUG
                            Debugger.Break();
#endif
                            selDataCachedDurations.Add(new Time(0));
                        }
                        else
                        {
                            selDataCachedDurations.Add(mm.AudioMediaData.AudioDuration);
                        }
                    }
                }

                CommandDeleteAudioSelection.Execute();

                bool allWasDeleted = false;
                if (selData != null)
                {
                    bool atLeastOneWasNotDeleted = false;
                    foreach (var treeNodeAndStreamSelection in selData)
                    {
#if ENABLE_SEQ_MEDIA
                        bool deleted = treeNodeAndStreamSelection.m_TreeNode.GetManagedAudioMediaOrSequenceMedia() == null;
#else
                        bool deleted = treeNodeAndStreamSelection.m_TreeNode.GetManagedAudioMedia() == null;
#endif
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

                if (audioSelectionOverwriteMode == AudioSelectionOverwriteMode.DeleteAndInsertAtLeftEdge)
                {
                    if (allWasDeleted) //!State.Audio.HasContent)
                    {
                        if (selData != null && selData.Count > 0)
                        {
                            TreeNode treeNode_ = selData[0].m_TreeNode;
                            var command_ =
                                treeNode.Presentation.CommandFactory.CreateTreeNodeSetManagedAudioMediaCommand(
                                    treeNode_, manMedia, treeNodeSelection.Item1);
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
                }
                else //DeleteAndMapStretchConsecutiveChunks OR AudioSelectionOverwriteMode.DeleteAndReplaceConsecutiveChunks
                {
                    if (AudioPlaybackStreamKeepAlive)
                    {
                        ensurePlaybackStreamIsDead();
                    }

                    if (selData != null && selData.Count > 0)
                    {
                        Time minimumTimePerChunk = new Time(manMedia.AudioMediaData.PCMFormat.Data.ConvertBytesToTime(manMedia.AudioMediaData.PCMFormat.Data.BlockAlign));

                        double ratio = 1.0;
                        if (audioSelectionOverwriteMode == AudioSelectionOverwriteMode.DeleteAndMapStretchConsecutiveChunks)
                        {
                            Time selDuration = new Time(0);
                            //foreach (var treeNodeAndStreamSelection in selData)
                            //{
                            //    ManagedAudioMedia selManMed =
                            //        treeNodeAndStreamSelection.m_TreeNode.GetManagedAudioMedia();
                            //    if (selManMed != null)
                            //    {
                            //        selDuration.Add(selManMed.AudioMediaData.AudioDuration);
                            //    }
                            //}
                            foreach (var dur in selDataCachedDurations)
                            {
                                selDuration.Add(dur);
                            }

                            if (selDuration.AsLocalUnits > 0)
                            {
                                ratio = manMedia.AudioMediaData.AudioDuration.AsLocalUnits / (double)selDuration.AsLocalUnits; // new divided by old
                            }
                        }

                        Time timeOffset = null;

                        int count = -1;
                        foreach (var treeNodeAndStreamSelection in selData)
                        {
                            count++;

                            TreeNode treeNode_ = treeNodeAndStreamSelection.m_TreeNode;

                            //        Time timeBegin = treeNodeAndStreamSelection.m_LocalStreamLeftMark == -1
                            //? Time.Zero
                            //: new Time(manMedia.AudioMediaData.PCMFormat.Data.ConvertBytesToTime(treeNodeAndStreamSelection.m_LocalStreamLeftMark));
                            Time timeBegin = Time.Zero;

                            if (timeOffset != null)
                            {
                                timeBegin.Add(timeOffset);
                            }

                            if (timeBegin.IsGreaterThanOrEqualTo(manMedia.AudioMediaData.AudioDuration))
                            {
                                break;
                            }

                            Time implicitEnd = selDataCachedDurations[count];
                            if (treeNodeAndStreamSelection.m_LocalStreamLeftMark != -1 &&
                                treeNodeAndStreamSelection.m_LocalStreamLeftMark != 0)
                            {
                                //implicitEnd = new Time(selDataCachedDurations[count]);
                                //implicitEnd.Substract(
                                //    new Time(
                                //        manMedia.AudioMediaData.PCMFormat.Data.ConvertBytesToTime(
                                //            treeNodeAndStreamSelection.m_LocalStreamLeftMark)));

                                implicitEnd = new Time(selDataCachedDurations[count].AsLocalUnits - manMedia.AudioMediaData.PCMFormat.Data.ConvertBytesToTime(treeNodeAndStreamSelection.m_LocalStreamLeftMark));
                            }
                            Time timeEnd = treeNodeAndStreamSelection.m_LocalStreamRightMark == -1
                    ? implicitEnd
                    : new Time(manMedia.AudioMediaData.PCMFormat.Data.ConvertBytesToTime(treeNodeAndStreamSelection.m_LocalStreamRightMark));

                            if (audioSelectionOverwriteMode == AudioSelectionOverwriteMode.DeleteAndMapStretchConsecutiveChunks)
                            {
                                timeEnd = new Time((long)Math.Floor((double)timeEnd.AsLocalUnits * ratio));
                            }

                            if (timeOffset != null)
                            {
                                timeEnd.Add(timeOffset);
                            }

                            timeOffset = timeEnd;

                            if (timeEnd.GetDifference(timeBegin).IsLessThanOrEqualTo(minimumTimePerChunk))
                            {
                                continue;
                            }

                            bool lastOne = false;
                            if (timeEnd.IsGreaterThanOrEqualTo(manMedia.AudioMediaData.AudioDuration)
                                || count == selData.Count - 1 // use up the entire provided audio, so there's no data loss
                                )
                            {
                                timeEnd = manMedia.AudioMediaData.AudioDuration; //.Copy(); no need
                                lastOne = true;
                            }

                            ManagedAudioMedia manMediaChunk = null;

                            if (timeBegin.IsEqualTo(Time.Zero) &&
                             (
                             timeEnd.IsEqualTo(Time.Zero)
                             ||
                             timeEnd.GetDifference(timeBegin).IsEqualTo(manMedia.AudioMediaData.AudioDuration)
                             ))
                            {
                                manMediaChunk = manMedia.Copy();
                            }
                            else
                            {
                                WavAudioMediaData mediaDataChunk = ((WavAudioMediaData)manMedia.AudioMediaData).Copy(timeBegin, timeEnd);

                                manMediaChunk = treeNode_.Presentation.MediaFactory.CreateManagedAudioMedia();
                                manMediaChunk.AudioMediaData = mediaDataChunk;
                            }

                            //long byteCount = treeNodeAndStreamSelection.m_LocalStreamRightMark - treeNodeAndStreamSelection.m_LocalStreamLeftMark;

                            //long timeLocalUnits = manMedia.AudioMediaData.PCMFormat.Data.ConvertBytesToTime(byteCount);
                            //long timeMS = timeLocalUnits/AudioLibPCMFormat.TIME_UNIT;

                            //AudioMediaData mediaDataChunk = treeNode_.Presentation.MediaDataFactory.CreateAudioMediaData();
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

                            ManagedAudioMedia selManMedia = treeNode_.GetManagedAudioMedia();

                            if (selManMedia == null)
                            {
                                var command_ =
                                    treeNode.Presentation.CommandFactory.CreateTreeNodeSetManagedAudioMediaCommand(
                                        treeNode_, manMediaChunk, treeNodeSelection.Item1);
                                treeNode.Presentation.UndoRedoManager.Execute(command_);
                            }
                            else
                            {
#if DEBUG
                                // middle wavform chunks cannot possibly be partially-deleted!
                                DebugFix.Assert(count == 0 || count == selData.Count - 1);
#endif
                                long lastByte = selManMedia.AudioMediaData.PCMFormat.Data.ConvertTimeToBytes(
                                    selManMedia.AudioMediaData.AudioDuration.AsLocalUnits);

                                var command_ = treeNode.Presentation.CommandFactory.
                                       CreateManagedAudioMediaInsertDataCommand(
                                           treeNode_, manMediaChunk,
                                           count == 0 ? lastByte : 0,
                                           treeNodeSelection.Item1);

                                treeNode.Presentation.UndoRedoManager.Execute(command_);
                            }

                            if (lastOne) break;
                        }
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

#if ENABLE_SEQ_MEDIA
                    var media = treeNode.GetManagedAudioMediaOrSequenceMedia();
#else
                    var media = treeNode.GetManagedAudioMedia();
#endif
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
                if (transaction)
                {
                    treeNode.Presentation.UndoRedoManager.EndTransaction();
                }
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
