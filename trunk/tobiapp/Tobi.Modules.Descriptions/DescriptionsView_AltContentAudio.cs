using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite;
using Microsoft.Practices.Composite.Events;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using Tobi.Plugin.AudioPane;
using Tobi.Plugin.Urakawa;
using urakawa;
using urakawa.core;
using urakawa.data;
using urakawa.exception;
using urakawa.media;
using urakawa.media.data.audio;
using urakawa.property.alt;
using urakawa.property.channel;

namespace Tobi.Plugin.Descriptions
{
    public partial class DescriptionsView
    {

        // WE NEED TO CAPTURE TOGGLE COMMANDS SUCH AS PLAY/PAUSE, RECORD, MONITOR (same KeyGesture, different Commands).
        private PopupModalWindow m_AudioPopupModalWindow;

        private void OnClick_ButtonAddEditAudio(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;


            Application.Current.MainWindow.Cursor = Cursors.Wait;
            this.Cursor = Cursors.Wait; //m_ShellView


            if (AudioMediaElement.Clock != null)
            {
                AudioMediaElement.Clock.Controller.Stop();
            }



            var pres = m_Session.DocumentProject.Presentations.Get(0);

            var project = new Project();
            project.SetPrettyFormat(m_Session.DocumentProject.IsPrettyFormat());

            // a proxy project/presentation/treenode (and UrakawaSession wrapper) to bridge the standard audio recording feature, without altering the main document.
            var presentation = new Presentation();
            presentation.Project = project;
            presentation.RootUri = pres.RootUri;
            int index = pres.DataProviderManager.DataFileDirectory.IndexOf(DataProviderManager.DefaultDataFileDirectorySeparator + DataProviderManager.DefaultDataFileDirectory);
            string prefix = pres.DataProviderManager.DataFileDirectory.Substring(0, index);
            string suffix = "--IMAGE_DESCRIPTIONS_TEMP_AUDIO";
            //DebugFix.Assert(Path.GetFileName(pres.RootUri.LocalPath) == prefix);
            presentation.DataProviderManager.SetDataFileDirectoryWithPrefix(prefix + suffix);
            presentation.MediaDataManager.DefaultPCMFormat = pres.MediaDataManager.DefaultPCMFormat.Copy();
            presentation.MediaDataManager.EnforceSinglePCMFormat = true;

            //DebugFix.Assert(presentation.DataProviderManager.DataFileDirectoryFullPath == pres.DataProviderManager.DataFileDirectoryFullPath + suffix);


            project.Presentations.Insert(0, presentation);

            var treeNode = presentation.TreeNodeFactory.Create();
            presentation.RootNode = treeNode;

            if (altContent.Audio != null)
            {
                var audioChannel = presentation.ChannelFactory.CreateAudioChannel();
                audioChannel.Name = "The DESCRIPTION Audio Channel";

                ManagedAudioMedia audio1 = presentation.MediaFactory.CreateManagedAudioMedia();
                AudioMediaData audioData1 = presentation.MediaDataFactory.CreateAudioMediaData();
                audio1.AudioMediaData = audioData1;

                // WARNING: WavAudioMediaData implementation differs from AudioMediaData:
                // the latter is naive and performs a stream binary copy, the latter is optimized and re-uses existing WavClips. 
                //  WARNING 2: The audio data from the given parameter gets emptied !
                //audio1.AudioMediaData.MergeWith(manMedia.AudioMediaData);

                if (!audio1.AudioMediaData.PCMFormat.Data.IsCompatibleWith(altContent.Audio.AudioMediaData.PCMFormat.Data))
                {
                    throw new InvalidDataFormatException(
                        "Can not merge description audio with a AudioMediaData with incompatible audio data");
                }
                Stream stream = altContent.Audio.AudioMediaData.OpenPcmInputStream();
                try
                {
                    audio1.AudioMediaData.AppendPcmData(stream, null); //manMedia.AudioMediaData.AudioDuration
                }
                finally
                {
                    stream.Close();
                }

                ChannelsProperty chProp = presentation.RootNode.GetOrCreateChannelsProperty();
                chProp.SetMedia(audioChannel, audio1);
            }

            if (altContent.Text != null)
            {
                var textChannel = presentation.ChannelFactory.CreateTextChannel();
                textChannel.Name = "The DESCRIPTION Text Channel";

                TextMedia text1 = presentation.MediaFactory.CreateTextMedia();
                text1.Text = altContent.Text.Text;

                ChannelsProperty chProp = presentation.RootNode.GetOrCreateChannelsProperty();
                chProp.SetMedia(textChannel, text1);
            }

            presentation.RootNode.XukInAfter_TextMediaCache();

            var audioEventAggregator = new EventAggregator();

            var audioSession = new UrakawaSession(
                m_Logger,
                m_Container,
                audioEventAggregator, //m_EventAggregator,
                this //m_ShellView
                );
            audioSession.DocumentProject = project;

            var audioViewModel = new AudioPaneViewModel(
                m_Logger,
                audioEventAggregator, //m_EventAggregator,
                this, //m_ShellView,
                audioSession
                );
            audioViewModel.IsSimpleMode = true;
            audioViewModel.InputBindingManager = this; //m_ShellView
            //m_audioViewModel.PlaybackRate = xxx; TODO: copy from main document session

            var audioView = new AudioPaneView(
                m_Logger,
                audioEventAggregator, //m_EventAggregator,
                audioViewModel,
                this //m_ShellView
                );

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                  UserInterfaceStrings.EscapeMnemonic("(audio) " + Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_ShortDesc),
                                                  audioView,
                                                  PopupModalWindow.DialogButtonsSet.OkCancel,
                                                  PopupModalWindow.DialogButton.Cancel,
                                                  true, 850, 320, null, 0);

