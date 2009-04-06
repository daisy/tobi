using System.Windows;
using System.Windows.Controls;
using Sid.Windows.Controls;

namespace Test
{
    /// <summary>
    /// Interaction logic for Uac.xaml
    /// </summary>
    public partial class Uac : UserControl
    {
        private readonly TaskDialogWindow _parentWindow;

        public Uac(TaskDialogWindow parentWindow)
        {
            InitializeComponent();
            _parentWindow = parentWindow;

            // how to get the TaskDialog instance
            //example : TaskDialog td = _parentWindow.Content as TaskDialog;
            //example : td.IsButton1Enabled = false;

            TaskDialog td = _parentWindow.Content as TaskDialog;

            buttonCancel.Click +=
                delegate
                {
                    _parentWindow.Close();
                    MessageBox.Show("Cancel...", "Action", MessageBoxButton.OK, MessageBoxImage.Information);
                };

            buttonAllow.Click +=
                delegate
                {
                    _parentWindow.Close();
                    MessageBox.Show("Allow...", "Action", MessageBoxButton.OK, MessageBoxImage.Information);
                };
        }
    }
}
