using System.Windows;

namespace Tobi
{
    /// <summary>
    /// The application starts the Composite WPF Bootstrapper
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Bootstrapper bootstrapper = new Bootstrapper();
            bootstrapper.Run();
        }
    }
}