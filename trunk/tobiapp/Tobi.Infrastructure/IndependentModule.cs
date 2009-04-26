using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.Composite.Modularity;

namespace Tobi.Infrastructure
{
    /*
     * [ModuleDependency("DesktopModule")]
     */

    [Module(ModuleName = "IndependentModule", OnDemand = true)]
    public class IndependentModule : IModule
    {
        private readonly IModuleCatalog m_Catalog;
        private readonly IModuleManager m_ModuleManager;

        public IndependentModule(IModuleManager moduleManager, IModuleCatalog catalog)
        {
            m_ModuleManager = moduleManager;
            m_Catalog = catalog;
        }

        public void Initialize()
        {
            var dependentModules = m_Catalog.Modules.Where(m => DependsOnModule(m.DependsOn, "IndependentModule"))
                                                    .Select(m => m.ModuleName);
            foreach (string dependentModule in dependentModules)
            {
                m_ModuleManager.LoadModule(dependentModule);
            }
        }

        private static bool DependsOnModule(IEnumerable<string> dependencies, string independent)
        {
            if (dependencies == null)
                return false;

            return dependencies.Contains(independent);
        }
    }
}
