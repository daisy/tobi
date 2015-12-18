using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;

namespace Tobi.Plugin.AudioPane
{
    /// <summary>
    /// Interaction logic for AudioSettings.xaml
    /// </summary>
    public partial class AudioSettings
    {
        public AudioSettings(AudioPaneViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();
        }

        public AudioPaneViewModel ViewModel
        {
            private set;
            get;
        }

        private void OnClick_ButtonTTSVoices(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> ttsVoiceMap = ViewModel.readTTSVoicesMapping();
            ViewModel.m_ShellView.ExecuteShellProcess(AudioPaneViewModel.TTS_VOICE_MAPPING_DIRECTORY);
        }

        private void OnClick_ButtonSpeak(object sender, RoutedEventArgs e)
        {
            //ViewModel.TTSVoice
            //ComboTTSVoices.SelectedValue
            //Settings.Default.Audio_TTS_Voice

            try
            {
                if (ViewModel.m_SpeechSynthesizer.State == SynthesizerState.Speaking)
                {
                    ViewModel.m_SpeechSynthesizer.SpeakAsyncCancelAll();
                    return;
                }

                ViewModel.m_SpeechSynthesizer.SpeakAsyncCancelAll();

                {
                    var currentSpeechSynthesizer = new SpeechSynthesizer();
                    ViewModel.m_SpeechSynthesizer.Rate = currentSpeechSynthesizer.Rate;
                }

                VoiceInfo currentVoice = ViewModel.m_SpeechSynthesizer.Voice;

                EventHandler<SpeakCompletedEventArgs> delegateCompleted = null;
                delegateCompleted = (s, ev) =>
                {
                    ViewModel.m_SpeechSynthesizer.SpeakCompleted -= delegateCompleted;

                    ViewModel.m_SpeechSynthesizer.SetOutputToNull(); // TTS flush buffers
                    //ViewModel.m_SpeechSynthesizer.Speak("null"); // TTS flush buffers

                    //ButtonTTSVoices.IsEnabled = true;
                    ComboTTSVoices.IsEnabled = true;

                    ViewModel.m_SpeechSynthesizer.SpeakAsyncCancelAll();

                    if (currentVoice != null)
                    {
                        try
                        {
                            ViewModel.m_SpeechSynthesizer.SelectVoice(currentVoice.Name);
                        }
                        catch (Exception ex_)
                        {
#if DEBUG
                            Debugger.Break();
#endif
                        }
                    }

                    if (ev.Cancelled)
                    {
                        int debug = 1;
                    }

                    var filePrompt = ev.Prompt as FilePrompt;
                    string str = ev.Prompt.ToString();
                    if (ViewModel.m_SpeechSynthesizer.State == SynthesizerState.Ready)
                    {
                        int debug = 1;
                    }
                    else
                    {
                        int debug = 1;
                    }
                };
                ViewModel.m_SpeechSynthesizer.SpeakCompleted += delegateCompleted;

                ViewModel.m_SpeechSynthesizer.SetOutputToDefaultAudioDevice();

                try
                {
                    ViewModel.m_SpeechSynthesizer.SelectVoice(ViewModel.TTSVoice.Name);
                }
                catch (Exception ex_)
                {
#if DEBUG
                    Debugger.Break();
#endif
                }

                //ButtonTTSVoices.IsEnabled = false;
                ComboTTSVoices.IsEnabled = false;

                ViewModel.m_SpeechSynthesizer.SpeakAsync(Tobi_Plugin_AudioPane_Lang.Speak_SampleText + " " + ViewModel.TTSVoice.Name);
            }
            catch (Exception ex)
            {
#if DEBUG
                Debugger.Break();
#endif
            }
        }
    }
}
