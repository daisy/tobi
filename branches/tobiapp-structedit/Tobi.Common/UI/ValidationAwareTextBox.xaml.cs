using System.Windows;
using System.Windows.Input;

namespace Tobi.Common.UI
{
    public partial class ValidationAwareTextBox
    {
        //static ValidationAwareTextBox()
        //{
        //    DefaultStyleKeyProperty.OverrideMetadata(typeof(ValidationAwareTextBox),
        //                                             new FrameworkPropertyMetadata(typeof(ValidationAwareTextBox)));
        //}

        public ValidationAwareTextBox()
        {
            InitializeComponent();
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            if (key == Key.Escape)
            {
                if (!string.IsNullOrEmpty(m_previousValidText))
                {
                    Text = m_previousValidText;
                }
            }
        }

        private string m_previousValidText;

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (System.Windows.Controls.Validation.GetHasError(this)
                && !string.IsNullOrEmpty(m_previousValidText))
            {
                Text = m_previousValidText;
            }
            FontWeight = FontWeights.Normal;
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            m_previousValidText = (System.Windows.Controls.Validation.GetHasError(this) ? null : Text);
            FontWeight = FontWeights.UltraBold;
        }
    }
}
