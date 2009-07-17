using Microsoft.Practices.Unity;

namespace Tobi.Infrastructure
{
    public class ViewModelShellBase : ViewModelBase
    {
        public IUnityContainer Container { get; private set; }

        public ViewModelShellBase(IUnityContainer container)
        {
            Container = container;
        }
    }
}
