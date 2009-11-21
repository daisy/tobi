using System;
using System.Windows.Controls;

namespace Tobi.Plugin.AudioPane
{
    /// <summary>
    /// Interaction logic for AudioSettings.xaml
    /// </summary>
    public partial class AudioSettings
    {
        public AudioSettings(AudioPaneViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();
        }

        public AudioPaneViewModel ViewModel
        {
            private set; get;
        }
    }
}
