using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Tobi.Modules.NavigationPane
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
