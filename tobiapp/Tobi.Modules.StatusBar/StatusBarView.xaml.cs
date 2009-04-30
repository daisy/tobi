using System.Windows.Controls;

namespace Tobi.Modules.StatusBar
{
    /// <summary>
    /// Interaction logic for StatusBarView.xaml
    /// </summary>
    public partial class StatusBarView
    {
        ///<summary>
        /// Dependency-injected constructor
        ///</summary>
        public StatusBarView()
        {
            InitializeComponent();
        }

        public string DisplayString
        {
            get { return "Testing..."; }
        }
    }

}
