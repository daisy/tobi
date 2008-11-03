namespace Tobi.Modules.NavigationPane
{
    ///<summary>
    /// Contract for the Presenter
    ///</summary>
    public interface INavigationPanePresenter
    {
        ///<summary>
        /// The View associated to this Presenter
        ///</summary>
        INavigationPaneView View { get; }
    }
}
