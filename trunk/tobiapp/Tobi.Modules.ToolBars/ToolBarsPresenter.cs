namespace Tobi.Modules.ToolBars
{
    ///<summary>
    /// Placeholder for future Status bar controller code,
    /// such as logic related to extension points for addins.
    ///</summary>
    public class ToolBarsPresenter : IToolBarsPresenter
    {
        private readonly IToolBarsService _service;

        public ToolBarsPresenter(IToolBarsView view, IToolBarsService service)
        {
            View = view;
            _service = service;

            View.Model = new ToolBarsPresentationModel();
        }

        ///<summary>
        /// The View property
        ///</summary>
        public IToolBarsView View
        {
            get;
            private set;
        }
    }
}
