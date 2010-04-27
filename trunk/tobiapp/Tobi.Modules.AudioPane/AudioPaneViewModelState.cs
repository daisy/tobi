using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AudioLib;
using Tobi.Common.MVVM;
using urakawa.core;
using urakawa.data;
using urakawa.media.data.audio;

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


            public void SetPlayStream_FromTreeNode(Stream stream)
            {
                if (stream != null)
                {
                    stream.Position = 0;
                    stream.Seek(0, SeekOrigin.Begin);

                    Tuple<TreeNode, TreeNode> treeNodeSelection = m_viewModel.m_UrakawaSession.GetTreeNodeSelection();

                    if (treeNodeSelection.Item1 != null)
                    {
                        Debug.Assert(treeNodeSelection.Item1.Presentation.MediaDataManager.EnforceSinglePCMFormat);
                        PcmFormat = treeNodeSelection.Item1.Presentation.MediaDataManager.DefaultPCMFormat.Copy();
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

            public void SetPlayStream_FromFile(FileStream fileStream, string filePathOptionalInfo)
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

                    stream = new SubStream(stream, stream.Position, dataLength, filePathOptionalInfo);

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

            public bool FindInPlayStreamMarkers(TreeNode treeNode, out int index, out long bytesLeft, out long bytesRight)
            {
                bytesRight = 0;
                bytesLeft = 0;
                index = -1;
                if (PlayStreamMarkers == null) return false;

                foreach (TreeNodeAndStreamDataLength marker in PlayStreamMarkers)
                {
                    index++;
                    bytesRight += marker.m_LocalStreamDataLength;
                    if (treeNode == marker.m_TreeNode || treeNode.IsDescendantOf(marker.m_TreeNode))
                    {
                        return true;
                    }
                    bytesLeft = bytesRight;
                }

                return false;
            }

            public bool FindInPlayStreamMarkers(long byteOffset, out TreeNode treeNode, out int index, out long bytesLeft, out long bytesRight)
            {
                treeNode = null;
                bytesRight = 0;
                bytesLeft = 0;
                index = -1;
                if (PlayStreamMarkers == null) return false;

                foreach (TreeNodeAndStreamDataLength marker in PlayStreamMarkers)
                {
                    index++;
                    bytesRight += marker.m_LocalStreamDataLength;
                    if (byteOffset < bytesRight
                    || index == (PlayStreamMarkers.Count - 1) && byteOffset >= bytesRight)
                    {
                        treeNode = marker.m_TreeNode;

                        return true;
                    }
                    bytesLeft = bytesRight;
                }

                return false;
            }

            public void FindInPlayStreamMarkersAndDo(long byteOffset,
                Func<long, long, TreeNode, int, long> matchFunc,
                Func<long, long, long, TreeNode, long> nonMatchFunc)
            {
                if (PlayStreamMarkers == null) return;

                TreeNode treeNode = null;
                long bytesRight = 0;
                long bytesLeft = 0;
                int index = -1;
                
                foreach (TreeNodeAndStreamDataLength marker in PlayStreamMarkers)
                {
                    treeNode = marker.m_TreeNode;

                    index++;
                    bytesRight += marker.m_LocalStreamDataLength;
                    if (byteOffset < bytesRight
                    || index == (PlayStreamMarkers.Count - 1) && byteOffset >= bytesRight)
                    {
                        long newMatch = matchFunc(bytesLeft, bytesRight, treeNode, index);
                        if (newMatch == -1) break;
                        byteOffset = newMatch;
                    }
                    else
                    {
                        long newMatch = nonMatchFunc(byteOffset, bytesLeft, bytesRight, treeNode);
                        if (newMatch == -1) break;
                        byteOffset = newMatch;
                    }
                    bytesLeft = bytesRight;
                }
            }
            //public bool IsTreeNodeShownInAudioWaveForm(TreeNode treeNode)
            //{
            //    if (treeNode == null)
            //    {
            //        return false;
            //    }

            //    Tuple<TreeNode, TreeNode> treeNodeSelection = m_viewModel.m_UrakawaSession.GetTreeNodeSelection();
            //    if (treeNodeSelection.Item1 == treeNode
            //        || treeNodeSelection.Item2 == treeNode)
            //    {
            //        return true;
            //    }

            //    if (PlayStreamMarkers == null)
            //    {
            //        return false;
            //    }

            //    long bytesRight;
            //    long bytesLeft;
            //    int index;
            //    return FindInPlayStreamMarkers(treeNode, out index, out bytesLeft, out bytesRight);
            //}
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

            public void SetSelectionBytes(long begin, long end)
            {
                if (m_viewModel.View != null && m_viewModel.State.Audio.HasContent)
                {
                    SelectionBeginBytePosition = m_viewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(begin);
                    SelectionEndBytePosition = m_viewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(end);

                    m_viewModel.View.SetSelectionBytes(SelectionBeginBytePosition, SelectionEndBytePosition);
                }
                else
                {
                    SelectionBeginBytePosition = begin;
                    SelectionEndBytePosition = end;
                }

                if (m_viewModel.IsAutoPlay)
                {
                    m_viewModel.PlayBytePosition = SelectionBeginBytePosition;


                    //m_viewModel.m_LastSetPlayHeadTime = SelectionBegin;
                    //m_viewModel.CommandPlay.Execute();



                    //long bytesFrom = Convert.ToInt64(m_TimeSelectionLeftX * BytesPerPixel);

                    //m_ViewModel.IsAutoPlay = false;
                    //m_ViewModel.LastPlayHeadTime = m_ViewModel.State.Audio.ConvertBytesToMilliseconds(bytesFrom);
                    //m_ViewModel.IsAutoPlay = true;

                    //long bytesTo = Convert.ToInt64(right * BytesPerPixel);

                    //m_ViewModel.AudioPlayer_PlayFromTo(bytesFrom, bytesTo);
                }
            }

            public void ClearSelection()
            {
                SelectionBeginBytePosition = -1;
                SelectionEndBytePosition = -1;
                if (m_viewModel.View != null)
                {
                    m_viewModel.View.ClearSelection();
                }
            }

            private long m_SelectionBeginBytePosition;
            public long SelectionBeginBytePosition
            {
                get
                {
                    return m_SelectionBeginBytePosition;
                }
                private set
                {
                    if (m_SelectionBeginBytePosition == value) return;
                    m_SelectionBeginBytePosition = value;
                    m_notifier.RaisePropertyChanged(() => SelectionBeginBytePosition);
                }
            }

            private long m_SelectionEndVytePosition;
            public long SelectionEndBytePosition
            {
                get
                {
                    return m_SelectionEndVytePosition;
                }
                private set
                {
                    if (m_SelectionEndVytePosition == value) return;
                    m_SelectionEndVytePosition = value;
                    m_notifier.RaisePropertyChanged(() => SelectionEndBytePosition);
                }
            }

            public void ResetAll()
            {
                SelectionBeginBytePosition = -1;
                SelectionEndBytePosition = -1;
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

            
            //// Main selected node. There are sub tree nodes when no audio is directly
            //// attached to this tree node.
            //// Automatically implies that FilePath is null
            //// (they are mutually-exclusive state values).
            //private TreeNode m_CurrentTreeNode;
            //public TreeNode CurrentTreeNode
            //{
            //    get
            //    {
            //        return m_CurrentTreeNode;
            //    }
            //    set
            //    {
            //        if (m_CurrentTreeNode == value) return;
            //        m_CurrentTreeNode = value;
            //        m_notifier.RaisePropertyChanged(() => CurrentTreeNode);

            //        CurrentSubTreeNode = null;

            //        FilePath = null;
            //    }
            //}

            //// Secondary selected node. By default is the first one in the series.
            //// It is equal to the main selected tree node when the audio data is attached directly to it.
            //private TreeNode m_CurrentSubTreeNode;
            //public TreeNode CurrentSubTreeNode
            //{
            //    get
            //    {
            //        return m_CurrentSubTreeNode;
            //    }
            //    set
            //    {
            //        if (m_CurrentSubTreeNode == value) return;
            //        m_CurrentSubTreeNode = value;
            //        m_notifier.RaisePropertyChanged(() => CurrentSubTreeNode);
            //    }
            //}

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

                    //CurrentTreeNode = null;
                }
            }

            public void ResetAll()
            {
                //m_viewModel.Logger.Log("Audio StateData reset.", Category.Debug, Priority.Medium);

                FilePath = null;
                //CurrentTreeNode = null;
                //CurrentSubTreeNode = null;

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
