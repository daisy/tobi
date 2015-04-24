using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
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
using urakawa.daisy;
using urakawa.data;
using urakawa.exception;
using urakawa.media;
using urakawa.media.timing;
using urakawa.media.data.audio;
using urakawa.property.alt;
using urakawa.property.channel;
using System.Diagnostics;

#if ENABLE_WPF_MEDIAKIT
using WPFMediaKit.DirectShow.Controls;
using urakawa.xuk;
using MediaState = System.Windows.Controls.MediaState;
using WPFMediaKit.DirectShow.MediaPlayers;
#endif //ENABLE_WPF_MEDIAKIT

namespace Tobi.Plugin.Descriptions
{
    public partial class DescriptionsView
    {
        private void ShowAudio(string diagramElementName)
        {
            ShowAdvanced(diagramElementName, 0);
        }

        private void ShowAdvanced(string diagramElementName, int target)
        {
            m_ViewModel.ShowAdvancedEditor = true;
            TabItem_Descriptions.IsSelected = true;
            if (target == 0)
            {
                TabItem_Audio.IsSelected = true;
            }
            else if (target == 1)
            {
                TabItem_Text.IsSelected = true;
            }
            else if (target == 2)
            {
                TabItem_Image.IsSelected = true;
            }
            else
            {
                TabItem_Attributes.IsSelected = true;
            }

            AlternateContent altContent = m_ViewModel.GetAltContent(diagramElementName);
            if (altContent == null)
            {
                string uid = m_ViewModel.GetNewXmlID(diagramElementName.Replace(':', '_'));
                altContent = addNewDescription(uid, diagramElementName);
            }

            if (altContent != null)
            {
                DescriptionsListView.SelectedItem = altContent;
            }

            //FocusHelper.Focus(ButtonAddEditAudio);

            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)
                (() =>
                {
                    var item = DescriptionsListView.ItemContainerGenerator.ContainerFromIndex(DescriptionsListView.SelectedIndex)
                        as ListViewItem;
                    if (item != null)
                    {
                        FocusHelper.Focus(item);
                    }
                }));
        }

        private void OnClick_ButtonAdvanced_LongDesc(object sender, RoutedEventArgs e)
        {
            ShowAdvanced(DiagramContentModelHelper.D_LondDesc, 1);
        }
        private void OnClick_ButtonAdvanced_Summary(object sender, RoutedEventArgs e)
        {
            ShowAdvanced(DiagramContentModelHelper.D_Summary, 1);
        }
        private void OnClick_ButtonAdvanced_SimplifiedLang(object sender, RoutedEventArgs e)
        {
            ShowAdvanced(DiagramContentModelHelper.D_SimplifiedLanguageDescription, 1);
        }


        private void OnClick_ButtonAdvanced_TactileImage(object sender, RoutedEventArgs e)
        {
            ShowAdvanced(DiagramContentModelHelper.D_Tactile, 2);
        }
        private void OnClick_ButtonAdvanced_TactileImage_Text(object sender, RoutedEventArgs e)
        {
            ShowAdvanced(DiagramContentModelHelper.D_Tactile, 1);
        }

        private void OnClick_ButtonAdvanced_SimplifiedImage(object sender, RoutedEventArgs e)
        {
            ShowAdvanced(DiagramContentModelHelper.D_SimplifiedImage, 2);
        }
        private void OnClick_ButtonAdvanced_SimplifiedImage_Text(object sender, RoutedEventArgs e)
        {
            ShowAdvanced(DiagramContentModelHelper.D_SimplifiedImage, 1);
        }



        private void OnClick_ButtonAudio_LongDesc(object sender, RoutedEventArgs e)
        {
            ShowAudio(DiagramContentModelHelper.D_LondDesc);
        }
        private void OnClick_ButtonNoAudio_LongDesc(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonAudio_LongDesc(sender, e);
        }

        private void OnClick_ButtonAudio_Summary(object sender, RoutedEventArgs e)
        {
            ShowAudio(DiagramContentModelHelper.D_Summary);
        }
        private void OnClick_ButtonNoAudio_Summary(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonAudio_Summary(sender, e);
        }

        private void OnClick_ButtonAudio_SimplifiedLanguage(object sender, RoutedEventArgs e)
        {
            ShowAudio(DiagramContentModelHelper.D_SimplifiedLanguageDescription);
        }
        private void OnClick_ButtonNoAudio_SimplifiedLanguage(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonAudio_SimplifiedLanguage(sender, e);
        }


        private void OnClick_ButtonAudio_TactileImage(object sender, RoutedEventArgs e)
        {
            ShowAudio(DiagramContentModelHelper.D_Tactile);
        }
        private void OnClick_ButtonNoAudio_TactileImage(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonAudio_TactileImage(sender, e);
        }

        private void OnClick_ButtonAudio_SimplifiedImage(object sender, RoutedEventArgs e)
        {
            ShowAudio(DiagramContentModelHelper.D_SimplifiedImage);
        }
        private void OnClick_ButtonNoAudio_SimplifiedImage(object sender, RoutedEventArgs e)
        {
            OnClick_ButtonAudio_SimplifiedImage(sender, e);
        }

        private PopupModalWindow m_DescriptionPopupModalWindow;

        // WE NEED TO CAPTURE TOGGLE COMMANDS SUCH AS PLAY/PAUSE, RECORD, MONITOR (same KeyGesture, different Commands).
        private PopupModalWindow m_AudioPopupModalWindow;

        private string extractHumanText(string str)
        {
            string text = str;
            text = Regex.Replace(text, @"<[^<^>]+>", " ");
            text = text.Trim();
            text = Regex.Replace(text, @"\s+", " ");
            return text;
        }

        private void OnClick_ButtonAddEditAudio(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetAlternateContentProperty();
            if (altProp == null) return;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;


            Application.Current.MainWindow.Cursor = Cursors.Wait;
            this.Cursor = Cursors.Wait; //m_ShellView

            try
            {
                stopAudioPlayer();



                var pres = m_Session.DocumentProject.Presentations.Get(0);

                var project = new Project();
                bool pretty = m_Session.DocumentProject.PrettyFormat;
                project.PrettyFormat = pretty;

                // a proxy project/presentation/treenode (and UrakawaSession wrapper) to bridge the standard audio recording feature, without altering the main document.
                var presentation = new Presentation();
                presentation.Project = project;
                presentation.RootUri = pres.RootUri;

                int index = pres.DataProviderManager.DataFileDirectory.IndexOf(DataProviderManager.DefaultDataFileDirectorySeparator + DataProviderManager.DefaultDataFileDirectory);

                string prefix = null;
                if (index <= 0)
                {
                    prefix = pres.DataProviderManager.DataFileDirectory;
                }
                else
                {
                    prefix = pres.DataProviderManager.DataFileDirectory.Substring(0, index);
                }

                string suffix = "_Img";
                //DebugFix.Assert(Path.GetFileName(pres.RootUri.LocalPath) == prefix);
                presentation.DataProviderManager.SetCustomDataFileDirectory(prefix + suffix);
                presentation.MediaDataManager.DefaultPCMFormat = pres.MediaDataManager.DefaultPCMFormat;//.Copy();
                //presentation.MediaDataManager.EnforceSinglePCMFormat = true;

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
                    text1.Text = extractHumanText(altContent.Text.Text);

                    ChannelsProperty chProp = presentation.RootNode.GetOrCreateChannelsProperty();
                    chProp.SetMedia(textChannel, text1);
                }

                //presentation.RootNode.XukInAfter_TextMediaCache();

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
                                                      true, 850, 320, null, 0, m_DescriptionPopupModalWindow);

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

                windowPopup.AddInputBinding(audioViewModel.CommandGenTTS.KeyBinding);


                windowPopup.AddInputBinding(audioViewModel.CommandPlaybackRateUp.KeyBinding);
                windowPopup.AddInputBinding(audioViewModel.CommandPlaybackRateDown.KeyBinding);
                windowPopup.AddInputBinding(audioViewModel.CommandPlaybackRateReset.KeyBinding);

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
                m_AudioPopupModalWindow.ShowModal();
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
                    manMedia.AudioMediaData = null; // so that this ManagedAudioMedia gets cleaned up (we copied the WAV above at m_ViewModel.SetDescriptionAudio(altContent, manMedia_)) 
                }

                string deletedDataFolderPath = audioSession.DataCleanup(false);
                string[] files = Directory.GetFiles(deletedDataFolderPath);
                if (files.Length != 0)
                {
                    //m_ShellView.ExecuteShellProcess(deletedDataFolderPath);

                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }
                }

                files = Directory.GetFiles(deletedDataFolderPath);
                if (files.Length == 0)
                {
                    FileDataProvider.DeleteDirectory(deletedDataFolderPath);
                }

                string dir = presentation.DataProviderManager.DataFileDirectoryFullPath;
                files = Directory.GetFiles(dir);
                if (files.Length == 0)
                {
                    FileDataProvider.DeleteDirectory(dir);
                }


                resetAudioPlayer();
            }
            finally
            {
                //// XukStrings maintains a pointer to the last-created Project instance!
                //XukStrings.RelocateProjectReference(m_Session.DocumentProject);

                Application.Current.MainWindow.Cursor = Cursors.Arrow;
                this.Cursor = Cursors.Arrow; //m_ShellView
            }
        }

        private void OnClick_ButtonClearAudio(object sender, RoutedEventArgs e)
        {
            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            stopAudioPlayer();

            m_ViewModel.SetDescriptionAudio(altContent, null);

            DescriptionsListView.Items.Refresh();

            resetAudioPlayer();
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

        public bool Activate()
        {
            return m_ShellView.Activate();
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

        public void DimBackgroundWhile(Action action, Window owner)
        {
            m_ShellView.DimBackgroundWhile(action, owner);
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


        private MediaElement medElement_WINDOWS_MEDIA_PLAYER = null;
#if ENABLE_WPF_MEDIAKIT
        private MediaUriElement medElement_MEDIAKIT_DIRECTSHOW = null;
#endif //ENABLE_WPF_MEDIAKIT


        private Action actionUpdateSliderFromVideoTime = null;
        private bool doNotUpdateVideoTimeWhenSliderChanges = false;
        private DispatcherTimer _timer = null;



        private void resetAudioPlayer()
        {
            stopAudioPlayer();

            bool thereWasAMediaElement = false;

            if (medElement_WINDOWS_MEDIA_PLAYER != null)
            {
                thereWasAMediaElement = true;
                medElement_WINDOWS_MEDIA_PLAYER.Close();
                medElement_WINDOWS_MEDIA_PLAYER = null;
            }

#if ENABLE_WPF_MEDIAKIT
            if (medElement_MEDIAKIT_DIRECTSHOW != null)
            {
                thereWasAMediaElement = true;
                medElement_MEDIAKIT_DIRECTSHOW.Close();
                medElement_MEDIAKIT_DIRECTSHOW = null;
            }
#endif //ENABLE_WPF_MEDIAKIT


            if (thereWasAMediaElement)
            {
                AudioMediaElementContainer.Children.RemoveAt(0);
            }

            AudioTimeSlider.Value = 0;
            AudioTimeLabel.Text = "";


            if (DescriptionsListView.SelectedIndex < 0) return;
            AlternateContent altContent = (AlternateContent)DescriptionsListView.SelectedItem;

            Tuple<TreeNode, TreeNode> selection = m_Session.GetTreeNodeSelection();
            TreeNode node = selection.Item2 ?? selection.Item1;
            if (node == null) return;

            var altProp = node.GetAlternateContentProperty();
            if (altProp == null) return;

            if (altProp.AlternateContents.IndexOf(altContent) < 0) return;

            if (altContent.Audio == null)
            {
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

            var uri = new Uri(path);


#if ENABLE_WPF_MEDIAKIT
            if (Common.Settings.Default.EnableMediaKit)
            {
                medElement_MEDIAKIT_DIRECTSHOW = new MediaUriElement();
            }
            else
#endif //ENABLE_WPF_MEDIAKIT
            {
                medElement_WINDOWS_MEDIA_PLAYER = new MediaElement();
            }





#if ENABLE_WPF_MEDIAKIT
            DebugFix.Assert((medElement_WINDOWS_MEDIA_PLAYER == null) == (medElement_MEDIAKIT_DIRECTSHOW != null));
#else  // DISABLE_WPF_MEDIAKIT
            DebugFix.Assert(medElement_WINDOWS_MEDIA_PLAYER != null);
#endif //ENABLE_WPF_MEDIAKIT


            if (medElement_WINDOWS_MEDIA_PLAYER != null)
            {
                medElement_WINDOWS_MEDIA_PLAYER.Stretch = Stretch.Uniform;
                medElement_WINDOWS_MEDIA_PLAYER.StretchDirection = StretchDirection.DownOnly;
            }

#if ENABLE_WPF_MEDIAKIT
            if (medElement_MEDIAKIT_DIRECTSHOW != null)
            {
                medElement_MEDIAKIT_DIRECTSHOW.Stretch = Stretch.Uniform;
                medElement_MEDIAKIT_DIRECTSHOW.StretchDirection = StretchDirection.DownOnly;
            }
#endif //ENABLE_WPF_MEDIAKIT



            FrameworkElement mediaElement = null;
            if (medElement_WINDOWS_MEDIA_PLAYER != null)
            {
                mediaElement = medElement_WINDOWS_MEDIA_PLAYER;
            }
            else
            {
                mediaElement = medElement_MEDIAKIT_DIRECTSHOW;
            }

            mediaElement.Focusable = false;

            mediaElement.Visibility = Visibility.Collapsed;

            AudioMediaElementContainer.Children.Insert(0, mediaElement);

            var actionMediaFailed = new Action<string>(
                (str) =>
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        (Action)(() =>
                        {
                            DebugFix.Assert(false);
                        }
                        ));
                }
                );


            actionUpdateSliderFromVideoTime = null;
            _timer = new DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 0, 250);
            _timer.Stop();
            _timer.Tick += (object oo, EventArgs ee) =>
            {
                actionUpdateSliderFromVideoTime.Invoke();
            };



            if (medElement_WINDOWS_MEDIA_PLAYER != null)
            {
                medElement_WINDOWS_MEDIA_PLAYER.ScrubbingEnabled = true;

                medElement_WINDOWS_MEDIA_PLAYER.LoadedBehavior = MediaState.Manual;
                medElement_WINDOWS_MEDIA_PLAYER.UnloadedBehavior = MediaState.Stop;


                doNotUpdateVideoTimeWhenSliderChanges = false;
                actionUpdateSliderFromVideoTime = new Action(() =>
                {
                    if (medElement_WINDOWS_MEDIA_PLAYER == null) return;

                    TimeSpan? timeSpan = medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentTime;
                    double timeMS = timeSpan != null ? timeSpan.Value.TotalMilliseconds : 0;

                    //Console.WriteLine("UPDATE: " + timeMS);

                    //if (medElement_WINDOWS_MEDIA_PLAYER.NaturalDuration.HasTimeSpan
                    //    && timeMS >= medElement_WINDOWS_MEDIA_PLAYER.NaturalDuration.TimeSpan.TotalMilliseconds - 50)
                    //{
                    //    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Stop();
                    //}

                    doNotUpdateVideoTimeWhenSliderChanges = true;
                    AudioTimeSlider.Value = timeMS;
                });

                medElement_WINDOWS_MEDIA_PLAYER.MediaFailed += new EventHandler<ExceptionRoutedEventArgs>(
                    (oo, ee) =>
                    {
                        if (medElement_WINDOWS_MEDIA_PLAYER == null) return;

                        //#if DEBUG
                        //                                Debugger.Break();
                        //#endif //DEBUG
                        //medElement_WINDOWS_MEDIA_PLAYER.Source
                        actionMediaFailed.Invoke(uri.ToString()
                            + " \n("
                            + (ee.ErrorException != null ? ee.ErrorException.Message : "ERROR!")
                            + ")");
                    }
                    );



                medElement_WINDOWS_MEDIA_PLAYER.MediaOpened += new RoutedEventHandler(
                    (oo, ee) =>
                    {
                        if (medElement_WINDOWS_MEDIA_PLAYER == null) return;

                        double durationMS = medElement_WINDOWS_MEDIA_PLAYER.NaturalDuration.TimeSpan.TotalMilliseconds;
                        AudioTimeLabel.Text = Time.Format_Standard(medElement_WINDOWS_MEDIA_PLAYER.NaturalDuration.TimeSpan);

                        AudioTimeSlider.Maximum = durationMS;


                        // freeze frame (poster)
                        if (medElement_WINDOWS_MEDIA_PLAYER.LoadedBehavior == MediaState.Manual)
                        {
                            medElement_WINDOWS_MEDIA_PLAYER.IsMuted = true;

                            medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Begin();
                            medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();

                            medElement_WINDOWS_MEDIA_PLAYER.IsMuted = false;

                            AudioTimeSlider.Value = 0.10;
                        }
                    }
                    );



                medElement_WINDOWS_MEDIA_PLAYER.MediaEnded +=
                    new RoutedEventHandler(
                    (oo, ee) =>
                    {
                        if (medElement_WINDOWS_MEDIA_PLAYER == null) return;

                        _timer.Stop();

                        medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Stop();

                        actionUpdateSliderFromVideoTime.Invoke();
                    }
                    );
            }












#if ENABLE_WPF_MEDIAKIT
            if (medElement_MEDIAKIT_DIRECTSHOW != null)
            {
                doNotUpdateVideoTimeWhenSliderChanges = false;
                actionUpdateSliderFromVideoTime = new Action(() =>
                {
                    if (medElement_MEDIAKIT_DIRECTSHOW == null) return;

                    long timeVideo = medElement_MEDIAKIT_DIRECTSHOW.MediaPosition;

                    //if (timeMS >= medElement_MEDIAKIT_DIRECTSHOW.MediaDuration - 50 * 10000.0)
                    //{
                    //    medElement_MEDIAKIT_DIRECTSHOW.Stop();
                    //}


                    double timeMS = timeVideo / 10000.0;

                    //Console.WriteLine("UPDATE: " + timeMS);

                    doNotUpdateVideoTimeWhenSliderChanges = true;
                    AudioTimeSlider.Value = timeMS;
                });


                medElement_MEDIAKIT_DIRECTSHOW.MediaFailed += new EventHandler<WPFMediaKit.DirectShow.MediaPlayers.MediaFailedEventArgs>(
                    (oo, ee) =>
                    {
                        if (medElement_MEDIAKIT_DIRECTSHOW == null) return;

                        //#if DEBUG
                        //                        Debugger.Break();
                        //#endif //DEBUG

                        //medElement_MEDIAKIT_DIRECTSHOW.Source
                        actionMediaFailed.Invoke(uri.ToString()
                            + " \n("
                            + (ee.Exception != null ? ee.Exception.Message : ee.Message)
                            + ")");
                    }
                        );



                medElement_MEDIAKIT_DIRECTSHOW.MediaOpened += new RoutedEventHandler(
                    (oo, ee) =>
                    {
                        if (medElement_MEDIAKIT_DIRECTSHOW == null) return;

                        long durationVideo = medElement_MEDIAKIT_DIRECTSHOW.MediaDuration;
                        if (durationVideo == 0)
                        {
                            return;
                        }

                        //MediaPositionFormat mpf = medElement.CurrentPositionFormat;
                        //MediaPositionFormat.MediaTime
                        double durationMS = durationVideo / 10000.0;

                        AudioTimeSlider.Maximum = durationMS;

                        var durationTimeSpan = new TimeSpan(0, 0, 0, 0, (int)Math.Round(durationMS));
                        AudioTimeLabel.Text = Time.Format_Standard(durationTimeSpan);


                        // freeze frame (poster)
                        if (medElement_MEDIAKIT_DIRECTSHOW.LoadedBehavior == WPFMediaKit.DirectShow.MediaPlayers.MediaState.Manual)
                        {
                            if (false)
                            {
                                double volume = medElement_MEDIAKIT_DIRECTSHOW.Volume;
                                medElement_MEDIAKIT_DIRECTSHOW.Volume = 0;

                                medElement_MEDIAKIT_DIRECTSHOW.Play();
                                AudioTimeSlider.Value = 0.10;
                                medElement_MEDIAKIT_DIRECTSHOW.Pause();

                                medElement_MEDIAKIT_DIRECTSHOW.Volume = volume;
                            }
                            else
                            {
                                medElement_MEDIAKIT_DIRECTSHOW.Pause();
                                AudioTimeSlider.Value = 0.10;
                            }
                        }
                    }
                    );



                medElement_MEDIAKIT_DIRECTSHOW.MediaEnded +=
                    new RoutedEventHandler(
                    (oo, ee) =>
                    {
                        if (medElement_MEDIAKIT_DIRECTSHOW == null) return;

                        _timer.Stop();
                        medElement_MEDIAKIT_DIRECTSHOW.Pause();
                        actionUpdateSliderFromVideoTime.Invoke();

                        // TODO: BaseClasses.cs in WPF Media Kit,
                        // MediaPlayerBase.OnMediaEvent
                        // ==> remove StopGraphPollTimer();
                        // in case EventCode.Complete.


                        //m_DocumentPaneView.Dispatcher.BeginInvoke(
                        //    DispatcherPriority.Background,
                        //    (Action)(() =>
                        //    {
                        //        //medElement_MEDIAKIT_DIRECTSHOW.BeginInit();
                        //        medElement_MEDIAKIT_DIRECTSHOW.Source = uri;
                        //        //medElement_MEDIAKIT_DIRECTSHOW.EndInit();
                        //    })
                        //    );
                    }
                    );


                medElement_MEDIAKIT_DIRECTSHOW.MediaClosed +=
                    new RoutedEventHandler(
                    (oo, ee) =>
                    {
                        if (medElement_MEDIAKIT_DIRECTSHOW == null) return;

                        int debug = 1;
                    }
                    );


                //DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(
                //    MediaSeekingElement.MediaPositionProperty,
                //    typeof(MediaSeekingElement));
                //if (dpd != null)
                //{
                //    dpd.AddValueChanged(medElement_MEDIAKIT_DIRECTSHOW, new EventHandler((o, e) =>
                //    {
                //        //actionRefreshTime.Invoke();

                //        //if (!_timer.IsEnabled)
                //        //{
                //        //    _timer.Start();
                //        //}
                //    }));
                //}

            }
#endif //ENABLE_WPF_MEDIAKIT




            if (medElement_WINDOWS_MEDIA_PLAYER != null)
            {
                var timeline = new MediaTimeline();
                timeline.Source = uri;

                medElement_WINDOWS_MEDIA_PLAYER.Clock = timeline.CreateClock(true) as MediaClock;

                medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Stop();

                //medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentTimeInvalidated += new EventHandler(
                //(o, e) =>
                //{
                //    //actionRefreshTime.Invoke();
                //    //if (!_timer.IsEnabled)
                //    //{
                //    //    _timer.Start();
                //    //}
                //});

            }

#if ENABLE_WPF_MEDIAKIT
            if (medElement_MEDIAKIT_DIRECTSHOW != null)
            {
                medElement_MEDIAKIT_DIRECTSHOW.BeginInit();

                medElement_MEDIAKIT_DIRECTSHOW.Loop = false;
                medElement_MEDIAKIT_DIRECTSHOW.VideoRenderer = VideoRendererType.VideoMixingRenderer9;

                // seems to be a multiplicator of 10,000 to get milliseconds
                medElement_MEDIAKIT_DIRECTSHOW.PreferedPositionFormat = MediaPositionFormat.MediaTime;


                medElement_MEDIAKIT_DIRECTSHOW.LoadedBehavior = WPFMediaKit.DirectShow.MediaPlayers.MediaState.Manual;
                medElement_MEDIAKIT_DIRECTSHOW.UnloadedBehavior = WPFMediaKit.DirectShow.MediaPlayers.MediaState.Stop;

                try
                {
                    medElement_MEDIAKIT_DIRECTSHOW.Source = uri;

                    medElement_MEDIAKIT_DIRECTSHOW.EndInit();
                }
                catch (Exception ex)
                {
#if DEBUG
                    Debugger.Break();
#endif //DEBUG
                    ; // swallow (reported in MediaFailed)
                }
            }
#endif //ENABLE_WPF_MEDIAKIT
        }

        private bool wasPlayingBeforeDrag = false;
        private void OnDragStarted_AudioTimeSlider(object sender, DragStartedEventArgs e)
        {
            if (medElement_WINDOWS_MEDIA_PLAYER != null)
            {
                wasPlayingBeforeDrag = false;

                //Is Active
                if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentState == ClockState.Active)
                {
                    //Is Paused
                    if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentGlobalSpeed == 0.0)
                    {

                    }
                    else //Is Playing
                    {
                        wasPlayingBeforeDrag = true;
                        medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                    }
                }
                else if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentState == ClockState.Stopped)
                {
                    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Begin();
                    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                }
            }

