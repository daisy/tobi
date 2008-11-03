namespace Tobi.Modules.ToolBars
{
    ///<summary>
    /// Contract for the View
    ///</summary>
    public interface IToolBarsView
    {
        ///<summary>
        /// The PresentationModel associated to this view
        ///</summary>
        ToolBarsPresentationModel Model { get; set; }
    }
}
