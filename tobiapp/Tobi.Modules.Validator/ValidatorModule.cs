using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Composite.Regions;
using Microsoft.Practices.Unity;
using Tobi.Common;

namespace Tobi.Modules.Validator
{
    ///<summary>
    /// The status bar is commonly displayed at the bottom of the application window
    /// to report live information about the state of the application.
    ///</summary>
    public class ValidatorModule : IModule
    {
        private readonly IUnityContainer m_Container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public ValidatorModule(IUnityContainer container)
        {
            m_Container = container;
        }

        ///<summary>
        /// Registers the <see cref="ValidatorPaneView"/> into the Dependency Injection container
        ///</summary>
        public void Initialize()
        {
            m_Container.RegisterType<ValidatorPaneView>(new ContainerControlledLifetimeManager());
        }
    }
}