#if ENABLE_WPF_MEDIAKIT
            if (medElement_MEDIAKIT_DIRECTSHOW != null)
            {
                wasPlayingBeforeDrag = medElement_MEDIAKIT_DIRECTSHOW.IsPlaying;

                if (wasPlayingBeforeDrag)
                {
                    medElement_MEDIAKIT_DIRECTSHOW.Pause();
                }
            }
#endif //ENABLE_WPF_MEDIAKIT

        }

        private void OnDragCompleted_AudioTimeSlider(object sender, DragCompletedEventArgs e)
        {
            if (medElement_WINDOWS_MEDIA_PLAYER != null)
            {
                if (wasPlayingBeforeDrag)
                {
                    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Resume();
                }
                wasPlayingBeforeDrag = false;
            }

#if ENABLE_WPF_MEDIAKIT
            if (medElement_MEDIAKIT_DIRECTSHOW != null)
            {
                if (wasPlayingBeforeDrag)
                {
                    medElement_MEDIAKIT_DIRECTSHOW.Play();
                }
                wasPlayingBeforeDrag = false;
            }
#endif //ENABLE_WPF_MEDIAKIT

        }

        private void OnAudioTimeSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (medElement_WINDOWS_MEDIA_PLAYER != null)
            {
                var timeSpan = new TimeSpan(0, 0, 0, 0, (int)Math.Round(AudioTimeSlider.Value));

                if (doNotUpdateVideoTimeWhenSliderChanges || !_timer.IsEnabled)
                {
                    double durationMS = medElement_WINDOWS_MEDIA_PLAYER.NaturalDuration.TimeSpan.TotalMilliseconds;

                    AudioTimeLabel.Text = String.Format(
                        "{0} / {1}",
                        Time.Format_Standard(timeSpan),
                        Time.Format_Standard(medElement_WINDOWS_MEDIA_PLAYER.NaturalDuration.TimeSpan)
                        );
                }

                if (doNotUpdateVideoTimeWhenSliderChanges)
                {
                    doNotUpdateVideoTimeWhenSliderChanges = false;
                    return;
                }

                bool wasPlaying = false;

                //Is Active
                if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentState == ClockState.Active)
                {
                    //Is Paused
                    if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentGlobalSpeed == 0.0)
                    {

                    }
                    else //Is Playing
                    {
                        wasPlaying = true;
                        medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                    }
                }
                else if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentState == ClockState.Stopped)
                {
                    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Begin();
                    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                }

                medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Seek(timeSpan, TimeSeekOrigin.BeginTime);

                if (wasPlaying)
                {
                    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Resume();
                }
            }

