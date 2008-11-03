using Tobi.Infrastructure;

namespace Tobi.Modules.UserInterfaceZoom
{
    ///<summary>
    /// Contract for the View
    ///</summary>
    public interface IUserInterfaceZoomView : IToggableView
    {
        ///<summary>
        /// The PresentationModel associated to this view
        ///</summary>
        UserInterfaceZoomPresenter Model { get; set; }
    }
}
