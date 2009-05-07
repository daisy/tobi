using Tobi.Infrastructure.Commanding;

namespace Tobi.Modules.UserInterfaceZoom
{
    ///<summary>
    /// Contract for the Presenter.
    ///</summary>
    public interface IUserInterfaceZoomPresenter
    {
        ///<summary>
        /// The View associated with this Presenter.
        ///</summary>
        IUserInterfaceZoomView View { get; }

        ///<summary>
        /// Command: toggles the zoom slider display on/off. If the parameter is non-null, .. TODO
        ///</summary>
        RichDelegateCommand<bool?> ZoomToggleCommand { get; }

        ///<summary>
        /// Command: increases the zoom level, constrained within <see cref="MinimumZoom"/> and <see cref="MaximumZoom"/>. If the parameter is non-null, it is used as the step value (negative values are allowed, although that's kind of non-intuitive !), otherwise <see cref="ZoomStep"/> is used.
        ///</summary>
        RichDelegateCommand<double?> IncreaseZoomCommand { get; }

        ///<summary>
        /// Command: decreases the zoom level, constrained within <see cref="MinimumZoom"/> and <see cref="MaximumZoom"/>. If the parameter is non-null, it is used as the step value (negative values are allowed, although that's kind of non-intuitive !), otherwise <see cref="ZoomStep"/> is used.
        ///</summary>
        RichDelegateCommand<double?> DecreaseZoomCommand { get; }

        ///<summary>
        /// The minimum allowed zoom value
        ///</summary>
        double MinimumZoom { get; }

        ///<summary>
        /// The maximum allowed zoom value
        ///</summary>
        double MaximumZoom { get; }

        ///<summary>
        /// The default step value used for zooming in/out gradually
        ///</summary>
        double ZoomStep { get; }
    }
}
