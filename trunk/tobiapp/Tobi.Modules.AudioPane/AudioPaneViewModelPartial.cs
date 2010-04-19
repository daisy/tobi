using System;
using System.Collections.Generic;
using System.Windows.Media;
using Colors = System.Windows.Media.Colors;

namespace Tobi.Plugin.AudioPane
{
    /// <summary>
    /// Temporary class to use for storing code whilst refactoring into the ViewModel
    /// </summary>
    public partial class AudioPaneViewModel
    {
        #region WaveForm configuration

        // ReSharper disable RedundantDefaultFieldInitializer
        private List<Double> m_DecibelResolutions = null;
        // ReSharper restore RedundantDefaultFieldInitializer
        public List<Double> DecibelResolutions
        {
            get
            {
                if (m_DecibelResolutions == null)
                {
                    m_DecibelResolutions = new List<double>
                                               {
                                                   4.0,
                                                   3.5,
                                                   3.0,
                                                   2.5,
                                                   2.0,
                                                   1.5,
                                                   1.0,
                                                   0.9,
                                                   0.8,
                                                   0.7,
                                                   0.6,
                                                   0.5,
                                                   0.4,
                                                   0.3,
                                                   0.2,
                                                   0.1,
                                                   0.09,
                                                   0.08,
                                                   0.07,
                                                   0.06,
                                                   0.05,
                                                   0.04,
                                                   0.03,
                                                   0.02,
                                                   0.01,
                                                   0.0
                                               };
                }
                return m_DecibelResolutions;
            }
        }

        private double m_DecibelResolution = 1;
        public double DecibelResolution
        {
            get
            {
                return m_DecibelResolution;
            }
            set
            {
                if (m_DecibelResolution == value) return;
                m_DecibelResolution = value;
                if (View != null)
                {
                    View.ResetWaveFormEmpty();

                    CommandRefresh.Execute();
                }
                RaisePropertyChanged(() => DecibelResolution);
            }
        }

        // ReSharper disable RedundantDefaultFieldInitializer
        private List<Double> m_WaveStepXs = null;
        // ReSharper restore RedundantDefaultFieldInitializer
        public List<Double> WaveStepXs
        {
            get
            {
                if (m_WaveStepXs == null)
                {
                    m_WaveStepXs = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0 };
                }
                return m_WaveStepXs;
            }
        }

        private double m_WaveStepX = Settings.Default.AudioWaveForm_Resolution;
        public double WaveStepX
        {
            get
            {
                return m_WaveStepX;
            }
            set
            {
                if (m_WaveStepX == value) return;
                m_WaveStepX = value;
                if (View != null)
                {
                    View.ResetWaveFormEmpty();

                    CommandRefresh.Execute();
                }
                RaisePropertyChanged(() => WaveStepX);
            }
        }

        private bool m_IsUseDecibelsAdjust = true;
        public bool IsUseDecibelsAdjust
        {
            get
            {
                return m_IsUseDecibelsAdjust;
            }
            set
            {
                if (m_IsUseDecibelsAdjust == value) return;
                m_IsUseDecibelsAdjust = value;
                //resetWaveFormBackground();
                if (View != null)
                {
                    CommandRefresh.Execute();
                }
                RaisePropertyChanged(() => IsUseDecibelsAdjust);
            }
        }

        // ReSharper disable RedundantDefaultFieldInitializer
        private bool m_IsUseDecibels = false;
        // ReSharper restore RedundantDefaultFieldInitializer
        public bool IsUseDecibels
        {
            get
            {
                return m_IsUseDecibels;
            }
            set
            {
                if (m_IsUseDecibels == value) return;
                m_IsUseDecibels = value;
                //resetWaveFormBackground();
                if (View != null)
                {
                    CommandRefresh.Execute();
                }
                RaisePropertyChanged(() => IsUseDecibels);
            }
        }

