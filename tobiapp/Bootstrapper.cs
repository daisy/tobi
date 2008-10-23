using System.Windows.Input;
using Microsoft.Practices.Composite.UnityExtensions;
using Microsoft.Practices.Composite.Modularity;
using System.Windows;
using Tobi.Modules.StatusBar;
using Tobi.Modules.StatusBar.Views;

namespace Tobi
{
    /// <summary>
    /// The Tobi-specific Bootstrapper
    /// </summary>
    public class Bootstrapper : UnityBootstrapper
    {
        /// <summary>
        /// Initialization of the Tobi Shell window
        /// </summary>
        protected override DependencyObject CreateShell()
        {
            Shell shell = Container.Resolve<Shell>();
            shell.Show();
            return shell;
        }

        /// <summary>
        /// Tobi loads its main Modules statically
        /// </summary>
        protected override IModuleEnumerator GetModuleEnumerator()
        {
            return new StaticModuleEnumerator()
                .AddModule(typeof(StatusBarModule));
        }
    }
}