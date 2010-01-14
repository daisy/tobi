using System.Collections.Generic;
using System.Configuration;

namespace Tobi.Common
{
    public interface ISettingsProvider
    {
        ApplicationSettingsBase Settings { get; }
    }

    public interface ISettingsAggregator
    {
        IEnumerable<ApplicationSettingsBase> Settings { get; }

        //void UpgradeAll();

        void SaveAll();
    }
}
