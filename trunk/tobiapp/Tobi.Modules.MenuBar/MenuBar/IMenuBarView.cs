using System.Windows.Input;

namespace Tobi.Modules.MenuBar
{
    ///<summary>
    /// Contract for the View
    ///</summary>
    public interface IMenuBarView
    {
        ///<summary>
        /// The PresentationModel associated to this view
        ///</summary>
        MenuBarPresentationModel Model { get; set; }

        void EnsureViewMenuCheckState(string regionName, bool visible);
    }
}
