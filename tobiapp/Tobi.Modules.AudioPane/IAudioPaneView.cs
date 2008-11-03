namespace Tobi.Modules.AudioPane
{
    ///<summary>
    /// Contract for the View
    ///</summary>
    public interface IAudioPaneView
    {
        ///<summary>
        /// The PresentationModel associated to this view
        ///</summary>
        AudioPanePresentationModel Model { get; set; }
    }
}
