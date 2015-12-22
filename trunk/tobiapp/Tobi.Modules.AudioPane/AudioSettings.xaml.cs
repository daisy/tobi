using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Speech.Synthesis;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Tobi.Common;
using Tobi.Common.UI;

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
            //ViewModel.m_ShellView.ExecuteShellProcess(AudioPaneViewModel.TTS_VOICE_MAPPING_DIRECTORY);

            string text;
            Dictionary<string, string> ttsVoiceMap = ViewModel.readTTSVoicesMapping(out text);

            var editBox = new TextBoxReadOnlyCaretVisible
            {
                Text = text,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.WrapWithOverflow
            };

            var windowPopup = new PopupModalWindow(ViewModel.m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_AudioPane_Lang.TTSVoiceMapping),
                                                   new ScrollViewer { Content = editBox },
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 500, 600, null, 40, null);

            windowPopup.EnableEnterKeyDefault = false;

            editBox.Loaded += new RoutedEventHandler((send, ev) =>
            {
                //editBox.SelectAll();
                FocusHelper.FocusBeginInvoke(editBox);
            });

            windowPopup.ShowModal();


            if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                string str = editBox.Text;
                if (string.IsNullOrEmpty(str))
                {
                    str = " ";
                }

                StreamWriter streamWriter = new StreamWriter(AudioPaneViewModel.TTS_VOICE_MAPPING_FILE, false, Encoding.UTF8);
                try
                {
                    streamWriter.Write(str);
                }
                finally
                {
                    streamWriter.Close();
                }

                string newText;
                ttsVoiceMap = ViewModel.readTTSVoicesMapping(out newText);
                //DebugFix.assert(newText.Equals(editBox.Text, StringComparison.Ordinal));

            }
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
                        ViewModel.SelectVoiceTTS(currentVoice.Name);
                        //ViewModel.m_SpeechSynthesizer.SelectVoice(currentVoice.Name);
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

                ViewModel.SelectVoiceTTS(ViewModel.TTSVoice.Name);
                //ViewModel.m_SpeechSynthesizer.SelectVoice(ViewModel.TTSVoice.Name);

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
