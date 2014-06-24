//using Microsoft.Windows.Controls;

using Tobi.Common;

namespace Tobi
{
    /// <summary>
    /// Interaction logic for KeyboardShortcuts.xaml
    /// </summary>
    public partial class KeyboardShortcuts
    {
        private IShellView ShellView
        {
            get;
            set;
        }

        public KeyboardShortcuts(IShellView shellView)
        {
            ShellView = shellView;
            DataContext = ShellView;

            InitializeComponent();
        }

        //private void OnLoadingRow(object sender, DataGridRowEventArgs e)
        //{
        //    e.Row.Header = e.Row.GetIndex();
        //}
    }
}
