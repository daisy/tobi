namespace Tobi.Modules.NavigationPane
{
    ///<summary>
    /// Contract for the View
    ///</summary>
    public interface INavigationPaneView
    {
        ///<summary>
        /// The PresentationModel associated to this view
        ///</summary>
        NavigationPanePresentationModel Model { get; set; }
    }
}
