namespace Tobi.Modules.DocumentPane
{
    ///<summary>
    /// Contract for the Presenter
    ///</summary>
    public interface IDocumentPanePresenter
    {
        ///<summary>
        /// The View associated to this Presenter
        ///</summary>
        IDocumentPaneView View { get; }
    }
}
