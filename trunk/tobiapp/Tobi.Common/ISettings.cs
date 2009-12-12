using System.Configuration;

namespace Tobi.Common
{
    public interface ISettingsProvider
    {
        ApplicationSettingsBase Settings { get; }
    }

    public interface ISettings
    {
        //IEnumerable<ApplicationSettingsBase> Settings { get; }
    }
}
