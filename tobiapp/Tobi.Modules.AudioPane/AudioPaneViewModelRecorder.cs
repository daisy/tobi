using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Infrastructure;
using urakawa.command;
using urakawa.commands;
using urakawa.core;
using urakawa.media;
using urakawa.media.data.audio;
using urakawa.media.data.audio.codec;
using urakawa.media.timing;
using urakawa.property.channel;

namespace Tobi.Modules.AudioPane
{
    public partial class AudioPaneViewModel
    {
        #region Audio Recorder

        private AudioRecorder m_Recorder;

        public List<InputDevice> InputDevices
        {
            get
            {
                return m_Recorder.InputDevices;
            }
        }
        public InputDevice InputDevice
        {
            get
            {
                return m_Recorder.InputDevice;
            }
            set
            {
                if (value != null && m_Recorder.InputDevice != value)
                {
                    if (m_Recorder.State != AudioRecorderState.Stopped)
                    {
                        return;
                    }
                    m_Recorder.InputDevice = value;
                }
            }
        }

        // ReSharper disable MemberCanBeMadeStatic.Local
        private void OnRecorderStateChanged(object sender, AudioLib.Events.Recorder.StateChangedEventArgs e)
        // ReSharper restore MemberCanBeMadeStatic.Local
        {
            Logger.Log("AudioPaneViewModel.OnRecorderStateChanged", Category.Debug, Priority.Medium);

            OnPropertyChanged(() => IsRecording);
            OnPropertyChanged(() => IsMonitoring);

            if ((e.OldState == AudioRecorderState.Recording || e.OldState == AudioRecorderState.Monitoring)
                && m_Recorder.State == AudioRecorderState.Stopped)
            {
                UpdatePeakMeter();
                if (View != null)
                {
                    View.StopPeakMeterTimer();
                }

                if (View != null)
                {
                    View.TimeMessageHide();
                }
            }
            if (m_Recorder.State == AudioRecorderState.Recording || m_Recorder.State == AudioRecorderState.Monitoring)
            {
                if (e.OldState == AudioRecorderState.Stopped)
                {
                    PeakOverloadCountCh1 = 0;
                    PeakOverloadCountCh2 = 0;
                }
                UpdatePeakMeter();
                if (View != null)
                {
                    View.StartPeakMeterTimer();
                }

                if (View != null)
                {
                    View.TimeMessageShow();
                }

                var presenter = Container.Resolve<IShellPresenter>();
                presenter.PlayAudioCueTock();
            }
        }

        public bool IsRecording
        {
            get
            {
                return m_Recorder.State == AudioRecorderState.Recording;
            }
        }

        public bool IsMonitoring
        {
            get
            {
                return m_Recorder.State == AudioRecorderState.Monitoring;
            }
        }

        public void AudioRecorder_StartStopMonitor()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_StartStopMonitor", Category.Debug, Priority.Medium);
            if (IsMonitoring)
            {
                AudioRecorder_StopMonitor();
            }
            else
            {
                AudioRecorder_StartMonitor();
            }
        }

        public void AudioRecorder_StartMonitor()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_StartMonitor", Category.Debug, Priority.Medium);

            var session = Container.Resolve<IUrakawaSession>();

            if (session.DocumentProject == null)
            {
                setRecordingDirectory(Directory.GetCurrentDirectory());
                PcmFormat = new PCMFormatInfo();
            }
            else
            {
                if (PcmFormat == null)
                {
                    PcmFormat = session.DocumentProject.GetPresentation(0).MediaDataManager.DefaultPCMFormat;
                }
            }

            m_Recorder.StartListening(new AudioLibPCMFormat(PcmFormat.NumberOfChannels, PcmFormat.SampleRate, PcmFormat.BitDepth));

