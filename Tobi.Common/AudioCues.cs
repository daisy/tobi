using System;
using System.IO;
using System.Media;

namespace Tobi.Common
{
    public static class AudioCues
    {
        private static void playAudioCue(string audioClipName)
        {
            string audioClipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                audioClipName);
            if (File.Exists(audioClipPath))
            {
                new SoundPlayer(audioClipPath).Play();
            }
        }

        public static void PlayHi()
        {
            if (!Settings.Default.EnableAudioCues) return;
            playAudioCue(@"hi.wav");
        }

        public static void PlayTock()
        {
            if (!Settings.Default.EnableAudioCues) return;
            playAudioCue(@"tock.wav");
        }

        public static void PlayTockTock()
        {
            if (!Settings.Default.EnableAudioCues) return;
            playAudioCue(@"tocktock.wav");
        }
        public static void PlayBeep()
        {
            if (!Settings.Default.EnableAudioCues) return;
            SystemSounds.Beep.Play();
        }
        public static void PlayAsterisk()
        {
            if (!Settings.Default.EnableAudioCues) return;
            SystemSounds.Asterisk.Play();
        }
        public static void PlayExclamation()
        {
            if (!Settings.Default.EnableAudioCues) return;
            SystemSounds.Exclamation.Play();
        }
        public static void PlayHand()
        {
            if (!Settings.Default.EnableAudioCues) return;
            SystemSounds.Hand.Play();
        }
        public static void PlayQuestion()
        {
            if (!Settings.Default.EnableAudioCues) return;
            SystemSounds.Question.Play();
        }
    }
}