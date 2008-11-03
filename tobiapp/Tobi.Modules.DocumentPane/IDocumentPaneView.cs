namespace Tobi.Modules.DocumentPane
{
    ///<summary>
    /// Contract for the View
    ///</summary>
    public interface IDocumentPaneView
    {
        ///<summary>
        /// The PresentationModel associated to this view
        ///</summary>
        DocumentPanePresentationModel Model { get; set; }
    }
}
