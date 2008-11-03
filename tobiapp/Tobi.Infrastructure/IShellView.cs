using System.Windows;
using System.Windows.Data;

namespace Tobi
{
    public interface IShellView
    {
        ///<summary>
        /// Shows the Shell window
        ///</summary>
        void ShowView();

        void DecreaseZoom(double? step);
        void IncreaseZoom(double? step);

        Binding GetZoomBinding();
        Window Window { get; }
    }
}
