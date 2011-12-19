using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AudioLib;
using Microsoft.Practices.Composite.Logging;

namespace Tobi.Plugin.AudioPane
{
    public partial class AudioPaneView
    {
        private double m_SelectionBackup_X = 0;
        private double m_SelectionBackup_Width = 0;

        private double m_TimeSelectionLeftX = -1;

        public void SelectAll()
        {
            m_Logger.Log("AudioPaneView.SelectAll", Category.Debug, Priority.Medium);

            m_TimeSelectionLeftX = 0;
            WaveFormTimeSelectionRect.Visibility = Visibility.Visible;

            double width = MillisecondsPerPixelToPixelWidthConverter.calc(ZoomSlider.Value, m_ViewModel);
#if DEBUG
            double widthReal = getWaveFormWidth();
            DebugFix.Assert((long)Math.Round(width * 100) == (long)Math.Round(widthReal * 100));
#endif //DEBUG

            WaveFormTimeSelectionRect.Width = width;
            WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, m_TimeSelectionLeftX);
        }

        public void SetSelectionBytes(long begin, long end)
        {
            //m_Logger.Log("AudioPaneView.SetSelectionBytes", Category.Debug, Priority.Medium);

            m_TimeSelectionLeftX = begin / BytesPerPixel;
            WaveFormTimeSelectionRect.Visibility = Visibility.Visible;
            WaveFormTimeSelectionRect.Width = end / BytesPerPixel - m_TimeSelectionLeftX;
            WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, m_TimeSelectionLeftX);
        }

        public void ClearSelection()
        {
            //m_Logger.Log("AudioPaneView.ClearSelection", Category.Debug, Priority.Medium);

            m_TimeSelectionLeftX = -1;
            WaveFormTimeSelectionRect.Visibility = Visibility.Hidden;
            WaveFormTimeSelectionRect.Width = 0;
            WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, m_TimeSelectionLeftX);
        }

        public bool IsSelectionSet
        {
            get { return m_TimeSelectionLeftX >= 0; }
        }

        public void ZoomSelection()
        {
            m_Logger.Log("AudioPaneView.OnZoomSelection", Category.Debug, Priority.Medium);

            if (m_TimeSelectionLeftX < 0)
            {
                return;
            }

            double widthToUse = WaveFormScroll.ViewportWidth;
            if (double.IsNaN(widthToUse) || widthToUse == 0)
            {
                widthToUse = WaveFormScroll.ActualWidth;
            }

            widthToUse -= 20;


            if (!m_ViewModel.State.Audio.HasContent)
            {
                // resets to MillisecondsPerPixelToPixelWidthConverter.defaultWidth

                m_ForceCanvasWidthUpdate = false;
                ZoomSlider.Value += 1;
                return;
            }

            double width = MillisecondsPerPixelToPixelWidthConverter.calc(ZoomSlider.Value, m_ViewModel);
#if DEBUG
            double widthReal = getWaveFormWidth();
            DebugFix.Assert((long)Math.Round(width * 100) == (long)Math.Round(widthReal * 100));
#endif //DEBUG

            double newWidth = width * (widthToUse / WaveFormTimeSelectionRect.Width);

            //if (newWidth > 8000)
            //{
            //    newWidth = 8000; //safeguard...image too large
            //}

            long bytesPerPixel = m_ViewModel.State.Audio.DataLength / (long)Math.Round(newWidth);
            bytesPerPixel =
                m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(bytesPerPixel);

            double millisecondsPerPixel = m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.ConvertBytesToTime(bytesPerPixel) / (double)AudioLibPCMFormat.TIME_UNIT;

            if (millisecondsPerPixel < ZoomSlider.Minimum)
            {
                ZoomSlider.Minimum = millisecondsPerPixel;
            }
            if (millisecondsPerPixel > ZoomSlider.Maximum)
            {
                ZoomSlider.Maximum = millisecondsPerPixel;
            }

            if (m_ViewModel.State.Audio.HasContent)
            {
                long selectionByteLeft =  m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize((long)Math.Round(m_TimeSelectionLeftX * BytesPerPixel));

                long selectionByteRight = m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize((long)Math.Round((m_TimeSelectionLeftX + WaveFormTimeSelectionRect.Width) * BytesPerPixel));

                if (m_ViewModel.PlayBytePosition < selectionByteLeft || m_ViewModel.PlayBytePosition > selectionByteRight)
                {
                    m_ViewModel.SetPlayHeadTimeBypassAutoPlay(selectionByteLeft);
                }
            }

            m_ForceCanvasWidthUpdate = false;
            ZoomSlider.Value = millisecondsPerPixel;
        }


        public double GetSelectionLeft()
        {
            return m_TimeSelectionLeftX;
        }

        public double GetSelectionWidth()
        {
            return WaveFormTimeSelectionRect.Width;
        }

        private const double MIN_SELECTION_PIXELS = 6;

        private void selectionFinished(double x)
        {
            WaveFormTimeSelectionRectBackup.Visibility = Visibility.Hidden;

            if (x == m_TimeSelectionLeftX)
            {
                ClearSelection();

                m_ViewModel.CommandClearSelection.Execute();
                //m_ViewModel.State.Selection.ClearSelection();

                if (!m_ViewModel.State.Audio.HasContent)
                {
                    return;
                }

                long bytes = m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize((long)Math.Round(x * BytesPerPixel));
                m_ViewModel.PlayBytePosition = bytes;

                return;
            }

            if (Math.Abs(m_TimeSelectionLeftX - x) <= MIN_SELECTION_PIXELS)
            {
                ClearSelection();
                m_TimeSelectionLeftX = x;
            }

            if (x == m_TimeSelectionLeftX)
            {
                restoreSelection();

                if (!m_ViewModel.State.Audio.HasContent)
                {
                    return;
                }

                long bytes = m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize((long)Math.Round(x * BytesPerPixel));
                m_ViewModel.PlayBytePosition = bytes;

                return;
            }

            double right = x;

            if (x < m_TimeSelectionLeftX)
            {
                right = m_TimeSelectionLeftX;
                m_TimeSelectionLeftX = x;
            }

            WaveFormTimeSelectionRect.Visibility = Visibility.Visible;
            WaveFormTimeSelectionRect.Width = right - m_TimeSelectionLeftX;
            WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, m_TimeSelectionLeftX);

            if (!m_ViewModel.State.Audio.HasContent)
            {
                return;
            }

            m_ViewModel.State.Selection.SetSelectionBytes(
                m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(
                (long)Math.Round(m_TimeSelectionLeftX * BytesPerPixel)),
                m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(
                (long)Math.Round((m_TimeSelectionLeftX + WaveFormTimeSelectionRect.Width) * BytesPerPixel)));
        }

        private void restoreSelection()
        {
            m_TimeSelectionLeftX = m_SelectionBackup_X;
            WaveFormTimeSelectionRect.Visibility = Visibility.Visible;
            WaveFormTimeSelectionRect.Width = m_SelectionBackup_Width;
            WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, m_TimeSelectionLeftX);

            if (m_TimeSelectionLeftX < 0)
            {
                m_ViewModel.State.Selection.ClearSelection();
            }
            else if (m_ViewModel.State.Audio.HasContent)
            {
                m_ViewModel.State.Selection.SetSelectionBytes(
                    m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(
                    (long)Math.Round(m_TimeSelectionLeftX * BytesPerPixel)),
                    m_ViewModel.State.Audio.GetCurrentPcmFormat().Data.AdjustByteToBlockAlignFrameSize(
                    (long)Math.Round((m_TimeSelectionLeftX + WaveFormTimeSelectionRect.Width) * BytesPerPixel)));
            }
        }

        private void backupSelection()
        {
            m_SelectionBackup_X = m_TimeSelectionLeftX;
            m_SelectionBackup_Width = WaveFormTimeSelectionRect.Width;

            WaveFormTimeSelectionRectBackup.Visibility = Visibility.Visible;
            WaveFormTimeSelectionRectBackup.Width = m_SelectionBackup_Width;
            WaveFormTimeSelectionRectBackup.SetValue(Canvas.LeftProperty, m_SelectionBackup_X);
        }
    }
}