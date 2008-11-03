namespace Tobi.Modules.StatusBar
{
    ///<summary>
    /// Placeholder for future Status bar controller code,
    /// such as logic related to extension points for addins.
    ///</summary>
    public class StatusBarPresenter : IStatusBarPresenter
    {
        private readonly IStatusBarService _service;

        public StatusBarPresenter(IStatusBarView view, IStatusBarService service)
        {
            View = view;
            _service = service;

            View.Model = new StatusBarPresentationModel();
        }

        ///<summary>
        /// The View property
        ///</summary>
        public IStatusBarView View
        {
            get;
            private set;
        }
    }
}
