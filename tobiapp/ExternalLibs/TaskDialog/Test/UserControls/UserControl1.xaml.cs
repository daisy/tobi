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
using Sid.Windows.Controls;

namespace Test
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        private readonly DialogWindow _parentDialog;

        public UserControl1(DialogWindow parentDialog)
        {
            InitializeComponent();
            _parentDialog = parentDialog;


            buttonCancel.Click +=
                delegate
                {
                    _parentDialog.Close();
                    MessageBox.Show("Cancel...", "Action", MessageBoxButton.OK, MessageBoxImage.Information);
                };

            buttonAllow.Click +=
                delegate
                {
                    _parentDialog.Close();
                    MessageBox.Show("Allow...", "Action", MessageBoxButton.OK, MessageBoxImage.Information);
                };

        }

    }
}