            var presenter = Container.Resolve<IShellPresenter>();
            presenter.PlayAudioCueTock();
        }

        public void AudioRecorder_StopMonitor()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_StopMonitor", Category.Debug, Priority.Medium);

            m_Recorder.StopRecording();

            var presenter = Container.Resolve<IShellPresenter>();
            presenter.PlayAudioCueTockTock();
        }

        public void AudioRecorder_StartStop()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_StartStop", Category.Debug, Priority.Medium);
            if (IsRecording)
            {
                AudioRecorder_Stop();
            }
            else
            {
                AudioRecorder_Start();
            }
        }

        public void AudioRecorder_Start()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_Start", Category.Debug, Priority.Medium);

            var session = Container.Resolve<IUrakawaSession>();

            if (session.DocumentProject == null)
            {
                setRecordingDirectory(Directory.GetCurrentDirectory());
                PcmFormat = new PCMFormatInfo();
            }
            else
            {
                if (CurrentTreeNode == null)
                {
                    return;
                }

                if (PcmFormat == null)
                {
                    PcmFormat = session.DocumentProject.GetPresentation(0).MediaDataManager.DefaultPCMFormat;
                }
            }

            m_Recorder.StartRecording(new AudioLibPCMFormat(PcmFormat.NumberOfChannels, PcmFormat.SampleRate, PcmFormat.BitDepth));
        }

        private ManagedAudioMedia makeManagedAudioMediaFromRecording()
        {
            Stream recordingStream = File.Open(m_Recorder.RecordedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            PCMDataInfo pcmFormat = PCMDataInfo.ParseRiffWaveHeader(recordingStream);
            long dataLength = recordingStream.Length - recordingStream.Position;
            double recordingDuration = pcmFormat.GetDuration(dataLength).TimeDeltaAsMillisecondDouble;


            ManagedAudioMedia managedAudioMediaNew =
                CurrentSubTreeNode.Presentation.MediaFactory.CreateManagedAudioMedia();

            var mediaData =
                (WavAudioMediaData)
                CurrentSubTreeNode.Presentation.MediaDataFactory.CreateAudioMediaData();

            managedAudioMediaNew.MediaData = mediaData;

            //mediaData.AppendAudioDataFromRiffWave(m_Recorder.RecordedFilePath);
            mediaData.AppendAudioData(recordingStream, new TimeDelta(recordingDuration));
            recordingStream.Close();

            File.Delete(m_Recorder.RecordedFilePath);

            return managedAudioMediaNew;
        }

        public void AudioRecorder_Stop()
        {
            Logger.Log("AudioPaneViewModel.AudioRecorder_Stop", Category.Debug, Priority.Medium);

            m_Recorder.StopRecording();

            var presenter = Container.Resolve<IShellPresenter>();
            presenter.PlayAudioCueTockTock();

            if (View != null)
            {
                View.ResetAll();
            }

            if (string.IsNullOrEmpty(m_Recorder.RecordedFilePath) || !File.Exists(m_Recorder.RecordedFilePath))
            {
                return;
            }

            var session = Container.Resolve<IUrakawaSession>();

            if (session.DocumentProject != null)
            {
                if (CurrentTreeNode == null)
                {
                    return;
                }
                if (CurrentSubTreeNode == null)
                {
                    Debug.Fail("This should never happen !!!");
                    return;
                }

                ManagedAudioMedia recordingManagedAudioMedia = makeManagedAudioMediaFromRecording();

                ManagedAudioMedia managedAudioMedia = CurrentSubTreeNode.GetManagedAudioMedia();
                if (managedAudioMedia == null)
                {
                    SequenceMedia seqAudioMedia = CurrentSubTreeNode.GetAudioSequenceMedia();
                    bool isSeqValid = seqAudioMedia != null && !seqAudioMedia.AllowMultipleTypes;
                    if (isSeqValid)
                    {
                        foreach (Media media in seqAudioMedia.ListOfItems)
                        {
                            if (!(media is ManagedAudioMedia))
                            {
                                isSeqValid = false;
                                break;
                            }
                        }
                    }
                    if (isSeqValid)
                    {
                        var byteOffset = (long)AudioPlayer_ConvertMillisecondsToBytes(LastPlayHeadTime);

                        double timeOffset = 0;
                        long sumData = 0;
                        long sumDataPrev = 0;
                        foreach (Media media in seqAudioMedia.ListOfItems)
                        {
                            var manangedMediaSeqItem = (ManagedAudioMedia) media;
                            AudioMediaData audioData = manangedMediaSeqItem.AudioMediaData;
                            sumData += audioData.GetPCMLength();
                            if (byteOffset < sumData)
                            {
                                timeOffset = AudioPlayer_ConvertBytesToMilliseconds(byteOffset - sumDataPrev);

                                if (AudioPlaybackStreamKeepAlive)
                                {
                                    ensurePlaybackStreamIsDead();
                                }

                                if (manangedMediaSeqItem.AudioMediaData == null)
                                {
                                    Debug.Fail("This should never happen !!!");
                                    //recordingStream.Close();
                                    return;
                                }

                                var command = CurrentSubTreeNode.Presentation.CommandFactory.CreateManagedAudioMediaInsertDataCommand(
                                                            manangedMediaSeqItem, recordingManagedAudioMedia, new Time(timeOffset));
                                CurrentSubTreeNode.Presentation.UndoRedoManager.Execute(command);

                                //manangedMediaSeqItem.AudioMediaData.InsertAudioData(recordingStream, new Time(timeOffset), new TimeDelta(recordingDuration));
                                //recordingStream.Close();
                                break;
                            }
                            sumDataPrev = sumData;
                        }
                    }
                    else
                    {
                        var command = CurrentSubTreeNode.Presentation.CommandFactory.CreateTreeNodeSetManagedAudioMediaCommand(
                                                    CurrentSubTreeNode, recordingManagedAudioMedia);
                        CurrentSubTreeNode.Presentation.UndoRedoManager.Execute(command);

                        if (AudioPlaybackStreamKeepAlive)
                        {
                            ensurePlaybackStreamIsDead();
                        }
                    }
                }
                else
                {
                    double timeOffset = LastPlayHeadTime;
                    if (CurrentSubTreeNode != CurrentTreeNode)
                    {
                        var byteOffset = (long)AudioPlayer_ConvertMillisecondsToBytes(LastPlayHeadTime);

                        long sumData = 0;
                        long sumDataPrev = 0;
                        foreach (TreeNodeAndStreamDataLength marker in PlayStreamMarkers)
                        {
                            sumData += marker.m_LocalStreamDataLength;
                            if (byteOffset < sumData)
                            {
                                if (CurrentSubTreeNode != marker.m_TreeNode)
                                {
                                    Debug.Fail("This should never happen !!!");
                                    //recordingStream.Close();
                                    return;
                                }

                                timeOffset = AudioPlayer_ConvertBytesToMilliseconds(byteOffset - sumDataPrev);
                                break;
                            }
                            sumDataPrev = sumData;
                        }
                    }

                    if (AudioPlaybackStreamKeepAlive)
                    {
                        ensurePlaybackStreamIsDead();
                    }

                    if (managedAudioMedia.AudioMediaData == null)
                    {
                        Debug.Fail("This should never happen !!!");
                        //recordingStream.Close();
                        return;
                    }

                    var command = CurrentSubTreeNode.Presentation.CommandFactory.CreateManagedAudioMediaInsertDataCommand(
                                                managedAudioMedia, recordingManagedAudioMedia, new Time(timeOffset));
                    CurrentSubTreeNode.Presentation.UndoRedoManager.Execute(command);

                    //managedAudioMedia.AudioMediaData.InsertAudioData(recordingStream, new Time(timeOffset), new TimeDelta(recordingDuration));
                    //recordingStream.Close();
                }

                SelectionBegin = (LastPlayHeadTime < 0 ? 0 : LastPlayHeadTime);
                SelectionEnd = SelectionBegin + recordingManagedAudioMedia.Duration.TimeDeltaAsMillisecondDouble;

                ReloadWaveForm();
            }
            else
            {
                if (AudioPlaybackStreamKeepAlive)
                {
                    ensurePlaybackStreamIsDead();
                }

                OpenFile(m_Recorder.RecordedFilePath);
            }
        }

        #endregion Audio Recorder
    }
}
