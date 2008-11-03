namespace Tobi.Modules.NavigationPane
{
    ///<summary>
    /// Placeholder for future Status bar controller code,
    /// such as logic related to extension points for addins.
    ///</summary>
    public class NavigationPanePresenter : INavigationPanePresenter
    {
        private readonly INavigationPaneService _service;

        public NavigationPanePresenter(INavigationPaneView view, INavigationPaneService service)
        {
            View = view;
            _service = service;

            View.Model = new NavigationPanePresentationModel();
        }

        ///<summary>
        /// The View property
        ///</summary>
        public INavigationPaneView View
        {
            get;
            private set;
        }
    }
}
