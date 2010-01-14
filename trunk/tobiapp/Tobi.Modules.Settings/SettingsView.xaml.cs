using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;

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

        public readonly ISettingsAggregator m_SettingsAggregator;

        private List<SettingWrapper> m_AggregatedSettings = new List<SettingWrapper>();
        public List<SettingWrapper> AggregatedSettings
        {
            get { return m_AggregatedSettings; }
        }

        [ImportingConstructor]
        public SettingsView(
            ILoggerFacade logger,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(ISettingsAggregator), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false)]
            ISettingsAggregator settingsAggregator)
        {
            m_Logger = logger;
            m_ShellView = shellView;

            m_SettingsAggregator = settingsAggregator;

            foreach (var settingsProvider in m_SettingsAggregator.Settings)
            {
                settingsProvider.PropertyChanged += Settings_PropertyChanged;

                SettingsPropertyCollection col1 = settingsProvider.Properties;
                IEnumerator enume1 = col1.GetEnumerator();
                while (enume1.MoveNext())
                {
                    var current = (SettingsProperty)enume1.Current;
                    AggregatedSettings.Add(new SettingWrapper(settingsProvider, current));

                    //(current.IsReadOnly ? "[readonly] " : "")
                    //+ current.Name + " = " + settingsProvider.Settings[current.Name]
                    // + "(" + current.DefaultValue + ")
                    // [" + current.PropertyType + "] ");
                }
            }

            //SettingsExpanded.Add("---");

            //foreach (var settingsProvider in SettingsProviders)
            //{
            //    SettingsPropertyValueCollection col2 = settingsProvider.Settings.PropertyValues;
            //    IEnumerator enume2 = col2.GetEnumerator();
            //    while (enume2.MoveNext())
            //    {
            //        var current = (SettingsPropertyValue)enume2.Current;
            //        SettingsExpanded.Add(current.Name + " = " + current.PropertyValue + "---" + current.SerializedValue + " (" + (current.UsingDefaultValue ? "default" : "user") + ")");
            //    }
            //}

            //DataContext = AggregatedSettings;

            InitializeComponent();
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var settingBase = (ApplicationSettingsBase) sender;
            string name = e.PropertyName;

            foreach (var aggregatedSetting in AggregatedSettings)
            {
                if (aggregatedSetting.Name == name
                    && aggregatedSetting.m_settingBase == settingBase)
                {
                    aggregatedSetting.NotifyValueChanged();
                }
            }
        }
    }

    public class SettingWrapper : INotifyPropertyChangedEx
    {
        public readonly ApplicationSettingsBase m_settingBase;
        public readonly SettingsProperty m_settingProperty;

        public SettingWrapper(ApplicationSettingsBase settingBase, SettingsProperty settingProperty)
        {
            m_settingBase = settingBase;
            m_settingProperty = settingProperty;

            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);
        }

        public string Name
        {
            get
            {
                return m_settingProperty.Name;
            }
        }

        public object DefaultValue
        {
            get
            {
                return m_settingProperty.DefaultValue;
            }
        }

        public Type ValueType
        {
            get
            {
                return m_settingProperty.PropertyType;
            }
        }

        public object Value
        {
            get
            {
                return m_settingBase[Name];
            }
            set
            {
                m_settingBase[Name] = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void DispatchPropertyChangedEvent(PropertyChangedEventArgs e)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private PropertyChangedNotifyBase m_PropertyChangeHandler;

        public void NotifyValueChanged()
        {
            m_PropertyChangeHandler.RaisePropertyChanged(() => Value);
        }
    }
    public class TextToDoubleConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Double.Parse((string)value);
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class SettingsValueTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item != null && item is SettingWrapper)
            {
                if (((SettingWrapper)item).ValueType == typeof(Boolean))
                {
                    var t1 =  ((ContentPresenter)container).FindResource("SettingEditTemplate_Boolean") as DataTemplate;
                    return t1;
                }
                if (((SettingWrapper)item).ValueType == typeof(Double))
                {
                    var t2 = ((ContentPresenter)container).FindResource("SettingEditTemplate_Double") as DataTemplate;
                    return t2;
                }
            }
            var defaultTemplate = ((ContentPresenter)container).FindResource("SettingEditTemplate_Text") as DataTemplate;
            return defaultTemplate;
        }
    }
}
