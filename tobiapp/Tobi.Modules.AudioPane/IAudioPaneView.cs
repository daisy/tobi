namespace Tobi.Plugin.AudioPane
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
        void ResetPeakLines();
        void ResetWaveFormChunkMarkers();
        void RefreshUI_LoadWaveForm(bool wasPlaying, bool onlyUpdateTiles);
        void RefreshUI_PeakMeter();
        void RefreshUI_PeakMeterBlackout(bool black);
        void RefreshUI_WaveFormPlayHead(bool scrollSelection);
        void RefreshUI_WaveFormChunkMarkers(long bytesLeft, long bytesRight);
        // ReSharper restore InconsistentNaming

        void BringIntoFocus();
        void BringIntoFocusStatusBar();
        void TimeMessageHide();
        void TimeMessageRefresh();
        void TimeMessageShow();
        void TimeMessageShowHide();
        void ShowHideWaveFormLoadingMessage(bool visible);
        void ResetWaveFormEmpty();
        string[] OpenFileDialog();
        double BytesPerPixel { get; set; }
        void StopWaveFormTimer();
        void StartWaveFormTimer();
        //void StopPeakMeterTimer();
        //void StartPeakMeterTimer();
        bool IsSelectionSet { get; }
        void ClearSelection();
        void SelectAll();
        void ZoomSelection();
        void InvalidateWaveFormOverlay();
        void ZoomFitFull();
        void InitGraphicalCommandBindings();
        //void Zoom_0();
        void Zoom_1();
        void Zoom_2();
        void Zoom_3();
        void Zoom_4();
        void Zoom_5();
        void Zoom_6();
        void Zoom_7();
        void Zoom_8();
        void Zoom_9();

        void SetSelectionBytes(long begin, long end);
        void CancelWaveFormLoad(bool interruptDrawingToo);
        //void RefreshCanvasWidth();
    }
}