        // ReSharper disable RedundantDefaultFieldInitializer
        private bool m_IsUseDecibelsNoAverage = false;
        // ReSharper restore RedundantDefaultFieldInitializer
        public bool IsUseDecibelsNoAverage
        {
            get
            {
                return m_IsUseDecibelsNoAverage;
            }
            set
            {
                if (m_IsUseDecibelsNoAverage == value) return;
                m_IsUseDecibelsNoAverage = value;
                //resetWaveFormBackground();
                if (View != null)
                {
                    CommandRefresh.Execute();
                }
                RaisePropertyChanged(() => IsUseDecibelsNoAverage);
            }
        }

        /*
        private bool m_IsUseDecibelsIntensity = false;
        public bool IsUseDecibelsIntensity
        {
            get
            {
                return m_IsUseDecibelsIntensity;
            }
            set
            {
                if (m_IsUseDecibelsIntensity == value) return;
                m_IsUseDecibelsIntensity = value;
                resetWaveFormBackground();
                startWaveFormLoadTimer(20, false);
                RaisePropertyChanged("IsUseDecibelsIntensity");
            }
        }*/

        private bool m_IsBackgroundVisible = true;
        public bool IsBackgroundVisible
        {
            get
            {
                return m_IsBackgroundVisible;
            }
            set
            {
                if (m_IsBackgroundVisible == value) return;
                m_IsBackgroundVisible = value;
                //resetWaveFormBackground();
                if (View != null)
                {
                    CommandRefresh.Execute();
                }
                RaisePropertyChanged(() => IsBackgroundVisible);
            }
        }

        /*
        private bool m_IsAdjustOffsetFix = false;
        public bool IsAdjustOffsetFix
        {
            get
            {
                return m_IsAdjustOffsetFix;
            }
            set
            {
                if (m_IsAdjustOffsetFix == value) return;
                m_IsAdjustOffsetFix = value;
                resetWaveFormBackground();
                loadWaveForm();
                RaisePropertyChanged("IsAdjustOffsetFix");
            }
        }*/

        // ReSharper disable RedundantDefaultFieldInitializer
        private bool m_IsWaveFillVisible = Settings.Default.AudioWaveForm_IsStroked;
        // ReSharper restore RedundantDefaultFieldInitializer
        public bool IsWaveFillVisible
        {
            get
            {
                return m_IsWaveFillVisible;
            }
            set
            {
                if (m_IsWaveFillVisible == value) return;
                m_IsWaveFillVisible = value;
                //resetWaveFormBackground();
                if (View != null)
                {
                    CommandRefresh.Execute();
                }
                RaisePropertyChanged(() => IsWaveFillVisible);
            }
        }

        private bool m_IsEnvelopeVisible = Settings.Default.AudioWaveForm_IsBordered;
        public bool IsEnvelopeVisible
        {
            get
            {
                return m_IsEnvelopeVisible;
            }
            set
            {
                if (m_IsEnvelopeVisible == value) return;
                m_IsEnvelopeVisible = value;
                //resetWaveFormBackground();
                if (View != null)
                {
                    CommandRefresh.Execute();
                }
                RaisePropertyChanged(() => IsEnvelopeVisible);
            }
        }

        private bool m_IsEnvelopeFilled = Settings.Default.AudioWaveForm_IsFilled;
        public bool IsEnvelopeFilled
        {
            get
            {
                return m_IsEnvelopeFilled;
            }
            set
            {
                if (m_IsEnvelopeFilled == value) return;
                m_IsEnvelopeFilled = value;
                //resetWaveFormBackground();
                if (View != null)
                {
                    CommandRefresh.Execute();
                }
                RaisePropertyChanged(() => IsEnvelopeFilled);
            }
        }

        private Color m_ColorTimeInfoText = Settings.Default.AudioWaveForm_Color_TimeText;
        public Color ColorTimeInfoText
        {
            get
            {
                return m_ColorTimeInfoText;
            }
            set
            {
                if (m_ColorTimeInfoText == value) return;
                m_ColorTimeInfoText = value;

                if (View != null)
                {
                    View.ResetWaveFormEmpty();

                    CommandRefresh.Execute();
                }

                RaisePropertyChanged(() => ColorTimeInfoText);
            }
        }

