using Microsoft.Practices.Composite.Modularity;
using Microsoft.Practices.Unity;
using Tobi.Common;

namespace Tobi.Modules.FileDialog
{
    ///<summary>
    ///
    ///</summary>
    public class FileDialogModule : IModule
    {
        private readonly IUnityContainer m_Container;

        ///<summary>
        /// Dependency Injection constructor
        ///</summary>
        ///<param name="container">The DI container</param>
        public FileDialogModule(IUnityContainer container)
        {
            m_Container = container;
        }

        ///<summary>
        /// 
        ///</summary>
        public void Initialize()
        {
            m_Container.RegisterType<IFileDialogService, FileDialogService>(new ContainerControlledLifetimeManager());
        }
    }
}
