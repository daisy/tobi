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
using System.Text;
using System.Threading;
using System.Windows;
using urakawa.data;
using urakawa.ExternalFiles;

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

            {
                var currentSpeechSynthesizer = new SpeechSynthesizer();
                SpeechSynthesizer.Rate = currentSpeechSynthesizer.Rate;
            }
        }

        public string GeneratedAudioFilePath
        {
            get;
            private set;
        }

        private static long m_ttsFileNameCounter;

        public override void DoWork()
        {
#if ENABLE_TTS_SPEAK_PROGRESS
            EventHandler<SpeakProgressEventArgs> delegateProgress = null;
#endif

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

                bool done = false;
                
#if ENABLE_TTS_SPEAK_PROGRESS
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

                    int percent = 100 * (ev.CharacterPosition + ev.CharacterCount) / Text.Length;
                    if (percent < 0) percent = 0;
                    if (percent > 100) percent = 100;

                    //Console.WriteLine("%%%%%%%% " + percent);

                    reportProgress_Throttle(percent, msg);
                };

                SpeechSynthesizer.SpeakProgress += delegateProgress;
#endif

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

                //Console.WriteLine(@"TTS GEN: " + Text);

                SpeechSynthesizer.SpeakAsync(Text);
                manualResetEvent.WaitOne();

                done = true;

                SpeechSynthesizer.SpeakCompleted -= delegateCompleted;

#if ENABLE_TTS_SPEAK_PROGRESS
                SpeechSynthesizer.SpeakProgress -= delegateProgress;
