namespace Tobi.Modules.AudioPane
{
    /// <summary>
    /// Minimal functionality exposed by the View, essentially to be consumed by the ViewModel
    /// in a technology-agnostic manner. In fact, the ViewModel could function without a real View
    /// (i.e the View can be mocked for testing purposes).
    /// </summary>
    public interface IAudioPaneView
    {
        // ReSharper disable InconsistentNaming
        void ResetAll();
        void RefreshUI_LoadWaveForm(bool wasPlaying, bool play);
        void RefreshUI_PeakMeter();
        void RefreshUI_PeakMeterBlackout(bool black);
        void RefreshUI_WaveFormPlayHead();
        void RefreshUI_WaveFormChunkMarkers(long bytesLeft, long bytesRight);
        // ReSharper restore InconsistentNaming

        void BringIntoFocus();
        void TimeMessageHide();
        void TimeMessageRefresh();
        void TimeMessageShow();
        void ShowHideWaveFormLoadingMessage(bool visible);
        void ResetWaveFormEmpty();
        string OpenFileDialog();
        double BytesPerPixel { get; set; }
        void StopWaveFormTimer();
        void StartWaveFormTimer();
        //void StopPeakMeterTimer();
        //void StartPeakMeterTimer();
        bool IsSelectionSet { get; }
        void ClearSelection();
        void SelectAll();
        void ZoomSelection();
        void ZoomFitFull();
        void InitGraphicalCommandBindings();
        void SetSelectionTime(double begin, double end);
        void SetSelectionBytes(long begin, long end);
    }
}
