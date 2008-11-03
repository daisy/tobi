namespace Tobi.Modules.ToolBars
{
    ///<summary>
    /// Contract for the Presenter
    ///</summary>
    public interface IToolBarsPresenter
    {
        ///<summary>
        /// The View associated to this Presenter
        ///</summary>
        IToolBarsView View { get; }
    }
}