        private Color m_ColorTimeSelection = Settings.Default.AudioWaveForm_Color_Selection;
        public Color ColorTimeSelection
        {
            get
            {
                return m_ColorTimeSelection;
            }
            set
            {
                if (m_ColorTimeSelection == value) return;
                m_ColorTimeSelection = value;
                RaisePropertyChanged(() => ColorTimeSelection);

                RaisePropertyChanged(() => ColorSelectionContourBrush);
            }
        }

        private Color m_ColorPlayhead = Settings.Default.AudioWaveForm_Color_CursorBorder;
        public Color ColorPlayhead
        {
            get
            {
                return m_ColorPlayhead;
            }
            set
            {
                if (m_ColorPlayhead == value) return;
                m_ColorPlayhead = value;
                AudioPlayer_UpdateWaveFormPlayHead();
                RaisePropertyChanged(() => ColorPlayhead);
            }
        }

        private Color m_ColorPlayheadFill = Settings.Default.AudioWaveForm_Color_CursorFill;
        public Color ColorPlayheadFill
        {
            get
            {
                return m_ColorPlayheadFill;
            }
            set
            {
                if (m_ColorPlayheadFill == value) return;
                m_ColorPlayheadFill = value;
                AudioPlayer_UpdateWaveFormPlayHead();
                RaisePropertyChanged(() => ColorPlayheadFill);
            }
        }

        private Color m_ColorWaveBackground = Settings.Default.AudioWaveForm_Color_Back;
        public Color ColorWaveBackground
        {
            get
            {
                return m_ColorWaveBackground;
            }
            set
            {
                if (m_ColorWaveBackground == value) return;
                m_ColorWaveBackground = value;
                if (View != null)
                {
                    View.ResetWaveFormEmpty();

                    CommandRefresh.Execute();
                }
                RaisePropertyChanged(() => ColorWaveBackground);
            }
        }

        private Color m_ColorMarkers = Settings.Default.AudioWaveForm_Color_Phrases;
        public Color ColorMarkers
        {
            get
            {
                return m_ColorMarkers;
            }
            set
            {
                if (m_ColorMarkers == value) return;
                m_ColorMarkers = value;

                if (View != null)
                {
                    View.ResetWaveFormEmpty();

                    CommandRefresh.Execute();
                }
                RaisePropertyChanged(() => ColorMarkers);
            }
        }

        private Color m_ColorWaveBars = Settings.Default.AudioWaveForm_Color_Stroke;
        public Color ColorWaveBars
        {
            get
            {
                return m_ColorWaveBars;
            }
            set
            {
                if (m_ColorWaveBars == value) return;
                m_ColorWaveBars = value;
                if (View != null)
                {
                    View.ResetWaveFormEmpty();

                    CommandRefresh.Execute();
                }
                RaisePropertyChanged(() => ColorWaveBars);
            }
        }

        public SolidColorBrush ColorSelectionContourBrush
        {
            get
            {
                return new SolidColorBrush(ColorTimeSelection) {Opacity = 0.6};
            }
        }

        private Color m_ColorEnvelopeFill = Settings.Default.AudioWaveForm_Color_Fill;
        public Color ColorEnvelopeFill
        {
            get
            {
                return m_ColorEnvelopeFill;
            }
            set
            {
                if (m_ColorEnvelopeFill == value) return;
                m_ColorEnvelopeFill = value;

                if (View != null)
                {
                    View.ResetWaveFormEmpty();
                    CommandRefresh.Execute();
                }
                RaisePropertyChanged(() => ColorEnvelopeFill);
            }
        }

        private Color m_ColorEnvelopeOutline = Settings.Default.AudioWaveForm_Color_Border;
        public Color ColorEnvelopeOutline
        {
            get
            {
                return m_ColorEnvelopeOutline;
            }
            set
            {
                if (m_ColorEnvelopeOutline == value) return;
                m_ColorEnvelopeOutline = value;
                if (View != null)
                {
                    View.ResetWaveFormEmpty();

                    CommandRefresh.Execute();
                }
                RaisePropertyChanged(() => ColorEnvelopeOutline);
            }
        }

        #endregion WaveForm configuration
    }
}
