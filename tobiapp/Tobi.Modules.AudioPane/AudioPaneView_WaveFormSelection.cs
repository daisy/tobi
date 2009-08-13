using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Composite.Logging;

namespace Tobi.Modules.AudioPane
{
    public partial class AudioPaneView
    {
        private double m_SelectionBackup_X = 0;
        private double m_SelectionBackup_Width = 0;

        private double m_TimeSelectionLeftX = -1;

        public void SelectAll()
        {
            ViewModel.Logger.Log("AudioPaneView.SelectAll", Category.Debug, Priority.Medium);

            m_TimeSelectionLeftX = 0;
            WaveFormTimeSelectionRect.Visibility = Visibility.Visible;
            WaveFormTimeSelectionRect.Width = WaveFormCanvas.ActualWidth;
            WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, m_TimeSelectionLeftX);
        }

        public void SetSelectionTime(double begin, double end)
        {
            ViewModel.Logger.Log("AudioPaneView.SetSelectionTime", Category.Debug, Priority.Medium);

            if (ViewModel.State.Audio.HasContent)
            {
                long beginBytes = ViewModel.State.Audio.ConvertMillisecondsToBytes(begin);
                long endBytes = ViewModel.State.Audio.ConvertMillisecondsToBytes(end);

                m_TimeSelectionLeftX = beginBytes / BytesPerPixel;
                WaveFormTimeSelectionRect.Visibility = Visibility.Visible;
                WaveFormTimeSelectionRect.Width = endBytes / BytesPerPixel - m_TimeSelectionLeftX;
                WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, m_TimeSelectionLeftX);
            }
        }

        public void SetSelectionBytes(long begin, long end)
        {
            ViewModel.Logger.Log("AudioPaneView.SetSelectionBytes", Category.Debug, Priority.Medium);

            m_TimeSelectionLeftX = begin / BytesPerPixel;
            WaveFormTimeSelectionRect.Visibility = Visibility.Visible;
            WaveFormTimeSelectionRect.Width = end / BytesPerPixel - m_TimeSelectionLeftX;
            WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, m_TimeSelectionLeftX);
        }

        public void ClearSelection()
        {
            ViewModel.Logger.Log("AudioPaneView.ClearSelection", Category.Debug, Priority.Medium);

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
            ViewModel.Logger.Log("AudioPaneView.OnZoomSelection", Category.Debug, Priority.Medium);

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

            double newSliderValue = ZoomSlider.Value * (widthToUse / WaveFormTimeSelectionRect.Width);

            if (newSliderValue > 20000)
            {
                newSliderValue = 20000; //safeguard...image too large
            }

            if (newSliderValue < ZoomSlider.Minimum)
            {
                ZoomSlider.Minimum = newSliderValue;
            }
            if (newSliderValue > ZoomSlider.Maximum)
            {
                ZoomSlider.Maximum = newSliderValue;
            }

            if (ViewModel.State.Audio.HasContent)
            {
                double selectionTimeLeft = ViewModel.State.Audio.ConvertBytesToMilliseconds(Convert.ToInt64(m_TimeSelectionLeftX * BytesPerPixel));
                double selectionTimeRight = ViewModel.State.Audio.ConvertBytesToMilliseconds(Convert.ToInt64((m_TimeSelectionLeftX + WaveFormTimeSelectionRect.Width) * BytesPerPixel));

                if (ViewModel.LastPlayHeadTime < selectionTimeLeft || ViewModel.LastPlayHeadTime > selectionTimeRight)
                {
                    ViewModel.LastPlayHeadTime = selectionTimeLeft;
                }
            }

            ZoomSlider.Value = newSliderValue;
        }


        public double GetSelectionLeft()
        {
            return m_TimeSelectionLeftX;
        }

        public double GetSelectionWidth()
        {
            return WaveFormTimeSelectionRect.Width;
        }

        private void selectionFinished(double x)
        {
            if (Math.Abs(m_TimeSelectionLeftX - x) <= 6)
            {
                ClearSelection();
                m_TimeSelectionLeftX = x;
            }

            if (x == m_TimeSelectionLeftX)
            {
                restoreSelection();

                if (!ViewModel.State.Audio.HasContent)
                {
                    return;
                }

                ViewModel.LastPlayHeadTime = ViewModel.State.Audio.ConvertBytesToMilliseconds(Convert.ToInt64(x * BytesPerPixel));

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

            if (!ViewModel.State.Audio.HasContent)
            {
                return;
            }

            ViewModel.State.Selection.SetSelectionBytes(Convert.ToInt64(m_TimeSelectionLeftX * BytesPerPixel), Convert.ToInt64((m_TimeSelectionLeftX + WaveFormTimeSelectionRect.Width) * BytesPerPixel));

            if (ViewModel.IsAutoPlay)
            {
                long bytesFrom = Convert.ToInt64(m_TimeSelectionLeftX * BytesPerPixel);

                ViewModel.IsAutoPlay = false;
                ViewModel.LastPlayHeadTime = ViewModel.State.Audio.ConvertBytesToMilliseconds(bytesFrom);
                ViewModel.IsAutoPlay = true;

                long bytesTo = Convert.ToInt64(right * BytesPerPixel);

                ViewModel.AudioPlayer_PlayFromTo(bytesFrom, bytesTo);
            }
        }

        private void restoreSelection()
        {
            m_TimeSelectionLeftX = m_SelectionBackup_X;
            WaveFormTimeSelectionRect.Visibility = Visibility.Visible;
            WaveFormTimeSelectionRect.Width = m_SelectionBackup_Width;
            WaveFormTimeSelectionRect.SetValue(Canvas.LeftProperty, m_TimeSelectionLeftX);

            if (m_TimeSelectionLeftX < 0)
            {
                ViewModel.State.Selection.ClearSelection();
            }
            else if (ViewModel.State.Audio.HasContent)
            {
                ViewModel.State.Selection.SetSelectionBytes(Convert.ToInt64(m_TimeSelectionLeftX * BytesPerPixel), Convert.ToInt64((m_TimeSelectionLeftX + WaveFormTimeSelectionRect.Width) * BytesPerPixel));
            }
        }

        private void backupSelection()
        {
            m_SelectionBackup_X = m_TimeSelectionLeftX;
            m_SelectionBackup_Width = WaveFormTimeSelectionRect.Width;
        }
    }
}