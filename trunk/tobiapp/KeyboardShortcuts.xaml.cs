using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tobi
{
    /// <summary>
    /// Interaction logic for KeyboardShortcuts.xaml
    /// </summary>
    public partial class KeyboardShortcuts
    {
        private ShellPresenter ShellPresenter
        {
            get; set;
        }

        public KeyboardShortcuts(ShellPresenter shellPresenter)
        {
            ShellPresenter = shellPresenter;
            DataContext = ShellPresenter;

            InitializeComponent();
        }
    }
}
