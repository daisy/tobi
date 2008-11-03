namespace Tobi.Modules.AudioPane
{
    ///<summary>
    /// Contract for the Presenter
    ///</summary>
    public interface IAudioPanePresenter
    {
        ///<summary>
        /// The View associated to this Presenter
        ///</summary>
        IAudioPaneView View { get; }
    }
}
