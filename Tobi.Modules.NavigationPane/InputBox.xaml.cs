using System.Windows.Controls;

namespace Tobi.Plugin.NavigationPane
{
    /// <summary>
    /// Interaction logic for InputBox.xaml
    /// </summary>
    public partial class InputBox : UserControl
    {
        public InputBox(string Caption, string Value)
        {
            InitializeComponent();
            tbInput.Text = Value;
            lblCaption.Content = Caption;
        }
        public InputBox()
        {
            InitializeComponent();
        }
    }
}
