
using Tobi.Modules.StatusBar.PresentationModels;

namespace Tobi.Modules.StatusBar.Views
{
    public interface IStatusBarView
    {
        string Text { get; }
        StatusBarPresentationModel Model { get; set; }
    }
}