#endif

                Thread.Sleep(100); // TTS flush buffers

                manualResetEvent.Reset();
                manualResetEvent = null;

                SpeechSynthesizer.SetOutputToNull(); // TTS flush buffers
                //SpeechSynthesizer.Speak("null"); // TTS flush buffers
                //manualResetEvent.WaitOne();

                GeneratedAudioFilePath = filePath;
            }
            finally
            {
                if (delegateCompleted != null)
                {
                    SpeechSynthesizer.SpeakCompleted -= delegateCompleted;
                }

#if ENABLE_TTS_SPEAK_PROGRESS
                if (delegateProgress != null)
                {
                    SpeechSynthesizer.SpeakProgress -= delegateProgress;
                }
#endif
            }
        }
    }

    public class AudioTTSGeneratorAutoAdvance : DualCancellableProgressReporter
    {
        private readonly AudioPaneViewModel m_viewModel;
        private readonly Dictionary<string, string> m_ttsVoiceMap;

        public AudioTTSGeneratorAutoAdvance(AudioPaneViewModel viewModel, Dictionary<string, string> ttsVoiceMap)
        {
            m_viewModel = viewModel;
            m_ttsVoiceMap = ttsVoiceMap;
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

            //m_viewModel.m_UrakawaSession.PerformanceFlag = true;

#if !ENABLE_TTS_SPEAK_PROGRESS
            int percent = 0;
#endif

            bool initial = true;
            try
            {
                TreeNode adjustedNode = null;
                next:

                if (m_viewModel.IsSimpleMode)
                {
                    adjustedNode = treeNodeSelection.Item1;
                }
                else
                {
                    TreeNode root = treeNodeSelection.Item1;
                    if (initial)
                    {
                        var lname = root.GetXmlElementLocalName();
                        if (lname != null && (lname.Equals("math", StringComparison.OrdinalIgnoreCase) ||
                            lname.Equals("svg", StringComparison.OrdinalIgnoreCase)))
                        {
                            adjustedNode = root;
                        }
                        else
                        {
                            adjustedNode = TreeNode.NavigateInsideSignificantText(root);
                        }

                        //TreeNode candidate = m_viewModel.m_UrakawaSession.AdjustTextSyncGranularity(adjustedNode, adjustedNode);
                        //if (candidate != null)
                        //{
                        //    treeNode = candidate;
                        //}
                    }
                    else
                    {
                        adjustedNode = treeNode;
                    }

                    if (adjustedNode == null
                        || !adjustedNode.IsDescendantOf(root) && adjustedNode != root)
                    {
                        return;
                    }

                    TreeNode math_ = adjustedNode.GetFirstAncestorWithXmlElement("math");
                    if (math_ != null)
                    {
                        adjustedNode = math_;
                    }
                    else
                    {
                        TreeNode svg_ = adjustedNode.GetFirstAncestorWithXmlElement("svg");
                        if (svg_ != null)
                        {
                            adjustedNode = svg_;
                        }
                        //else
                        //{
                        //    TreeNode candidate = m_viewModel.m_UrakawaSession.AdjustTextSyncGranularity(adjustedNode, adjustedNode);
                        //    if (candidate != null)
                        //    {
                        //        adjustedNode = candidate;
                        //    }
                        //}
                    }

                    if (adjustedNode == null
                        || !adjustedNode.IsDescendantOf(root) && adjustedNode != root)
                    {
                        return;
                    }
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

                var text = adjustedNode.GetTextFlattened();
                if (string.IsNullOrEmpty(text))
                {
#if DEBUG
                    Debugger.Break();
#endif
                    return;
                }

#if DEBUG
                DebugFix.Assert(TreeNode.GetLengthStringChunks(adjustedNode.GetTextFlattened_()) != 0);
#endif

                m_viewModel.SelectVoiceTTS(Settings.Default.Audio_TTS_Voice);
                //m_viewModel.m_SpeechSynthesizer.SelectVoice(Settings.Default.Audio_TTS_Voice);

                var adjustedNodeName = adjustedNode.GetXmlElementLocalName();
                if (adjustedNodeName != null)
                {
                    foreach (string elementName in m_ttsVoiceMap.Keys)
                    {
                        if (elementName.Equals(adjustedNodeName, StringComparison.OrdinalIgnoreCase))
                        {
                            string voiceName = m_ttsVoiceMap[elementName];

                            m_viewModel.SelectVoiceTTS(voiceName);
                            //m_viewModel.m_SpeechSynthesizer.SelectVoice(voiceName);

                            break;
                        }
                    }
                }

#if !ENABLE_TTS_SPEAK_PROGRESS
                percent += 10;
                if (percent > 100) percent = 10;

                string msg = Tobi_Plugin_AudioPane_Lang.GeneratingTTSAudio + " [" + (text.Length > 20 ? text.Substring(0, 19) + "..." : text) + "]";
                reportProgress_Throttle(percent, msg);
#endif

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
                   TreeNode sub = (adjustedNode == treeNodeSelection.Item1 ? null : adjustedNode);
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

#if DEBUG
                   Tuple<TreeNode, TreeNode> treeNodeSelectionCheck = m_viewModel.m_UrakawaSession.GetTreeNodeSelection();
                   if (newSelection.Item1 != treeNodeSelectionCheck.Item1)
                   {
                       Debugger.Break();
                   }
                   if (newSelection.Item2 != treeNodeSelectionCheck.Item2)
                   {
                       Debugger.Break();
                   }
#endif


                   m_viewModel.openFile(converter.GeneratedAudioFilePath, true, true, pcmFormat);

                   needsRefresh = true;

                   //m_viewModel.CommandRefresh.Execute();
                   //if (m_viewModel.View != null)
                   //{
                   //    m_viewModel.View.CancelWaveFormLoad(true);
                   //}
               }));

#if DEBUG
                if (m_viewModel.TheDispatcher.CheckAccess())
                {
                    Debugger.Break();
                }
#endif

                //Thread.Sleep(100);

                ////Action EmptyDelegate = delegate() { };
                //m_viewModel.TheDispatcher.Invoke(DispatcherPriority.Background, (Action)(() =>
                //{
                //    //nop
                //}));
                ////m_viewModel.m_ShellView.PumpDispatcherFrames(DispatcherPriority.Background);


                if (RequestCancellation)
                {
                    if (!string.IsNullOrEmpty(converter.GeneratedAudioFilePath)
                        && File.Exists(converter.GeneratedAudioFilePath))
                    {
                        File.Delete(converter.GeneratedAudioFilePath);
                    }
                    return;
                }

                //TreeNode nested;
                //treeNode = TreeNode.GetNextTreeNodeWithNoSignificantTextOnlySiblings(false, adjustedNode, out nested);
                treeNode = TreeNode.NavigatePreviousNextSignificantText(false, adjustedNode);
                if (treeNode == null)
                {
                    return;
                }

                TreeNode math = treeNode.GetFirstAncestorWithXmlElement("math");
                if (math != null)
                {
                    treeNode = math;
                }
                else
                {
                    TreeNode svg = treeNode.GetFirstAncestorWithXmlElement("svg");
                    if (svg != null)
                    {
                        treeNode = svg;
                    }
                    //else
                    //{
                    //    TreeNode candidate = m_viewModel.m_UrakawaSession.AdjustTextSyncGranularity(treeNode, adjustedNode);
                    //    if (candidate != null)
                    //    {
                    //        treeNode = candidate;
                    //    }
                    //}
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
                //m_viewModel.m_UrakawaSession.PerformanceFlag = false;

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
        public static readonly string TTS_VOICE_MAPPING_DIRECTORY =
            Path.Combine(ExternalFilesDataManager.STORAGE_FOLDER_PATH, "TTS");
        public static readonly string TTS_VOICE_MAPPING_FILE =
            Path.Combine(TTS_VOICE_MAPPING_DIRECTORY, "voices.txt");

        public Dictionary<string, string> readTTSVoicesMapping(out string text)
        {
            text = "";

            Dictionary<string, string> ttsVoiceMap = new Dictionary<string, string>();

            string prefix_VOICE = "[TTS_VOICE]";

            List<VoiceInfo> voices = TTSVoices;

            if (!File.Exists(TTS_VOICE_MAPPING_FILE))
            {
                if (!Directory.Exists(TTS_VOICE_MAPPING_DIRECTORY))
                {
                    FileDataProvider.CreateDirectory(TTS_VOICE_MAPPING_DIRECTORY);
                }

                StreamWriter streamWriter = new StreamWriter(TTS_VOICE_MAPPING_FILE, false, Encoding.UTF8);
                try
                {
                    string line = null;
                    foreach (VoiceInfo voice in voices)
                    {
                        line = prefix_VOICE + " " + voice.Name;
                        streamWriter.WriteLine(line);
                        text += (line + '\n');

                        line = "element_name_1";
                        streamWriter.WriteLine(line);
                        text += (line + '\n');

                        line = "element_name_2";
                        streamWriter.WriteLine(line);
                        text += (line + '\n');

                        line = "etc.";
                        streamWriter.WriteLine(line);
                        text += (line + '\n');

                        streamWriter.WriteLine("");
                        text += ("" + '\n');

                        //// useless for now, could be used when ttsVoiceMap is hard-coded here?
                        //foreach (String key in ttsVoiceMap.Keys)
                        //{
                        //    string name = ttsVoiceMap[key];
                        //    if (name.Equals(voice.Name, StringComparison.Ordinal))
                        //    {
                        //        streamWriter.WriteLine(key);
                        //    }
                        //}
                    }
                }
                finally
                {
                    streamWriter.Close();
                }
            }
            else
            {
                List<string> voicesPresent = new List<string>();

                string line = null;

                StreamReader streamReader = new StreamReader(TTS_VOICE_MAPPING_FILE, Encoding.UTF8);
                try
                {
                    string currentVoice = null;

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        text += (line + '\n');

                        line = line.Trim();
                        if (string.IsNullOrEmpty(line)) continue;

                        if (line.StartsWith(prefix_VOICE))
                        {
                            currentVoice = line.Substring(prefix_VOICE.Length).Trim();
                            if (!voicesPresent.Contains(currentVoice))
                            {
                                voicesPresent.Add(currentVoice);
                            }

                            if (!string.IsNullOrEmpty(currentVoice))
                            {
                                bool found = false;
                                foreach (VoiceInfo voice in voices)
                                {
                                    if (currentVoice.Equals(voice.Name, StringComparison.Ordinal))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    currentVoice = null;
                                }
                            }
                        }
                        else if (!string.IsNullOrEmpty(currentVoice))
                        {
                            if (ttsVoiceMap.ContainsKey(line))
                            {
                                ttsVoiceMap.Remove(line);
                            }
                            ttsVoiceMap.Add(line, currentVoice);
                        }
                    }
                }
                finally
                {
                    streamReader.Close();
                }

                foreach (VoiceInfo voice in voices)
                {
                    if (voicesPresent.Contains(voice.Name))
                    {
                        continue;
                    }

                    text += ("" + '\n');

                    line = prefix_VOICE + " " + voice.Name;
                    text += (line + '\n');

                    line = "element_name_1";
                    text += (line + '\n');

                    line = "element_name_2";
                    text += (line + '\n');

                    line = "etc.";
                    text += (line + '\n');

                    text += ("" + '\n');
                }
            }

            return ttsVoiceMap;
        }

        private void CommandGenTTS_All_Execute()
        {
            m_UrakawaSession.PerformTreeNodeSelection(m_UrakawaSession.DocumentProject.Presentations.Get(0).RootNode, false, null);
            CommandGenTTS_Execute();
        }

        private void CommandGenTTS_Execute()
        {
            string text;
            Dictionary<string, string> ttsVoiceMap = readTTSVoicesMapping(out text);

            bool cancelled = false;

            m_UrakawaSession.AutoSave_OFF();

            m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction(Tobi_Plugin_AudioPane_Lang.GeneratingTTSAudio, Tobi_Plugin_AudioPane_Lang.CmdAudioGenTTS_LongDesc, COMMAND_TRANSATION_ID__AUDIO_TTS);
            try
            {
                CommandSelectAll.Execute();
                CommandDeleteAudioSelection.Execute();
                CommandRefresh.Execute();

                m_TTSGen = true;

                var converter = new AudioTTSGeneratorAutoAdvance(this, ttsVoiceMap);

                bool error = m_ShellView.RunModalCancellableProgressTask(true,
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
                    //DebugFix.Assert(!result);
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
                m_TTSGen = false;
                m_UrakawaSession.AutoSave_ON();

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

        //private void CommandGenTTS_ExecuteOLDFLICKERINGDIALOG()
        //{
        //    Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
        //    TreeNode treeNode = treeNodeSelection.Item1;

        //    bool cancelled = false;

        //    bool initial = true;
        //    m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.StartTransaction(Tobi_Plugin_AudioPane_Lang.GeneratingTTSAudio, Tobi_Plugin_AudioPane_Lang.CmdAudioGenTTS_LongDesc);
        //    try
        //    {
        //        CommandSelectAll.Execute();
        //        CommandDeleteAudioSelection.Execute();
        //        CommandRefresh.Execute();

        //    next:
        //        var adjustedNode = TreeNode.EnsureTreeNodeHasNoSignificantTextOnlySiblings(false, treeNodeSelection.Item1, initial ? null : treeNode);
        //        if (adjustedNode == null)
        //        {
        //            return;
        //        }
        //        initial = false;

        //        var text = adjustedNode.GetTextFlattened();
        //        if (string.IsNullOrEmpty(text))
        //        {
        //            return;
        //        }

        //        var pcmFormat = m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat;

        //        var converter = new AudioTTSGenerator(text, pcmFormat.Data, m_Recorder.RecordingDirectory, m_SpeechSynthesizer);

        //        bool result = m_ShellView.RunModalCancellableProgressTask(true,
        //            Tobi_Plugin_AudioPane_Lang.GeneratingTTSAudio,
        //            converter,
        //            () =>
        //            {
        //                Logger.Log(@"Audio TTS CANCELLED", Category.Debug, Priority.Medium);
        //                cancelled = true;
        //            },
        //            () =>
        //            {
        //                Logger.Log(@"Audio TTS DONE", Category.Debug, Priority.Medium);
        //                cancelled = false;
        //            });

        //        if (cancelled)
        //        {
        //            DebugFix.Assert(!result);

        //            if (!string.IsNullOrEmpty(converter.GeneratedAudioFilePath)
        //                && File.Exists(converter.GeneratedAudioFilePath))
        //            {
        //                File.Delete(converter.GeneratedAudioFilePath);
        //            }
        //            return;
        //        }

        //        if (!File.Exists(converter.GeneratedAudioFilePath))
        //        {
        //            return;
        //        }

        //        TreeNode sub = adjustedNode == treeNodeSelection.Item1 ? null : adjustedNode;
        //        Tuple<TreeNode, TreeNode> newSelection = m_UrakawaSession.PerformTreeNodeSelection(treeNodeSelection.Item1, false, sub);
        //        if (newSelection.Item1 != treeNodeSelection.Item1 || newSelection.Item2 != sub)
        //        {
        //            return;
        //        }

        //        openFile(converter.GeneratedAudioFilePath, true, true, pcmFormat);

        //        CommandRefresh.Execute();
        //        if (View != null)
        //        {
        //            View.CancelWaveFormLoad(true);
        //        }

        //        treeNode = adjustedNode.GetNextSiblingWithText();
        //        while (treeNode != null && (treeNode.GetXmlElementQName() == null
        //                || TreeNode.TextOnlyContainsPunctuation(treeNode.GetText())
        //                ))
        //        {
        //            treeNode = treeNode.GetNextSiblingWithText();
        //        }

        //        if (treeNode == null)
        //        {
        //            return;
        //        }

        //        goto next;
        //    }
        //    catch (Exception ex)
        //    {
        //        cancelled = true;
        //    }
        //    finally
        //    {
        //        if (cancelled)
        //        {
        //            m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.CancelTransaction();

        //            //TODO: waveform not refreshed !!

        //            //m_LastSetPlayBytePosition = -1;

        //            ////AudioPlayer_UpdateWaveFormPlayHead();
        //            //if (View != null)
        //            //{
        //            //    View.RefreshUI_WaveFormPlayHead();
        //            //}

        //            ////RefreshWaveFormChunkMarkersForCurrentSubTreeNode(false);

        //            //if (View != null)
        //            //{
        //            //    View.ResetAll();
        //            //}

        //            //if (AudioPlaybackStreamKeepAlive)
        //            //{
        //            //    ensurePlaybackStreamIsDead();
        //            //}
        //            //if (m_CurrentAudioStreamProvider() != null)
        //            //{
        //            //    m_StateToRestore = null;
        //            //    CommandRefresh.Execute();
        //            //}
        //        }
        //        else
        //        {
        //            m_UrakawaSession.DocumentProject.Presentations.Get(0).UndoRedoManager.EndTransaction();
        //        }
        //    }


        //    //Tuple<TreeNode, TreeNode> treeNodeSelection = m_UrakawaSession.GetTreeNodeSelection();
        //    //TreeNode node = treeNodeSelection.Item2 ?? treeNodeSelection.Item1;
        //    //if (node == null) return;

        //    //var text = node.GetTextMediaFlattened(true);
        //    //if (string.IsNullOrEmpty(text)) return;

        //    //bool cancelled = false;

        //    //var pcmFormat = m_UrakawaSession.DocumentProject.Presentations.Get(0).MediaDataManager.DefaultPCMFormat;

        //    //var converter = new AudioTTSGenerator(text, pcmFormat.Data, m_Recorder.RecordingDirectory, m_SpeechSynthesizer);

        //    //bool result = m_ShellView.RunModalCancellableProgressTask(true,
        //    //    Tobi_Plugin_AudioPane_Lang.GeneratingTTSAudio,
        //    //    converter,
        //    //    () =>
        //    //    {
        //    //        Logger.Log(@"Audio TTS CANCELLED", Category.Debug, Priority.Medium);
        //    //        cancelled = true;
        //    //    },
        //    //    () =>
        //    //    {
        //    //        Logger.Log(@"Audio TTS DONE", Category.Debug, Priority.Medium);
        //    //        cancelled = false;
        //    //    });

        //    //if (cancelled)
        //    //{
        //    //    DebugFix.Assert(!result);

        //    //    if (!string.IsNullOrEmpty(converter.GeneratedAudioFilePath)
        //    //        && File.Exists(converter.GeneratedAudioFilePath))
        //    //    {
        //    //        File.Delete(converter.GeneratedAudioFilePath);
        //    //    }
        //    //    return;
        //    //}

        //    //if (!File.Exists(converter.GeneratedAudioFilePath))
        //    //{
        //    //    return;
        //    //}

        //    //if (treeNodeSelection.Item2 == null)
        //    //{
        //    //    CommandSelectAll.Execute();
        //    //}
        //    //else
        //    //{
        //    //    long byteOffset = getByteOffset(treeNodeSelection.Item2, null);
        //    //    SelectChunk(byteOffset);
        //    //}

        //    //openFile(converter.GeneratedAudioFilePath, true, true, pcmFormat);
        //}


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
