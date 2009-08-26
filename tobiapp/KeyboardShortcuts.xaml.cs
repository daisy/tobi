using System;
using Microsoft.Windows.Controls;

namespace Tobi
{
    /// <summary>
    /// Interaction logic for KeyboardShortcuts.xaml
    /// </summary>
    public partial class KeyboardShortcuts
    {
        private ShellPresenter ShellPresenter
        {
            get;
            set;
        }

        public KeyboardShortcuts(ShellPresenter shellPresenter)
        {
            ShellPresenter = shellPresenter;
            DataContext = ShellPresenter;

            InitializeComponent();
        }

        private void OnLoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = e.Row.GetIndex();
        }
    }
}
