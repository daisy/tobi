namespace Tobi.Modules.DocumentPane
{
    ///<summary>
    /// Placeholder for future Status bar controller code,
    /// such as logic related to extension points for addins.
    ///</summary>
    public class DocumentPanePresenter : IDocumentPanePresenter
    {
        private readonly IDocumentPaneService _service;

        public DocumentPanePresenter(IDocumentPaneView view, IDocumentPaneService service)
        {
            View = view;
            _service = service;

            View.Model = new DocumentPanePresentationModel();
        }

        ///<summary>
        /// The View property
        ///</summary>
        public IDocumentPaneView View
        {
            get;
            private set;
        }
    }
}
