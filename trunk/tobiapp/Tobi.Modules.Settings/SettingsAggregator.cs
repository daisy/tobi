using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
        }

        public void SaveAll()
        {
            foreach (var settingsProvider in m_SettingsProviders)
            {
                settingsProvider.Settings.Save();
            }
        }
    }
}
