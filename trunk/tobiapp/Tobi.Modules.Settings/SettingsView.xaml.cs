using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Configuration;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;

namespace Tobi.Plugin.Settings
{
    [Export(typeof(SettingsView)), PartCreationPolicy(CreationPolicy.Shared)]
    public partial class SettingsView : IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        private readonly ILoggerFacade m_Logger;
        private readonly IShellView m_ShellView;

        public readonly IEnumerable<ISettingsProvider> SettingsProviders;

        public List<string> SettingsExpanded = new List<string>();

        [ImportingConstructor]
        public SettingsView(
            ILoggerFacade logger,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [ImportMany(typeof(ISettingsProvider), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false)]
            IEnumerable<ISettingsProvider> settingsProviders)
        {
            m_Logger = logger;
            m_ShellView = shellView;

            SettingsProviders = settingsProviders;

            foreach (var settingsProvider in SettingsProviders)
            {
                SettingsPropertyCollection col1 = settingsProvider.Settings.Properties;
                IEnumerator enume1 = col1.GetEnumerator();
                while (enume1.MoveNext())
                {
                    var current = (SettingsProperty)enume1.Current;
                    SettingsExpanded.Add((current.IsReadOnly ? "[readonly] " : "") + current.Name + " = " + settingsProvider.Settings[current.Name] + "(" + current.DefaultValue + ") [" + current.PropertyType + "] ");
                }

                settingsProvider.Settings.PropertyChanged += Settings_PropertyChanged;

                //SettingsPropertyValueCollection col2 = settingsProvider.Settings.PropertyValues;
                //IEnumerator enume2 = col2.GetEnumerator();
                //while (enume2.MoveNext())
                //{
                //    var current = (SettingsPropertyValue)enume2.Current;
                //    SettingsExpanded.Add(current.Name + " = " + current.PropertyValue);
                //}
            }

            DataContext = SettingsExpanded;
            InitializeComponent();
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            int debug = 1;
            //e.PropertyName
        }
    }
}
