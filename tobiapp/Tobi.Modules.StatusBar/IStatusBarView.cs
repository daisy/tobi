namespace Tobi.Modules.StatusBar
{
    ///<summary>
    /// Contract for the View
    ///</summary>
    public interface IStatusBarView
    {
        ///<summary>
        /// The PresentationModel associated to this view
        ///</summary>
        StatusBarPresentationModel Model { get; set; }
    }
}
