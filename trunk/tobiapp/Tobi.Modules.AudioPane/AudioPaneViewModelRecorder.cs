using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM.Command;
using urakawa.media.data.audio;

namespace Tobi.Modules.AudioPane
{
    public partial class AudioPaneViewModel
    {
        #region Audio Recorder

        private void initializeCommands_Recorder()
        {
            var shellPresenter = Container.Resolve<IShellPresenter>();

            CommandStopRecord = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_StopRecord,
                UserInterfaceStrings.Audio_StopRecord_,
                UserInterfaceStrings.Audio_StopRecord_KEYS,
                shellPresenter.LoadTangoIcon("media-playback-stop"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStopRecord", Category.Debug, Priority.Medium);

                    m_Recorder.StopRecording();
                },
                obj => !IsWaveFormLoading && IsRecording);

            shellPresenter.RegisterRichCommand(CommandStopRecord);
            //
            CommandStartRecord = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_StartRecord,
                UserInterfaceStrings.Audio_StartRecord_,
                UserInterfaceStrings.Audio_StartRecord_KEYS,
                shellPresenter.LoadTangoIcon("media-record"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStartRecord", Category.Debug, Priority.Medium);

                    var session = Container.Resolve<IUrakawaSession>();

                    if (session.DocumentProject == null)
                    {
                        State.ResetAll();

                        m_Recorder.RecordingDirectory = Directory.GetCurrentDirectory();
                        State.Audio.PcmFormatAlt = new PCMFormatInfo();
                    }
                    else
                    {
                        if (State.CurrentTreeNode == null)
                        {
                            return;
                        }

                        Debug.Assert(session.DocumentProject.Presentations.Get(0).MediaDataManager.EnforceSinglePCMFormat);
                        State.Audio.PcmFormatAlt = session.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat;
                    }

                    m_Recorder.StartRecording(new AudioLibPCMFormat(State.Audio.PcmFormatAlt.Data.NumberOfChannels, State.Audio.PcmFormatAlt.Data.SampleRate, State.Audio.PcmFormatAlt.Data.BitDepth));
                },
                obj =>
                {
                    var session = Container.Resolve<IUrakawaSession>();

                    return !IsWaveFormLoading && !IsPlaying && !IsMonitoring && !IsRecording
                        && (
                        (session.DocumentProject != null && State.CurrentTreeNode != null)
                        ||
                        (session.DocumentProject == null)
                        );
                });

            shellPresenter.RegisterRichCommand(CommandStartRecord);

            //
            CommandStartMonitor = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_StartMonitor,
                UserInterfaceStrings.Audio_StartMonitor_,
                UserInterfaceStrings.Audio_StartMonitor_KEYS,
                shellPresenter.LoadGnomeNeuIcon("Neu_audio-x-generic"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStartMonitor", Category.Debug, Priority.Medium);

                    var session = Container.Resolve<IUrakawaSession>();

                    if (session.DocumentProject == null)
                    {
                        State.ResetAll();

                        m_Recorder.RecordingDirectory = Directory.GetCurrentDirectory();
                        State.Audio.PcmFormatAlt = new PCMFormatInfo();

                        //m_PcmFormatOfAudioToInsert = IsAudioLoaded ? State.Audio.PcmFormat : new PCMFormatInfo();
                        //m_Recorder.InputDevice.Capture.Caps.Format44KhzMono16Bit
                    }
                    else
                    {
                        Debug.Assert(session.DocumentProject.Presentations.Get(0).MediaDataManager.EnforceSinglePCMFormat);
                        State.Audio.PcmFormatAlt = session.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat;
                    }

                    m_Recorder.StartMonitoring(new AudioLibPCMFormat(State.Audio.PcmFormatAlt.Data.NumberOfChannels, State.Audio.PcmFormatAlt.Data.SampleRate, State.Audio.PcmFormatAlt.Data.BitDepth));

                    var presenter = Container.Resolve<IShellPresenter>();
                    presenter.PlayAudioCueTock();
                },
                obj => !IsWaveFormLoading && !IsPlaying && !IsRecording && !IsMonitoring);

            shellPresenter.RegisterRichCommand(CommandStartMonitor);
            //
            CommandStopMonitor = new RichDelegateCommand<object>(
                UserInterfaceStrings.Audio_StopMonitor,
                UserInterfaceStrings.Audio_StopMonitor_,
                UserInterfaceStrings.Audio_StopMonitor_KEYS,
                shellPresenter.LoadTangoIcon("media-playback-stop"),
                obj =>
                {
                    Logger.Log("AudioPaneViewModel.CommandStopMonitor", Category.Debug, Priority.Medium);

                    m_Recorder.StopRecording();

                    State.Audio.PcmFormatAlt = null;

                    var presenter = Container.Resolve<IShellPresenter>();
                    presenter.PlayAudioCueTockTock();
                },
                obj => !IsWaveFormLoading && IsMonitoring);

            shellPresenter.RegisterRichCommand(CommandStopMonitor);

            //
        }

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
                if (m_Recorder.CurrentState == AudioRecorder.State.Stopped && value != null && m_Recorder.InputDevice != value)
                {
                    m_Recorder.InputDevice = value;
                }
            }
        }

        private void OnAudioRecordingFinished(object sender, AudioRecorder.AudioRecordingFinishEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(RefreshUI_WaveFormChunkMarkers));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => OnAudioRecordingFinished(sender, e)));
                return;
            }

            openFile(e.RecordedFilePath, true);
        }

        // ReSharper disable MemberCanBeMadeStatic.Local
        private void OnStateChanged_Recorder(object sender, AudioRecorder.StateChangedEventArgs e)
        // ReSharper restore MemberCanBeMadeStatic.Local
        {
            Logger.Log("AudioPaneViewModel.OnStateChanged_Recorder", Category.Debug, Priority.Medium);
            
            resetPeakMeter();

            RaisePropertyChanged(() => IsRecording);
            RaisePropertyChanged(() => IsMonitoring);

            if ((e.OldState == AudioRecorder.State.Recording || e.OldState == AudioRecorder.State.Monitoring)
                && m_Recorder.CurrentState == AudioRecorder.State.Stopped)
            {
                UpdatePeakMeter();
                //if (View != null)
                //{
                //    View.StopPeakMeterTimer();
                //}

                if (View != null)
                {
                    View.TimeMessageHide();
                }
            }
            if (m_Recorder.CurrentState == AudioRecorder.State.Recording || m_Recorder.CurrentState == AudioRecorder.State.Monitoring)
            {
                if (e.OldState == AudioRecorder.State.Stopped)
                {
                    PeakOverloadCountCh1 = 0;
                    PeakOverloadCountCh2 = 0;
                }
                UpdatePeakMeter();
                //if (View != null)
                //{
                //    View.StartPeakMeterTimer();
                //}

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
                return m_Recorder.CurrentState == AudioRecorder.State.Recording;
            }
        }

        public bool IsMonitoring
        {
            get
            {
                return m_Recorder.CurrentState == AudioRecorder.State.Monitoring;
            }
        }

        //private void OnRecorderResetVuMeter(object sender, UpdateVuMeterEventArgs e)
        //{
        //    resetPeakMeter();
        //}

        #endregion Audio Recorder
    }
}
