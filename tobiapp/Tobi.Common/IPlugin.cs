using Microsoft.Practices.Composite.Modularity;

namespace Tobi.Common
{
    public interface IPlugin : IModule
    {
        string Name { get; set; }
        string Description { get; set; }
    }
}
