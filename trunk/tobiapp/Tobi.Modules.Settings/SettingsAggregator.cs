using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;

namespace Tobi.Plugin.Settings
{
    [Export(typeof(ISettingsAggregator)), PartCreationPolicy(CreationPolicy.Shared)]
    public class SettingsAggregator : ISettingsAggregator
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif

        }
        
        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;

        public readonly IEnumerable<ISettingsProvider> m_SettingsProviders;

        [ImportingConstructor]
        public SettingsAggregator(
            ILoggerFacade logger,
            IEventAggregator eventAggregator,
            [ImportMany(typeof(ISettingsProvider), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false)]
            IEnumerable<ISettingsProvider> settingsProviders)
        {
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            m_SettingsProviders = settingsProviders;

            // Get the previous settings, if any. Saves to persistent storage.
            UpgradeAll();

            // Make sure we mark all the settings so that they all show in persistant storage next time we save.
            MarkAllAsChanged();

            SaveAll();
        }

        public void SaveAll()
        {
            foreach (var settingsProvider in m_SettingsProviders)
            {
                settingsProvider.Settings.Save();
            }
        }

        public void UpgradeAll()
        {
            foreach (var settingsProvider in m_SettingsProviders)
            {
                settingsProvider.Settings.Upgrade();
            }
        }

        private void MarkAllAsChanged()
        {
            foreach (var settingsProvider in m_SettingsProviders)
            {
                SettingsPropertyCollection col1 = settingsProvider.Settings.Properties;
                IEnumerator enume1 = col1.GetEnumerator();
                while (enume1.MoveNext())
                {
                    var current = (SettingsProperty)enume1.Current;
                    if (current.IsReadOnly)
                    {
                        continue;
                    }

                    settingsProvider.Settings[current.Name] = settingsProvider.Settings[current.Name];
                }

                //SettingsPropertyValueCollection col2 = settingsProvider.Settings.PropertyValues;
                //IEnumerator enume2 = col2.GetEnumerator();
                //while (enume2.MoveNext())
                //{
                //    var current = (SettingsPropertyValue)enume2.Current;
                //    settingsProvider.Settings[current.Name] = current.PropertyValue;
                //}
            }
        }
    }
}
