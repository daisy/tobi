using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Unity;
using Tobi.Common;

namespace Tobi.Modules.Urakawa
{
    ///<summary>
    /// The Urakawa SDK Project is hosted by a "session" instance from this module.
    ///</summary>
    public class UrakawaModule : IModule
    {
        private readonly IUnityContainer m_Container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public UrakawaModule(IUnityContainer container)
        {
            m_Container = container;
        }

        ///<summary>
        ///</summary>
        public void Initialize()
        {
            m_Container.RegisterType<IUrakawaSession, UrakawaSession>(new ContainerControlledLifetimeManager());
        }
    }
}
