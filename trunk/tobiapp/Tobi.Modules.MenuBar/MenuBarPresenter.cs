using Microsoft.Practices.Composite.Logging;
using Microsoft.Practices.Unity;

namespace Tobi.Modules.MenuBar
{
    ///<summary>
    /// Placeholder for future menu bar controller code,
    /// such as logic related to extension points for addins.
    ///</summary>
    public class MenuBarPresenter : IMenuBarPresenter
    {
        private readonly IMenuBarService _service;
        private readonly IUnityContainer _container;

        public MenuBarPresenter(IMenuBarView view, IMenuBarService service, IUnityContainer container)
        {
            View = view;
            _service = service;
            _container = container;
            View.Model = new MenuBarPresentationModel(_container);
        }

        ///<summary>
        /// The View property
        ///</summary>
        public IMenuBarView View
        {
            get;
            private set;
        }
    }
}
