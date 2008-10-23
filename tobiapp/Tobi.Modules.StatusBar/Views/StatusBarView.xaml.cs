
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Practices.Composite.Wpf.Commands;
using Tobi.Modules.StatusBar.PresentationModels;

namespace Tobi.Modules.StatusBar.Views
{
    /// <summary>
    /// Interaction logic for StatusBarView.xaml
    /// </summary>
    public partial class StatusBarView : UserControl, IStatusBarView
    {
        public static CompositeCommand StatusTextCommand = new CompositeCommand();

        public StatusBarView()
        {
            InitializeComponent();

            Model = new StatusBarPresentationModel();
            StatusTextCommand.RegisterCommand(Model.StatusTextCommand);
        }

        public string Text
        {
            get
            {
                return "Status: " + Model.DisplayString;
            }
        }
        public StatusBarPresentationModel Model
        {
            get { return DataContext as StatusBarPresentationModel; }
            set { DataContext = value; }
        }

    }
}
