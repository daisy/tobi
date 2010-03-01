using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using urakawa.core;
using urakawa.data;
using urakawa.media.data.audio;
using urakawa.media.data.utilities;

namespace Tobi.Plugin.AudioPane
{
    public partial class AudioPaneViewModel
    {
        public class StreamStateData
        {
            private AudioPaneViewModel m_viewModel;
            private PropertyChangedNotifyBase m_notifier;
            public StreamStateData(PropertyChangedNotifyBase notifier, AudioPaneViewModel vm)
            {
                m_viewModel = vm;
                m_notifier = notifier;
            }

            public bool HasContent
            {
                get { return PlayStream != null; }
            }

            public double ConvertBytesToMilliseconds(long bytes)
            {
                PCMFormatInfo pcmInfo = GetCurrentPcmFormat();

                return pcmInfo.Data.ConvertBytesToTime(bytes);
            }

            public long ConvertMillisecondsToBytes(double ms)
            {
                PCMFormatInfo pcmInfo = GetCurrentPcmFormat();

                return pcmInfo.Data.ConvertTimeToBytes(ms);
            }


            public void SetPlayStream_FromTreeNode(Stream stream)
            {
                if (stream != null)
                {
                    stream.Position = 0;
                    stream.Seek(0, SeekOrigin.Begin);

                    if (m_viewModel.State.CurrentTreeNode != null)
                    {
                        Debug.Assert(m_viewModel.State.CurrentTreeNode.Presentation.MediaDataManager.EnforceSinglePCMFormat);
                        PcmFormat = m_viewModel.State.CurrentTreeNode.Presentation.MediaDataManager.DefaultPCMFormat.Copy();
                    }
                    else if (m_viewModel.m_UrakawaSession != null && m_viewModel.m_UrakawaSession.DocumentProject != null)
                    {
                        Debug.Assert(m_viewModel.m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.EnforceSinglePCMFormat);
                        PcmFormat = m_viewModel.m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat.Copy();
                    }
                    else
                    {
                        PcmFormat = null;
                        Debug.Fail("This should never happen !!");
                    }

                    DataLength = stream.Length;
                    EndOffsetOfPlayStream = DataLength;
                }

                PlayStream = stream;
            }

            public void SetPlayStream_FromFile(FileStream fileStream)
            {
                Stream stream = fileStream;

                if (stream != null)
                {
                    stream.Position = 0;
                    stream.Seek(0, SeekOrigin.Begin);

                    uint dataLength;
                    AudioLibPCMFormat format = AudioLibPCMFormat.RiffHeaderParse(stream, out dataLength);

                    PcmFormat = new PCMFormatInfo(format);

                    dataLength = (uint)(stream.Length - stream.Position);

                    stream = new SubStream(stream, stream.Position, dataLength);

                    Debug.Assert(dataLength == stream.Length);

                    DataLength = stream.Length;
                    EndOffsetOfPlayStream = DataLength;
                }

                PlayStream = stream;
            }

            // The single stream of contiguous PCM data,
            // regardless of the sub chunks / tree nodes
            private Stream m_PlayStream;
            public Stream PlayStream
            {
                get
                {
                    return m_PlayStream;
                }
                set
                {
                    if (m_PlayStream == value) return;
                    m_PlayStream = value;
                    m_notifier.RaisePropertyChanged(() => PlayStream);
                }
            }

            // The total byte length of the stream of audio PCM data.
            private long m_DataLength;
            public long DataLength
            {
                get
                {
                    return m_DataLength;
                }
                set
                {
                    if (m_DataLength == value) return;
                    m_DataLength = value;
                    m_notifier.RaisePropertyChanged(() => DataLength);
                }
            }

            // The PCM format of the stream of audio data.
            // Can have a valid value even when the stream is null (e.g. when recording or inserting an external audio file)
            private PCMFormatInfo m_PcmFormat;
            public PCMFormatInfo PcmFormat
            {
                get
                {
                    return m_PcmFormat;
                }
                set
                {
                    if (m_PcmFormat == value) return;
                    m_PcmFormat = value;
                    m_notifier.RaisePropertyChanged(() => PcmFormat);
                }
            }

