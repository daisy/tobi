using Tobi.Common.MVVM.Command;

namespace Tobi.Common
{
    public interface IAudioViewModel
    {
        bool IsPlaying { get; }
        bool IsRecording { get; }
        RichDelegateCommand CommandPlay { get; }
        RichDelegateCommand CommandPause { get; }
        RichDelegateCommand CommandStartRecord { get; }
        RichDelegateCommand CommandStopRecord { get; }
        RichDelegateCommand CommandStopRecordAndContinue { get; }
    }
}