            windowPopup.IgnoreEscape = true;

            //UIElement win = TryFindParent<Window>(this);

            // WE HAND PICK THE COMMAND KEY BINDINGS INSTEAD OF RELYING ON AUTO REGISTRATION!

            windowPopup.AddInputBinding(audioViewModel.CommandPause.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandPlay.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandStopMonitor.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandStartMonitor.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandStopRecord.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandStartRecord.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandInsertFile.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandPlayPreviewLeft.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandPlayPreviewRight.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandRewind.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandFastForward.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandGotoBegining.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandGotoEnd.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandDeleteAudioSelection.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandSelectAll.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandSelectLeft.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandSelectRight.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandClearSelection.KeyBinding);

            windowPopup.AddInputBinding(audioViewModel.CommandZoomFitFull.KeyBinding);
            windowPopup.AddInputBinding(audioViewModel.CommandZoomSelection.KeyBinding);

            windowPopup.AddInputBinding(audioSession.UndoCommand.KeyBinding);
            windowPopup.AddInputBinding(audioSession.RedoCommand.KeyBinding);

            //var bindings = Application.Current.MainWindow.InputBindings;
            //foreach (var binding in bindings)
            //{
            //    if (binding is KeyBinding)
            //    {
            //        var keyBinding = (KeyBinding)binding;
            //        if (keyBinding.Command == m_ShellView.ExitCommand)
            //        {
            //            continue;
            //        }
            //        windowPopup.AddInputBinding(keyBinding);
            //    }
            //}

            //windowPopup.InputBindings.AddRange(Application.Current.MainWindow.InputBindings);

            windowPopup.KeyUp += (object o, KeyEventArgs ev) =>
            {
                var key = (ev.Key == Key.System
                                ? ev.SystemKey
                                : (ev.Key == Key.ImeProcessed ? ev.ImeProcessedKey : ev.Key));

                if (key == Key.Escape)
                {
                    audioEventAggregator.GetEvent<EscapeEvent>().Publish(null);
                }
            };

            windowPopup.Closed += (o, ev) => Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                (Action)(() =>
                {
                    //
                }));

            //presentation.UndoRedoManager.StartTransaction
            //    ("(AUDIO) " + Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_ShortDesc,
            //    "(AUDIO) " + Tobi_Plugin_Descriptions_Lang.CmdEditDescriptions_LongDesc);



