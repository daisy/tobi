namespace Tobi.Modules.AudioPane
{
    ///<summary>
    /// Placeholder for future Status bar controller code,
    /// such as logic related to extension points for addins.
    ///</summary>
    public class AudioPanePresenter : IAudioPanePresenter
    {
        private readonly IAudioPaneService _service;

        public AudioPanePresenter(IAudioPaneView view, IAudioPaneService service)
        {
            View = view;
            _service = service;

            View.Model = new AudioPanePresentationModel();
        }

        ///<summary>
        /// The View property
        ///</summary>
        public IAudioPaneView View
        {
            get;
            private set;
        }
    }
}
