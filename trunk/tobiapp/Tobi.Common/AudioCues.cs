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
            playAudioCue(@"hi.wav");
        }

        public static void PlayTock()
        {
            playAudioCue(@"tock.wav");
        }

        public static void PlayTockTock()
        {
            playAudioCue(@"tocktock.wav");
        }
    }
}