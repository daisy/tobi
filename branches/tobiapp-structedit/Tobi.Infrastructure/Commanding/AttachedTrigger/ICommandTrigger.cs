using System.Windows;

// See http://blogs.microsoft.co.il/blogs/tomershamam/archive/2009/04/14/wpf-commands-everywhere.aspx

namespace Tobi.Infrastructure.Commanding.AttachedTrigger
{
    public interface ICommandTrigger
    {
        void Initialize(FrameworkElement source);
    }
}