            windowPopup.Loaded += (o, ev) => Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                (Action)(() =>
                {
                    audioViewModel.OnProjectLoaded(audioSession.DocumentProject);

                    //Tuple<TreeNode, TreeNode> treeNodeSelection = m_Session.GetTreeNodeSelection();
                    //--
                    audioSession.PerformTreeNodeSelection(audioSession.DocumentProject.Presentations.Get(0).RootNode);
                    //--
                    //m_audioSession.ForceInitTreeNodeSelection(m_audioSession.DocumentProject.Presentations.Get(0).RootNode);
                    //
                    //var treeNodeSelection = new Tuple<TreeNode, TreeNode>(m_audioSession.DocumentProject.Presentations.Get(0).RootNode, null);
                    //var oldTreeNodeSelection = new Tuple<TreeNode, TreeNode>(treeNodeSelection.Item1.Parent, null);
                    //var tuple = new Tuple<Tuple<TreeNode, TreeNode>, Tuple<TreeNode, TreeNode>>(oldTreeNodeSelection, treeNodeSelection);
                    //m_audioViewModel.OnTreeNodeSelectionChanged(tuple);

                }));


            Application.Current.MainWindow.Cursor = Cursors.Arrow;
            this.Cursor = Cursors.Arrow; //m_ShellView

            m_AudioPopupModalWindow = windowPopup;
            windowPopup.ShowModal();
            m_AudioPopupModalWindow = null;

            audioViewModel.OnProjectUnLoaded(audioSession.DocumentProject);

            //bool empty = m_audioSession.DocumentProject.Presentations.Get(0).UndoRedoManager.IsTransactionEmpty;



            if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Ok)
            {
                //presentation.UndoRedoManager.EndTransaction();

                //if (DescriptionsListView.SelectedIndex < 0) return;
                //AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

                // Can be null (or empty audio media data), but that's ok.
                ManagedAudioMedia manMedia_ = presentation.RootNode.GetManagedAudioMedia();

                m_ViewModel.SetDescriptionAudio(altContent, manMedia_);

                DescriptionsListView.Items.Refresh();

                //presentation.UndoRedoManager.Undo();
            }
            else
            {
                //presentation.UndoRedoManager.CancelTransaction();
            }

            //while (presentation.UndoRedoManager.CanUndo)
            //{
            //    presentation.UndoRedoManager.Undo();
            //}

            presentation.UndoRedoManager.FlushCommands();

            ManagedAudioMedia manMedia = presentation.RootNode.GetManagedAudioMedia();
            if (manMedia != null)
            {
                manMedia.AudioMediaData = null;
            }

            string deletedDataFolderPath = audioSession.DataCleanup(false);
            string[] files = Directory.GetFiles(deletedDataFolderPath);
            if (files.Length != 0)
            {
                //m_ShellView.ExecuteShellProcess(deletedDataFolderPath);

                //TODO: delete containing folder(s) ?
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }

            resetAudioPlayer();
        }

        private void OnClick_ButtonClearAudio(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            if (AudioMediaElement.Clock != null)
            {
                AudioMediaElement.Clock.Controller.Stop();
            }

            m_ViewModel.SetDescriptionAudio(altContent, null);

            DescriptionsListView.Items.Refresh();
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public void DispatchPropertyChangedEvent(PropertyChangedEventArgs e)
        {
            m_ShellView.DispatchPropertyChangedEvent(e);
        }

        public RichDelegateCommand ExitCommand
        {
            get { return m_ShellView.ExitCommand; }
        }

        public void RaiseEscapeEvent()
        {
            m_ShellView.RaiseEscapeEvent();
        }

        public IActiveAware ActiveAware
        {
            get { return m_ShellView.ActiveAware; }
        }

        public event EventHandler DeviceRemoved;
        public event EventHandler DeviceArrived;

        public void PumpDispatcherFrames(DispatcherPriority prio)
        {
            m_ShellView.PumpDispatcherFrames(prio);
        }

        public bool IsUIAutomationDisabled
        {
            get { return m_ShellView.IsUIAutomationDisabled; }
        }

        public void Show()
        {
            m_ShellView.Show();
        }

        public bool SplitterDrag
        {
            get { return m_ShellView.SplitterDrag; }
        }

        public double MagnificationLevel
        {
            get { return m_ShellView.MagnificationLevel; }
            set { m_ShellView.MagnificationLevel = value; }
        }

        public VisualBrush LoadTangoIcon(string resourceKey)
        {
            return m_ShellView.LoadTangoIcon(resourceKey);
        }

        public VisualBrush LoadGnomeNeuIcon(string resourceKey)
        {
            return m_ShellView.LoadGnomeNeuIcon(resourceKey);
        }

        public VisualBrush LoadGnomeGionIcon(string resourceKey)
        {
            return m_ShellView.LoadGnomeGionIcon(resourceKey);
        }

        public VisualBrush LoadGnomeFoxtrotIcon(string resourceKey)
        {
            return m_ShellView.LoadGnomeFoxtrotIcon(resourceKey);
        }

        public void DimBackgroundWhile(Action action)
        {
            m_ShellView.DimBackgroundWhile(action);
        }

        public void ExecuteShellProcess(string shellCmd)
        {
            m_ShellView.ExecuteShellProcess(shellCmd);
        }

        public bool RunModalCancellableProgressTask(bool inSeparateThread, string title, IDualCancellableProgressReporter reporter, Action actionCancelled, Action actionCompleted)
        {
            return m_ShellView.RunModalCancellableProgressTask(inSeparateThread, title, reporter, actionCancelled, actionCompleted);
        }

        public void RegisterRichCommand(RichDelegateCommand command)
        {
            //AddInputBinding(command.KeyBinding);
        }
        public bool AddInputBinding(InputBinding inputBinding)
        {
            if (m_AudioPopupModalWindow != null)
            {
                return m_AudioPopupModalWindow.AddInputBinding(inputBinding);
            }
            return true;
        }
        public void RemoveInputBinding(InputBinding inputBinding)
        {
            if (m_AudioPopupModalWindow != null)
            {
                m_AudioPopupModalWindow.RemoveInputBinding(inputBinding);
            }
        }

        //        public void RemoveSubInputBindingManager(IInputBindingManager ibm)
        //        {
        //#if DEBUG
        //            Debugger.Break();
        //#endif // DEBUG
        //        }

        //        public void AddSubInputBindingManager(IInputBindingManager ibm)
        //        {
        //#if DEBUG
        //            Debugger.Break();
        //#endif // DEBUG
        //        }

        private void resetAudioPlayer()
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetProperty<AlternateContentProperty>();
            if (altProp == null) return;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;



            if (AudioMediaElement.Clock != null)
            {
                AudioMediaElement.Clock.Controller.Stop();
                AudioMediaElement.Clock.Controller.Remove();

                AudioMediaElement.Close();
                AudioMediaElement.Clock = null;
            }


            //if (AudioMediaElement.Source != null)
            //{
            //    AudioMediaElement.Close();
            //    AudioMediaElement.Source = null;
            //}


            m_SliderValueChangeFromCode = true;
            AudioTimeSlider.Value = 0;

            if (altContent.Audio == null)
            {
                AudioTimeSlider.Maximum = 100;
                return;
            }

            string path = null;
            foreach (var dataProv in altContent.Audio.AudioMediaData.UsedDataProviders)
            {
                path = ((FileDataProvider)dataProv).DataFileFullPath;
                break;
            }
            if (String.IsNullOrEmpty(path))
            {
                return;
            }


            var mediaTimeline = new MediaTimeline(new Uri(path));
            mediaTimeline.CurrentTimeInvalidated += new EventHandler(mediaTimeline_CurrentTimeInvalidated);
            AudioMediaElement.Clock = mediaTimeline.CreateClock(true) as MediaClock;
            AudioMediaElement.Clock.Controller.Stop();

            //AudioMediaElement.Source = new Uri(path);
            //AudioMediaElement.Clock.CurrentTimeInvalidated += new EventHandler(mediaTimeline_CurrentTimeInvalidated);

            //if (mediaPlayer == null)
            //{
            //    mediaPlayer = new MediaPlayer();
            //    mediaPlayer.Volume = 1;
            //    mediaPlayer.MediaOpened += new EventHandler(AudioElement_MediaOpened);
            //    mediaPlayer.MediaEnded += new EventHandler(AudioElement_MediaEnded);
            //}
            //mediaPlayer.Open(new Uri(path));

        }

        //private MediaPlayer mediaPlayer;

        private bool m_SliderValueChangeFromCode;

        private void mediaTimeline_CurrentTimeInvalidated(object sender, EventArgs e)
        {
            if (m_SliderDragging) return;

            if (AudioMediaElement.Clock == null) return;

            if (AudioMediaElement.Clock.CurrentState == ClockState.Filling)
            {
                AudioElement_MediaEnded(null, null);
                return;
            }

            if (AudioMediaElement.Clock.CurrentTime.HasValue)
            {
                m_SliderValueChangeFromCode = true;
                AudioTimeSlider.Value = AudioMediaElement.Clock.CurrentTime.Value.TotalMilliseconds;
            }
        }

        private void AudioElement_MediaOpened(object sender, EventArgs e)
        {
            if (AudioMediaElement.NaturalDuration.HasTimeSpan)
            {
                AudioTimeSlider.Maximum = AudioMediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;
            }

            if (AudioMediaElement.Clock == null) return;

            AudioMediaElement.Clock.Controller.Stop();
        }

        private void AudioElement_MediaEnded(object sender, EventArgs e)
        {
            if (AudioMediaElement.Clock == null) return;

            AudioMediaElement.Clock.Controller.Stop();

            m_SliderValueChangeFromCode = true;
            AudioTimeSlider.Value = 0;
        }


        private bool m_SliderDragging = false;

        private void OnDragCompleted_AudioTimeSlider(object sender, DragCompletedEventArgs e)
        {
            if (!AudioMediaElement.NaturalDuration.HasTimeSpan)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    resetAudioPlayer();
                }));

                return;
            }

            if (AudioMediaElement.Clock == null) return;

            if (AudioMediaElement.Clock.CurrentState == ClockState.Filling)
            {
                AudioMediaElement.Clock.Controller.Stop();
                AudioMediaElement.Clock.Controller.Pause();
            }

            int SliderValue = (int)AudioTimeSlider.Value;

            TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);

            //AudioMediaElement.Clock.Controller.SeekAlignedToLastTick(ts, TimeSeekOrigin.BeginTime);
            AudioMediaElement.Clock.Controller.Seek(ts, TimeSeekOrigin.BeginTime);

            m_SliderDragging = false;
        }

        private void OnDragStarted_AudioTimeSlider(object sender, DragStartedEventArgs e)
        {
            if (AudioMediaElement.Clock == null) return;

            if (!AudioMediaElement.NaturalDuration.HasTimeSpan)
            {
                AudioMediaElement.Clock.Controller.Stop();
                return;
            }


            if (AudioMediaElement.Clock.CurrentState == ClockState.Active)
            {
                if (AudioMediaElement.Clock.IsPaused || AudioMediaElement.Clock.CurrentGlobalSpeed == 0.0)
                {
                }
                else
                {
                    AudioMediaElement.Clock.Controller.Pause();
                }
            }
            else if (AudioMediaElement.Clock.CurrentState == ClockState.Filling)
            {
                AudioMediaElement.Clock.Controller.Stop();
                AudioMediaElement.Clock.Controller.Pause();
            }

            m_SliderDragging = true;
        }

        private void OnAudioTimeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (m_SliderValueChangeFromCode)
            {
                m_SliderValueChangeFromCode = false;
                return;
            }

            OnDragStarted_AudioTimeSlider(null, null);
            OnDragCompleted_AudioTimeSlider(null, null);
        }

        private void OnClick_ButtonAudioPlayPause(object sender, RoutedEventArgs e)
        {
            if (AudioMediaElement.Clock == null) return;

            if (!AudioMediaElement.NaturalDuration.HasTimeSpan)
            {
                AudioMediaElement.Clock.Controller.Stop();
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    resetAudioPlayer();
                }));

                return;
            }

            if (AudioMediaElement.Clock.CurrentState == ClockState.Active)
            {
                if (//AudioMediaElement.Clock.IsPaused ||
                    AudioMediaElement.Clock.CurrentGlobalSpeed == 0.0)
                {
                    AudioMediaElement.Clock.Controller.Resume();
                }
                else
                {
                    AudioMediaElement.Clock.Controller.Pause();
                }
            }
            else if (AudioMediaElement.Clock.CurrentState == ClockState.Stopped)
            {
                AudioMediaElement.Clock.Controller.Begin();
            }
            else if (AudioMediaElement.Clock.CurrentState == ClockState.Filling)
            {
                AudioElement_MediaEnded(null, null);
            }
        }
    }
}
