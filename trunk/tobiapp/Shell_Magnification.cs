using System;
using System.Windows;
using Application = System.Windows.Application;

namespace Tobi
{
    public partial class Shell
    {
        public static readonly DependencyProperty MagnificationLevelProperty =
            DependencyProperty.Register(@"MagnificationLevel",
            typeof(double),
            typeof(Shell),
            new PropertyMetadata(1.0, OnMagnificationLevelChanged, OnMagnificationLevelCoerce));

        public double MagnificationLevel
        {
            get { return (double)GetValue(MagnificationLevelProperty); }
            set
            {
                // The value will be coerced after this call !
                SetValue(MagnificationLevelProperty, value);
            }
        }


        private static object OnMagnificationLevelCoerce(DependencyObject d, object basevalue)
        {
            var shell = d as Shell;
            if (shell == null) return 1.0;

            var value = (Double)basevalue;
            if (value > shell.ZoomSlider.Maximum)
            {
                value = shell.ZoomSlider.Maximum;
            }
            if (value < shell.ZoomSlider.Minimum)
            {
                value = shell.ZoomSlider.Minimum;
            }

            Application.Current.Resources[@"MagnificationLevel"] = value;

            return value;
        }

        private static void OnMagnificationLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var shell = d as Shell;
            if (shell == null) return;
            shell.NotifyMagnificationLevel();
        }

        private bool m_InConstructor = false;
        private void NotifyMagnificationLevel()
        {
            if (m_InConstructor)
            {
                return;
            }

            updateIconDrawScales(MagnificationLevel);
        }
    }
}