            // Used when recording or monitoring (no loaded stream data yet, just the PCM information)
            private PCMFormatInfo m_PcmFormatRecordingMonitoring;
            public PCMFormatInfo PcmFormatRecordingMonitoring
            {
                get
                {
                    return m_PcmFormatRecordingMonitoring;
                }
                set
                {
                    if (m_PcmFormatRecordingMonitoring == value) return;
                    m_PcmFormatRecordingMonitoring = value;
                    m_notifier.RaisePropertyChanged(() => PcmFormatRecordingMonitoring);
                }
            }

            public PCMFormatInfo GetCurrentPcmFormat()
            {
                if (m_viewModel.IsRecording || m_viewModel.IsMonitoring)
                    return PcmFormatRecordingMonitoring;
                return PcmFormat;
            }


            // The stream offset in bytes where the audio playback should stop.
            // By default: it is the DataLength, but it can be changed when dealing with selections and preview-playback modes.
            private long m_EndOffsetOfPlayStream;
            public long EndOffsetOfPlayStream
            {
                get
                {
                    return m_EndOffsetOfPlayStream;
                }
                set
                {
                    if (m_EndOffsetOfPlayStream == value) return;
                    m_EndOffsetOfPlayStream = value;
                    m_notifier.RaisePropertyChanged(() => EndOffsetOfPlayStream);
                }
            }

            // The list that defines the sub treenodes with associated chunks of audio data
            // This is never null: the count is 1 when the current main tree node has direct audio (no sub tree nodes)
            private List<TreeNodeAndStreamDataLength> m_PlayStreamMarkers;
            public List<TreeNodeAndStreamDataLength> PlayStreamMarkers
            {
                get
                {
                    return m_PlayStreamMarkers;
                }
                set
                {
                    if (m_PlayStreamMarkers == value) return;
                    if (value == null)
                    {
                        m_PlayStreamMarkers.Clear();
                    }
                    m_PlayStreamMarkers = value;
                    m_notifier.RaisePropertyChanged(() => PlayStreamMarkers);
                }
            }

            public void ResetAll()
            {
                PlayStream = null; // must be first because NotifyPropertyChange chain-reacts for DataLegth (TimeString data binding) 

                EndOffsetOfPlayStream = -1;
                PcmFormat = null;
                PcmFormatRecordingMonitoring = null;
                PlayStreamMarkers = null;
                DataLength = -1;
            }
        }

        public class SelectionStateData
        {
            private AudioPaneViewModel m_viewModel;
            private PropertyChangedNotifyBase m_notifier;
            public SelectionStateData(PropertyChangedNotifyBase notifier, AudioPaneViewModel vm)
            {
                m_notifier = notifier;
                m_viewModel = vm;
            }

            public void SetSelectionTime(double begin, double end)
            {
                SelectionBegin = begin;
                SelectionEnd = end;

                if (m_viewModel.View != null && m_viewModel.State.Audio.HasContent)
                {
                    m_viewModel.View.SetSelectionTime(SelectionBegin, SelectionEnd);
                }
            }

            public void SetSelectionBytes(long begin, long end)
            {
                SelectionBegin = m_viewModel.State.Audio.ConvertBytesToMilliseconds(begin);
                SelectionEnd = m_viewModel.State.Audio.ConvertBytesToMilliseconds(end);

                if (m_viewModel.View != null && m_viewModel.State.Audio.HasContent)
                {
                    m_viewModel.View.SetSelectionBytes(begin, end);
                }
            }

            public void ClearSelection()
            {
                SelectionBegin = -1.0;
                SelectionEnd = -1.0;
                if (m_viewModel.View != null)
                {
                    m_viewModel.View.ClearSelection();
                }
            }

