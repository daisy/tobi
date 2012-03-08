using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using AudioLib;
using Microsoft.Practices.Composite.Logging;
using urakawa.core;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Threading;
using urakawa.data;

namespace Tobi.Plugin.AudioPane
{
    public class AudioTTSGenerator : DualCancellableProgressReporter
    {
        private readonly string Text;
        private readonly AudioLibPCMFormat PcmFormat;
        private readonly string OutputDirectory;
        private readonly SpeechSynthesizer SpeechSynthesizer;

        public AudioTTSGenerator(string text, AudioLibPCMFormat pcmFormat, string outputDirectory, SpeechSynthesizer speechSynthesizer)
        {
            Text = text;
            PcmFormat = pcmFormat;
            OutputDirectory = outputDirectory;
            SpeechSynthesizer = speechSynthesizer;
        }

        public string GeneratedAudioFilePath
        {
            get;
            private set;
        }

        private static long m_ttsFileNameCounter;

        public override void DoWork()
        {
            EventHandler<SpeakProgressEventArgs> delegateProgress = null;
            EventHandler<SpeakCompletedEventArgs> delegateCompleted = null;
            try
            {
                if (PcmFormat.BitDepth != 16)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    return;
                }
                var formatInfo = new SpeechAudioFormatInfo((int)PcmFormat.SampleRate, AudioBitsPerSample.Sixteen, PcmFormat.NumberOfChannels == 2 ? AudioChannel.Stereo : AudioChannel.Mono);

                int i = 0;

            tryagain:
                i++;

            var filePath = Path.Combine(OutputDirectory, "tts_" + m_ttsFileNameCounter + DataProviderFactory.AUDIO_WAV_EXTENSION);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                try
                {
                    SpeechSynthesizer.SetOutputToWaveFile(filePath, formatInfo);
                }
                catch (Exception ex)
                {
                    SpeechSynthesizer.SetOutputToNull();
                    if (i > 100) return;
                    goto tryagain;
                }

                var manualResetEvent = new ManualResetEvent(false);

                var watch = new Stopwatch();
                watch.Start();

                bool done = false;

                string msg = Tobi_Plugin_AudioPane_Lang.GeneratingTTSAudio + " [" + (Text.Length > 20 ? Text.Substring(0, 19) + "..." : Text) + "]";

                delegateProgress = (sender, ev) =>
                {
                    if (done)
                    {
                        return;
                    }

                    if (RequestCancellation)
                    {
                        SpeechSynthesizer.SpeakAsyncCancelAll();
                        return;
                    }

                    if (true || watch.ElapsedMilliseconds > 500)
                    {
                        watch.Stop();

                        int percent = 100 * (ev.CharacterPosition + ev.CharacterCount) / Text.Length;
                        if (percent < 0) percent = 0;
                        if (percent > 100) percent = 100;

                        //Console.WriteLine("%%%%%%%% " + percent);

                        reportProgress(percent, msg);

                        watch.Reset();
                        watch.Start();
                    }
                };
                SpeechSynthesizer.SpeakProgress += delegateProgress;

                delegateCompleted = (sender, ev) =>
                {
                    if (ev.Cancelled)
                    {
                        int debug = 1;
                    }

                    var filePrompt = ev.Prompt as FilePrompt;
                    string str = ev.Prompt.ToString();
                    if (SpeechSynthesizer.State == SynthesizerState.Ready)
                    {
                        manualResetEvent.Set();
                    }
                    else
                    {
                        int debug = 1;
                    }
                };
                SpeechSynthesizer.SpeakCompleted += delegateCompleted;

                //SpeechSynthesizer.StateChanged += (sender, ev) =>
                //{
                //    if (ev.PreviousState == SynthesizerState.Speaking
                //        //&& ev.State == SynthesizerState.Paused
                //        )
                //    {
                //        manualResetEvent.Set();
                //    }
                //};

                SpeechSynthesizer.SpeakAsync(Text);
                manualResetEvent.WaitOne();

                done = true;
                watch.Stop();

                SpeechSynthesizer.SpeakCompleted -= delegateCompleted;
                SpeechSynthesizer.SpeakProgress -= delegateProgress;

                Thread.Sleep(100); // TTS flush buffers

                manualResetEvent.Reset();
                manualResetEvent = null;

                SpeechSynthesizer.SetOutputToNull(); // TTS flush buffers
                SpeechSynthesizer.Speak("null"); // TTS flush buffers
                //manualResetEvent.WaitOne();

                GeneratedAudioFilePath = filePath;
            }
            finally
            {
                if (delegateCompleted != null)
                {
                    SpeechSynthesizer.SpeakCompleted -= delegateCompleted;
                }
                if (delegateProgress != null)
                {
                    SpeechSynthesizer.SpeakProgress -= delegateProgress;
                }
            }
        }
    }

    public class AudioTTSGeneratorAutoAdvance : DualCancellableProgressReporter
    {
        private readonly AudioPaneViewModel m_viewModel;

        public AudioTTSGeneratorAutoAdvance(AudioPaneViewModel viewModel)
        {
            m_viewModel = viewModel;
        }

        public override void DoWork()
        {
            var pcmFormat = m_viewModel.m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat;

            if (pcmFormat.Data.BitDepth != 16)
            {
#if DEBUG
                Debugger.Break();
#endif
                return;
            }

            Tuple<TreeNode, TreeNode> treeNodeSelection = m_viewModel.m_UrakawaSession.GetTreeNodeSelection();
            TreeNode treeNode = treeNodeSelection.Item1;

            bool needsRefresh = false;
            bool skipDrawing = Settings.Default.AudioWaveForm_DisableDraw;
            Settings.Default.AudioWaveForm_DisableDraw = true;

            bool initial = true;
            try
            {
            next:
                var adjustedNode = TreeNode.EnsureTreeNodeHasNoSignificantTextOnlySiblings(treeNodeSelection.Item1, initial ? null : treeNode);
                if (adjustedNode == null)
                {
                    return;
                }

                if (adjustedNode.GetManagedAudioMedia() != null
                    || adjustedNode.GetFirstDescendantWithManagedAudio() != null)
                {
#if DEBUG
                    Debugger.Break();
#endif
                    return;
                }

                initial = false;

                var text = adjustedNode.GetTextFlattened(true);
                if (string.IsNullOrEmpty(text))
                {
#if DEBUG
                    Debugger.Break();
#endif
                    return;
                }

                var converter = new AudioTTSGenerator(text, pcmFormat.Data, m_viewModel.m_Recorder.RecordingDirectory, m_viewModel.m_SpeechSynthesizer);

                AddSubCancellable(converter);
                converter.DoWork();
                RemoveSubCancellable(converter);

                if (RequestCancellation)
                {
                    if (!string.IsNullOrEmpty(converter.GeneratedAudioFilePath)
                        && File.Exists(converter.GeneratedAudioFilePath))
                    {
                        File.Delete(converter.GeneratedAudioFilePath);
                    }
                    return;
                }

                if (!File.Exists(converter.GeneratedAudioFilePath))
                {
#if DEBUG
                    Debugger.Break();
#endif
                    RequestCancellation = true;
                    return;
                }

                //var manualResetEvent = new ManualResetEvent(false);
                m_viewModel.TheDispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
               {
                   TreeNode sub = adjustedNode == treeNodeSelection.Item1 ? null : adjustedNode;
                   Tuple<TreeNode, TreeNode> newSelection = m_viewModel.m_UrakawaSession.PerformTreeNodeSelection(treeNodeSelection.Item1, false, sub);
                   if (newSelection.Item1 != treeNodeSelection.Item1 || newSelection.Item2 != sub)
                   {
#if DEBUG
                       Debugger.Break();
#endif
                       RequestCancellation = true;
                       return;
                   }

                   TreeNode selectedNode = newSelection.Item2 ?? newSelection.Item1;
                   if (selectedNode.GetManagedAudioMedia() != null
                       || selectedNode.GetFirstDescendantWithManagedAudio() != null)
                   {
#if DEBUG
                       Debugger.Break();
#endif
                       RequestCancellation = true;
                       return;
                   }

                   m_viewModel.openFile(converter.GeneratedAudioFilePath, true, true, pcmFormat);

                   needsRefresh = true;

                   //m_viewModel.CommandRefresh.Execute();
                   //if (m_viewModel.View != null)
                   //{
                   //    m_viewModel.View.CancelWaveFormLoad(true);
                   //}
               }));

                if (RequestCancellation)
                {
                    if (!string.IsNullOrEmpty(converter.GeneratedAudioFilePath)
                        && File.Exists(converter.GeneratedAudioFilePath))
                    {
                        File.Delete(converter.GeneratedAudioFilePath);
                    }
                    return;
                }

                treeNode = adjustedNode.GetNextSiblingWithText(true);
                while (treeNode != null && (treeNode.GetXmlElementQName() == null
                        || TreeNode.TextOnlyContainsPunctuation(treeNode.GetText(true).Trim())
                        ))
                {
                    treeNode = treeNode.GetNextSiblingWithText(true);
                }

                if (treeNode == null)
                {
                    return;
                }

                goto next;
            }
            catch (Exception ex)
            {
#if DEBUG
                Debugger.Break();
#endif
                RequestCancellation = true;
                return;
            }
            finally
            {
                m_viewModel.TheDispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    Settings.Default.AudioWaveForm_DisableDraw = skipDrawing;

                    if (needsRefresh)
                    {
                        m_viewModel.CommandRefresh.Execute();
                    }
                }));
            }
        }
    }

    public partial class AudioPaneViewModel
    {
        private void CommandGenTTS_Execute()
        {
            bool cancelled = false;

            m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction(Tobi_Plugin_AudioPane_Lang.GeneratingTTSAudio, Tobi_Plugin_AudioPane_Lang.CmdAudioGenTTS_LongDesc);
            try
            {
                CommandSelectAll.Execute();
                CommandDeleteAudioSelection.Execute();
                CommandRefresh.Execute();

                var converter = new AudioTTSGeneratorAutoAdvance(this);

                bool result = m_ShellView.RunModalCancellableProgressTask(true,
                    Tobi_Plugin_AudioPane_Lang.GeneratingTTSAudio,
                    converter,
                    () =>
                    {
                        Logger.Log(@"Audio TTS CANCELLED", Category.Debug, Priority.Medium);
                        cancelled = true;
                    },
                    () =>
                    {
                        Logger.Log(@"Audio TTS DONE", Category.Debug, Priority.Medium);
                        cancelled = false;
                    });

                if (cancelled)
                {
                    DebugFix.Assert(!result);
                }

                if (converter.RequestCancellation) // Exception, not user-triggered
                {
                    cancelled = true;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Debugger.Break();
#endif
                cancelled = true;
            }
            finally
            {
                if (cancelled)
                {
                    m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.CancelTransaction();

                    //TODO: waveform not refreshed !!

                    //m_LastSetPlayBytePosition = -1;

                    ////AudioPlayer_UpdateWaveFormPlayHead();
                    //if (View != null)
                    //{
                    //    View.RefreshUI_WaveFormPlayHead();
                    //}

                    ////RefreshWaveFormChunkMarkersForCurrentSubTreeNode(false);

                    //if (View != null)
                    //{
                    //    View.ResetAll();
                    //}

                    //if (AudioPlaybackStreamKeepAlive)
                    //{
                    //    ensurePlaybackStreamIsDead();
                    //}
                    //if (m_CurrentAudioStreamProvider() != null)
                    //{
                    //    m_StateToRestore = null;
                    //    CommandRefresh.Execute();
                    //}
                }
                else
                {
                    m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();
                }
            }
        }

        private void CommandGenTTS_ExecuteOLDFLICKERINGDIALOG()
        {
            Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
            TreeNode treeNode = treeNodeSelection.Item1;

            bool cancelled = false;

            bool initial = true;
            m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction(Tobi_Plugin_AudioPane_Lang.GeneratingTTSAudio, Tobi_Plugin_AudioPane_Lang.CmdAudioGenTTS_LongDesc);
            try
            {
                CommandSelectAll.Execute();
                CommandDeleteAudioSelection.Execute();
                CommandRefresh.Execute();

            next:
                var adjustedNode = TreeNode.EnsureTreeNodeHasNoSignificantTextOnlySiblings(treeNodeSelection.Item1, initial ? null : treeNode);
                if (adjustedNode == null)
                {
                    return;
                }
                initial = false;

                var text = adjustedNode.GetTextFlattened(true);
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                var pcmFormat = m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat;

                var converter = new AudioTTSGenerator(text, pcmFormat.Data, m_Recorder.RecordingDirectory, m_SpeechSynthesizer);

                bool result = m_ShellView.RunModalCancellableProgressTask(true,
                    Tobi_Plugin_AudioPane_Lang.GeneratingTTSAudio,
                    converter,
                    () =>
                    {
                        Logger.Log(@"Audio TTS CANCELLED", Category.Debug, Priority.Medium);
                        cancelled = true;
                    },
                    () =>
                    {
                        Logger.Log(@"Audio TTS DONE", Category.Debug, Priority.Medium);
                        cancelled = false;
                    });

                if (cancelled)
                {
                    DebugFix.Assert(!result);

                    if (!string.IsNullOrEmpty(converter.GeneratedAudioFilePath)
                        && File.Exists(converter.GeneratedAudioFilePath))
                    {
                        File.Delete(converter.GeneratedAudioFilePath);
                    }
                    return;
                }

                if (!File.Exists(converter.GeneratedAudioFilePath))
                {
                    return;
                }

                TreeNode sub = adjustedNode == treeNodeSelection.Item1 ? null : adjustedNode;
                Tuple<TreeNode, TreeNode> newSelection = m_UrakawaSession.PerformTreeNodeSelection(treeNodeSelection.Item1, false, sub);
                if (newSelection.Item1 != treeNodeSelection.Item1 || newSelection.Item2 != sub)
                {
                    return;
                }

                openFile(converter.GeneratedAudioFilePath, true, true, pcmFormat);

                CommandRefresh.Execute();
                if (View != null)
                {
                    View.CancelWaveFormLoad(true);
                }

                treeNode = adjustedNode.GetNextSiblingWithText(true);
                while (treeNode != null && (treeNode.GetXmlElementQName() == null
                        || TreeNode.TextOnlyContainsPunctuation(treeNode.GetText(true).Trim())
                        ))
                {
                    treeNode = treeNode.GetNextSiblingWithText(true);
                }

                if (treeNode == null)
                {
                    return;
                }

                goto next;
            }
            catch (Exception ex)
            {
                cancelled = true;
            }
            finally
            {
                if (cancelled)
                {
                    m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.CancelTransaction();

                    //TODO: waveform not refreshed !!

                    //m_LastSetPlayBytePosition = -1;

                    ////AudioPlayer_UpdateWaveFormPlayHead();
                    //if (View != null)
                    //{
                    //    View.RefreshUI_WaveFormPlayHead();
                    //}

                    ////RefreshWaveFormChunkMarkersForCurrentSubTreeNode(false);

                    //if (View != null)
                    //{
                    //    View.ResetAll();
                    //}

                    //if (AudioPlaybackStreamKeepAlive)
                    //{
                    //    ensurePlaybackStreamIsDead();
                    //}
                    //if (m_CurrentAudioStreamProvider() != null)
                    //{
                    //    m_StateToRestore = null;
                    //    CommandRefresh.Execute();
                    //}
                }
                else
                {
                    m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();
                }
            }


            //Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
            //TreeNode node = treeNodeSelection.Item2 ?? treeNodeSelection.Item1;
            //if (node == null) return;

            //var text = node.GetTextMediaFlattened(true);
            //if (string.IsNullOrEmpty(text)) return;

            //bool cancelled = false;

            //var pcmFormat = m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat;

            //var converter = new AudioTTSGenerator(text, pcmFormat.Data, m_Recorder.RecordingDirectory, m_SpeechSynthesizer);

            //bool result = m_ShellView.RunModalCancellableProgressTask(true,
            //    Tobi_Plugin_AudioPane_Lang.GeneratingTTSAudio,
            //    converter,
            //    () =>
            //    {
            //        Logger.Log(@"Audio TTS CANCELLED", Category.Debug, Priority.Medium);
            //        cancelled = true;
            //    },
            //    () =>
            //    {
            //        Logger.Log(@"Audio TTS DONE", Category.Debug, Priority.Medium);
            //        cancelled = false;
            //    });

            //if (cancelled)
            //{
            //    DebugFix.Assert(!result);

            //    if (!string.IsNullOrEmpty(converter.GeneratedAudioFilePath)
            //        && File.Exists(converter.GeneratedAudioFilePath))
            //    {
            //        File.Delete(converter.GeneratedAudioFilePath);
            //    }
            //    return;
            //}

            //if (!File.Exists(converter.GeneratedAudioFilePath))
            //{
            //    return;
            //}

            //if (treeNodeSelection.Item2 == null)
            //{
            //    CommandSelectAll.Execute();
            //}
            //else
            //{
            //    long byteOffset = getByteOffset(treeNodeSelection.Item2, null);
            //    SelectChunk(byteOffset);
            //}

            //openFile(converter.GeneratedAudioFilePath, true, true, pcmFormat);
        }


        internal SpeechSynthesizer m_SpeechSynthesizer = new SpeechSynthesizer();

        private List<VoiceInfo> m_TTSVoices;
        public List<VoiceInfo> TTSVoices
        {
            get
            {
                m_TTSVoices = new List<VoiceInfo>();
                try
                {
                    foreach (var installedVoice in m_SpeechSynthesizer.GetInstalledVoices())
                    {
                        m_TTSVoices.Add(installedVoice.VoiceInfo);
                    }
                }
                catch (Exception ex_)
                {
#if DEBUG
                    Debugger.Break();
#endif
                }
                return m_TTSVoices;
            }
        }

        public VoiceInfo TTSVoice
        {
            get
            {
                try
                {
                    if (m_TTSVoices != null)
                        foreach (var voice in m_TTSVoices)
                        {
                            if (voice.Name == m_SpeechSynthesizer.Voice.Name)
                                return voice;
                        }
                    return m_SpeechSynthesizer.Voice;
                }
                catch (Exception ex_)
                {
#if DEBUG
                    Debugger.Break();
#endif
                }
                return null;
            }
            set
            {
                try
                {
                    if (value != null
                        && (m_SpeechSynthesizer.Voice.Name != value.Name))
                    {
                        Settings.Default.Audio_TTS_Voice = value.Name;
                    }
                }
                catch (Exception ex_)
                {
#if DEBUG
                    Debugger.Break();
#endif
                }
            }
        }
    }
}