#if ENABLE_WPF_MEDIAKIT
            if (medElement_MEDIAKIT_DIRECTSHOW != null)
            {
                double timeMs = AudioTimeSlider.Value;

                if (doNotUpdateVideoTimeWhenSliderChanges || !_timer.IsEnabled)
                {
                    var timeSpan = new TimeSpan(0, 0, 0, 0, (int)Math.Round(timeMs));

                    double durationMS = medElement_MEDIAKIT_DIRECTSHOW.MediaDuration / 10000.0;

                    //MediaPositionFormat.MediaTime
                    //MediaPositionFormat mpf = medElement.CurrentPositionFormat;

                    AudioTimeLabel.Text = String.Format(
                        "{0} / {1}",
                        Time.Format_Standard(timeSpan),
                        Time.Format_Standard(new TimeSpan(0, 0, 0, 0, (int)Math.Round(durationMS)))
                         );
                }

                if (doNotUpdateVideoTimeWhenSliderChanges)
                {
                    doNotUpdateVideoTimeWhenSliderChanges = false;
                    return;
                }

                bool wasPlaying = medElement_MEDIAKIT_DIRECTSHOW.IsPlaying;

                if (wasPlaying)
                {
                    medElement_MEDIAKIT_DIRECTSHOW.Pause();
                }

                long timeVideo = (long)Math.Round(timeMs * 10000.0);
                medElement_MEDIAKIT_DIRECTSHOW.MediaPosition = timeVideo;

                DebugFix.Assert(medElement_MEDIAKIT_DIRECTSHOW.MediaPosition == timeVideo);

                if (wasPlaying)
                {
                    medElement_MEDIAKIT_DIRECTSHOW.Play();
                }
            }