            private double m_SelectionBegin;
            public double SelectionBegin
            {
                get
                {
                    return m_SelectionBegin;
                }
                private set
                {
                    if (m_SelectionBegin == value) return;
                    m_SelectionBegin = value;
                    m_notifier.RaisePropertyChanged(() => SelectionBegin);
                }
            }

            private double m_SelectionEnd;
            public double SelectionEnd
            {
                get
                {
                    return m_SelectionEnd;
                }
                private set
                {
                    if (m_SelectionEnd == value) return;
                    m_SelectionEnd = value;
                    m_notifier.RaisePropertyChanged(() => SelectionEnd);
                }
            }

            public void ResetAll()
            {
                SelectionBegin = -1;
                SelectionEnd = -1;
            }
        }

        public StateData State { get; private set; }
        public class StateData
        {
            public SelectionStateData Selection { get; private set; }
            public StreamStateData Audio { get; private set; }

            private AudioPaneViewModel m_viewModel;
            private PropertyChangedNotifyBase m_notifier;
            public StateData(PropertyChangedNotifyBase notifier, AudioPaneViewModel vm)
            {
                m_notifier = notifier;
                m_viewModel = vm;
                Audio = new StreamStateData(m_notifier, vm);
                Selection = new SelectionStateData(m_notifier, vm);
            }

            public bool IsTreeNodeShownInAudioWaveForm(TreeNode treeNode)
            {
                if (treeNode == null)
                {
                    return false;
                }

                if (CurrentTreeNode == treeNode || CurrentSubTreeNode == treeNode)
                {
                    return true;
                }

                if (!Audio.HasContent)
                {
                    return false;
                }

                foreach (TreeNodeAndStreamDataLength marker in Audio.PlayStreamMarkers)
                {
                    if (marker.m_TreeNode == treeNode) return true;
                }

                return false;
            }

            // Main selected node. There are sub tree nodes when no audio is directly
            // attached to this tree node.
            // Automatically implies that FilePath is null
            // (they are mutually-exclusive state values).
            private TreeNode m_CurrentTreeNode;
            public TreeNode CurrentTreeNode
            {
                get
                {
                    return m_CurrentTreeNode;
                }
                set
                {
                    if (m_CurrentTreeNode == value) return;
                    m_CurrentTreeNode = value;
                    m_notifier.RaisePropertyChanged(() => CurrentTreeNode);

                    CurrentSubTreeNode = null;

                    FilePath = null;
                }
            }

            // Secondary selected node. By default is the first one in the series.
            // It is equal to the main selected tree node when the audio data is attached directly to it.
            private TreeNode m_CurrentSubTreeNode;
            public TreeNode CurrentSubTreeNode
            {
                get
                {
                    return m_CurrentSubTreeNode;
                }
                set
                {
                    if (m_CurrentSubTreeNode == value) return;
                    m_CurrentSubTreeNode = value;
                    m_notifier.RaisePropertyChanged(() => CurrentSubTreeNode);
                }
            }

            // Path to a WAV file,
            // only used when the user opens such file for playback / preview.
            // Automatically implies that CurrentTreeNode and CurrentSubTreeNode are null
            // (they are mutually-exclusive state values).
            private string m_WavFilePath;
            public string FilePath
            {
                get
                {
                    return m_WavFilePath;
                }
                set
                {
                    if (m_WavFilePath == value) return;
                    m_WavFilePath = value;
                    m_notifier.RaisePropertyChanged(() => FilePath);

                    CurrentTreeNode = null;
                }
            }

            public void ResetAll()
            {
                //m_viewModel.Logger.Log("Audio StateData reset.", Category.Debug, Priority.Medium);

                FilePath = null;
                CurrentTreeNode = null;
                CurrentSubTreeNode = null;

                Selection.ResetAll();
                Audio.ResetAll();

                if (m_viewModel.View != null)
                {
                    m_viewModel.View.ResetAll();
                }
            }
        }
    }
}
