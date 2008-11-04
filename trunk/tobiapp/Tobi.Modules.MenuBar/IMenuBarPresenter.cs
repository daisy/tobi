namespace Tobi.Modules.MenuBar
{
    ///<summary>
    /// Contract for the Presenter
    ///</summary>
    public interface IMenuBarPresenter
    {
        ///<summary>
        /// The View associated to this Presenter
        ///</summary>
        IMenuBarView View { get; }
    }
}