#endif //ENABLE_WPF_MEDIAKIT
        }

        private void stopAudioPlayer()
        {

            if (medElement_WINDOWS_MEDIA_PLAYER != null)
            {
                if (medElement_WINDOWS_MEDIA_PLAYER.LoadedBehavior != MediaState.Manual)
                {
                    return;
                }

                //Is Active
                if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentState == ClockState.Active)
                {
                    //Is Paused
                    if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentGlobalSpeed == 0.0)
                    {
                    }
                    else //Is Playing
                    {
                        _timer.Stop();
                        medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                        actionUpdateSliderFromVideoTime.Invoke();
                    }
                }
            }

#if ENABLE_WPF_MEDIAKIT
            if (medElement_MEDIAKIT_DIRECTSHOW != null)
            {
                if (medElement_MEDIAKIT_DIRECTSHOW.LoadedBehavior != WPFMediaKit.DirectShow.MediaPlayers.MediaState.Manual)
                {
                    return;
                }

                if (medElement_MEDIAKIT_DIRECTSHOW.IsPlaying)
                {
                    _timer.Stop();
                    medElement_MEDIAKIT_DIRECTSHOW.Pause();
                    actionUpdateSliderFromVideoTime.Invoke();
                }
            }
#endif //ENABLE_WPF_MEDIAKIT
        }

        private void OnClick_ButtonAudioPlayPause(object sender, RoutedEventArgs e)
        {
            if (medElement_WINDOWS_MEDIA_PLAYER != null)
            {
                if (medElement_WINDOWS_MEDIA_PLAYER.LoadedBehavior != MediaState.Manual)
                {
                    return;
                }

                bool wasPlaying = false;
                bool wasStopped = false;

                //Is Active
                if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentState == ClockState.Active)
                {
                    //Is Paused
                    if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentGlobalSpeed == 0.0)
                    {
                    }
                    else //Is Playing
                    {
                        wasPlaying = true;
                        medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                    }
                }
                else if (medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentState == ClockState.Stopped)
                {
                    wasStopped = true;
                    //medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Begin();
                    //medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                }

                double durationMS = medElement_WINDOWS_MEDIA_PLAYER.NaturalDuration.TimeSpan.TotalMilliseconds;
                double timeMS =
                    medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentTime == null
                    || !medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentTime.HasValue
                    ? -1.0
                    : medElement_WINDOWS_MEDIA_PLAYER.Clock.CurrentTime.Value.TotalMilliseconds;

                if (timeMS == -1.0 || timeMS >= durationMS)
                {
                    AudioTimeSlider.Value = 0.100;
                }

                if (!wasPlaying)
                {
                    _timer.Start();
                    if (wasStopped)
                    {
                        medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Begin();
                    }
                    else
                    {
                        medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Resume();
                    }
                }
                else
                {
                    _timer.Stop();
                    medElement_WINDOWS_MEDIA_PLAYER.Clock.Controller.Pause();
                    actionUpdateSliderFromVideoTime.Invoke();
                }
            }

#if ENABLE_WPF_MEDIAKIT
            if (medElement_MEDIAKIT_DIRECTSHOW != null)
            {
                if (medElement_MEDIAKIT_DIRECTSHOW.LoadedBehavior != WPFMediaKit.DirectShow.MediaPlayers.MediaState.Manual)
                {
                    return;
                }

                if (medElement_MEDIAKIT_DIRECTSHOW.IsPlaying)
                {
                    _timer.Stop();
                    medElement_MEDIAKIT_DIRECTSHOW.Pause();
                    actionUpdateSliderFromVideoTime.Invoke();
                }
                else
                {
                    _timer.Start();
                    medElement_MEDIAKIT_DIRECTSHOW.Play();
                }


                double durationMS = medElement_MEDIAKIT_DIRECTSHOW.MediaDuration / 10000.0;
                double timeMS = medElement_MEDIAKIT_DIRECTSHOW.MediaPosition / 10000.0;

                if (timeMS >= durationMS)
                {
                    AudioTimeSlider.Value = 0.100;
                }
            }
#endif //ENABLE_WPF_MEDIAKIT
        }
    }
}
