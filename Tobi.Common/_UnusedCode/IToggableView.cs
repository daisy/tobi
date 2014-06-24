
namespace Tobi.Common._UnusedCode
{
    public interface IToggableView
    {
        ///<summary>
        /// The Region name into which this view is meant to be displayed
        ///</summary>
        string RegionName {get;}

        ///<summary>
        /// Puts the keyboard focus on the zoom control
        ///</summary>
        void FocusControl();
    }
}