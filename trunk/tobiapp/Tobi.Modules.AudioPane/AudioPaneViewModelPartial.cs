using System;
using System.Collections.Generic;
using System.Windows.Media;
using Colors = System.Windows.Media.Colors;

namespace Tobi.Modules.AudioPane
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
                    View.RefreshUI_WaveFormBackground();

                    View.StartWaveFormLoadTimer(20, false);
                }
                OnPropertyChanged(() => DecibelResolution);
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

        private double m_WaveStepX = 2;
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
                    View.RefreshUI_WaveFormBackground();

                    View.StartWaveFormLoadTimer(20, false);
                }
                OnPropertyChanged(() => WaveStepX);
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
                    View.StartWaveFormLoadTimer(20, false);
                }
                OnPropertyChanged(() => IsUseDecibelsAdjust);
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
                    View.StartWaveFormLoadTimer(20, false);
                }
                OnPropertyChanged(() => IsUseDecibels);
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
                    View.StartWaveFormLoadTimer(20, false);
                }
                OnPropertyChanged(() => IsUseDecibelsNoAverage);
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
                OnPropertyChanged("IsUseDecibelsIntensity");
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
                    View.StartWaveFormLoadTimer(20, false);
                }
                OnPropertyChanged(() => IsBackgroundVisible);
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
                OnPropertyChanged("IsAdjustOffsetFix");
            }
        }*/

        // ReSharper disable RedundantDefaultFieldInitializer
        private bool m_IsWaveFillVisible = false;
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
                    View.StartWaveFormLoadTimer(20, false);
                }
                OnPropertyChanged(() => IsWaveFillVisible);
            }
        }

        private bool m_IsEnvelopeVisible = true;
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
                    View.StartWaveFormLoadTimer(20, false);
                }
                OnPropertyChanged(() => IsEnvelopeVisible);
            }
        }

        private bool m_IsEnvelopeFilled = true;
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
                    View.StartWaveFormLoadTimer(20, false);
                }
                OnPropertyChanged(() => IsEnvelopeFilled);
            }
        }

        private Color m_ColorTimeSelection = Colors.Aqua;
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
                OnPropertyChanged(() => ColorTimeSelection);
            }
        }

        private Color m_ColorPlayhead = Colors.Red;
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
                OnPropertyChanged(() => ColorPlayhead);
            }
        }

        private Color m_ColorPlayheadFill = Colors.Gold;
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
                OnPropertyChanged(() => ColorPlayheadFill);
            }
        }

        private Color m_ColorWaveBackground = Colors.Black;
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
                    View.RefreshUI_WaveFormBackground();

                    View.StartWaveFormLoadTimer(20, false);
                }
                OnPropertyChanged(() => ColorWaveBackground);
            }
        }

        private Color m_ColorMarkers = Colors.BlueViolet;
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
                    View.RefreshUI_WaveFormBackground();

                    View.StartWaveFormLoadTimer(20, false);
                }
                OnPropertyChanged(() => ColorMarkers);
            }
        }

        private Color m_ColorWaveBars = Colors.Lime;
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
                    View.RefreshUI_WaveFormBackground();

                    View.StartWaveFormLoadTimer(20, false);
                }
                OnPropertyChanged(() => ColorWaveBars);
            }
        }

        private Color m_ColorEnvelopeFill = Colors.ForestGreen;
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
                    View.RefreshUI_WaveFormBackground();

                    View.StartWaveFormLoadTimer(20, false);
                }
                OnPropertyChanged(() => ColorEnvelopeFill);
            }
        }

        private Color m_ColorEnvelopeOutline = Colors.LawnGreen;
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
                    View.RefreshUI_WaveFormBackground();
                
                    View.StartWaveFormLoadTimer(20, false);
                }
                OnPropertyChanged(() => ColorEnvelopeOutline);
            }
        }

        #endregion WaveForm configuration
    }
}
