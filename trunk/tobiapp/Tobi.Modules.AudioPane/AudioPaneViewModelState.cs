using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common.MVVM;
using urakawa.core;
using urakawa.media.data.audio;
using urakawa.media.data.utilities;
using urakawa.media.timing;

namespace Tobi.Modules.AudioPane
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

            public double ConvertBytesToMilliseconds(double bytes)
            {
                PCMFormatInfo pcm = PcmFormat;
                if (pcm == null)
                {
                    pcm = m_viewModel.m_PcmFormatOfAudioToInsert;
                }
                return pcm.GetDuration((long)bytes).TimeDeltaAsMillisecondDouble;
                //return 1000.0 * bytes / ((double)PcmFormat.SampleRate * PcmFormat.NumberOfChannels * PcmFormat.BitDepth / 8.0);
            }

            public double ConvertMillisecondsToBytes(double ms)
            {
                PCMFormatInfo pcm = PcmFormat;
                if (pcm == null)
                {
                    pcm = m_viewModel.m_PcmFormatOfAudioToInsert;
                }
                return pcm.GetDataLength(new TimeDelta(ms));
                //return (ms * PcmFormat.SampleRate * PcmFormat.NumberOfChannels * PcmFormat.BitDepth / 8.0) / 1000.0;
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
                    if (m_PlayStream != null)
                    {
                        m_PlayStream.Position = 0;
                        m_PlayStream.Seek(0, SeekOrigin.Begin);

                        if (m_viewModel.State.CurrentTreeNode != null)
                        {
                            Debug.Assert(m_viewModel.State.CurrentTreeNode.Presentation.MediaDataManager.EnforceSinglePCMFormat);
                            PcmFormat =
                                m_viewModel.State.CurrentTreeNode.Presentation.MediaDataManager.DefaultPCMFormat.Copy();

                            DataLength = m_PlayStream.Length;
                            EndOffsetOfPlayStream = DataLength;
                        }
                        else
                        {
                            PcmFormat = PCMDataInfo.ParseRiffWaveHeader(m_PlayStream);

                            long dataLength = m_PlayStream.Length - m_PlayStream.Position;

                            m_PlayStream = new SubStream(m_PlayStream, m_PlayStream.Position, dataLength);

                            DataLength = m_PlayStream.Length;
                            EndOffsetOfPlayStream = DataLength;

                            Debug.Assert(dataLength == DataLength);
                        }
                    }
                    m_notifier.OnPropertyChanged(() => PlayStream);
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
                    m_notifier.OnPropertyChanged(() => DataLength);
                }
            }

            // The PCM format of the stream of audio data.
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
                    m_notifier.OnPropertyChanged(() => PcmFormat);
                }
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
                    m_notifier.OnPropertyChanged(() => EndOffsetOfPlayStream);
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
                    m_notifier.OnPropertyChanged(() => PlayStreamMarkers);
                }
            }

            public void ResetAll()
            {
                PlayStream = null; // must be first because NotifyPropertyChange chain-reacts for DataLegth (TimeString data binding) 

                EndOffsetOfPlayStream = -1;
                PcmFormat = null;
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

            public void SetSelection(double begin, double end)
            {
                SelectionBegin = begin;
                SelectionEnd = end;

                if (m_viewModel.View != null && m_viewModel.State.Audio.HasContent)
                {
                    m_viewModel.View.SetSelection(
                        m_viewModel.State.Audio.ConvertMillisecondsToBytes(SelectionBegin),
                        m_viewModel.State.Audio.ConvertMillisecondsToBytes(SelectionEnd));
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

            public bool IsSelectionSet
            {
                get
                {
                    return SelectionBegin >= 0 && SelectionEnd >= 0;
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
                    m_notifier.OnPropertyChanged(() => SelectionBegin);
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
                    m_notifier.OnPropertyChanged(() => SelectionEnd);
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
                if (CurrentTreeNode == null || !Audio.HasContent)
                {
                    return false;
                }

                if (CurrentTreeNode == treeNode || CurrentSubTreeNode == treeNode)
                {
                    return true;
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
                    m_notifier.OnPropertyChanged(() => CurrentTreeNode);

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
                    m_notifier.OnPropertyChanged(() => CurrentSubTreeNode);
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
                    m_notifier.OnPropertyChanged(() => FilePath);

                    CurrentTreeNode = null;
                }
            }

            public void ResetAll()
            {
                m_viewModel.Logger.Log("Audio StateData reset.", Category.Debug, Priority.Medium);

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
