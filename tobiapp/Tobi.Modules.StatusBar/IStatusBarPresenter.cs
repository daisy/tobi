namespace Tobi.Modules.StatusBar
{
    ///<summary>
    /// Contract for the Presenter
    ///</summary>
    public interface IStatusBarPresenter
    {
        ///<summary>
        /// The View associated to this Presenter
        ///</summary>
        IStatusBarView View { get; }
    }
}
