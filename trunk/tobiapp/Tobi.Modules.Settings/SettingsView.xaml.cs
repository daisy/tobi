using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common._UnusedCode;
using Tobi.Common.MVVM;
using Tobi.Common.UI;

namespace Tobi.Plugin.Settings
{
    [Export(typeof(SettingsView)), PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class SettingsView : IPartImportsSatisfiedNotification, INotifyPropertyChangedEx
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

        public List<SettingWrapper> AggregatedSettings
        {
            get;
            private set;
        }

        [ImportingConstructor]
        public SettingsView(
            ILoggerFacade logger,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            [Import(typeof(ISettingsAggregator), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false)]
            ISettingsAggregator settingsAggregator)
        {
            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);

            m_Logger = logger;
            m_ShellView = shellView;

            m_SettingsAggregator = settingsAggregator;

            resetList();

            InitializeComponent();

            // NEEDS to be after InitializeComponent() in order for the DataContext bridge to work.
            DataContext = this;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var settingBase = (ApplicationSettingsBase)sender;
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

        public bool HasValidationErrors
        {
            get { return m_nErrors > 0; }
        }

        private int m_nErrors = 0;

        private void OnError(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
                m_nErrors++;
            else
                m_nErrors--;

            m_PropertyChangeHandler.RaisePropertyChanged(() => HasValidationErrors);
        }


        private void OnKeyUp_ListItem(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            // We capture only the ESCAPE KeyUp bubbling-up from UI descendants
            if (key != Key.Escape || !(sender is ListViewItem))
            {
                return;
            }

            // We re-focus on the ListItem,
            // only if the Key press happened within one of the known editors
            if (true || // Let's do it all the time !
                e.OriginalSource is KeyGestureSinkBox
                || e.OriginalSource is CheckBox
                || e.OriginalSource is TextBox
                || e.OriginalSource is ComboBoxColor)
            {
                FocusHelper.Focus(((UIElement)sender), ((UIElement)sender));

                // We void the effect of the ESCAPE key
                // (which would normally close the parent dialog window: CANCEL action)
                e.Handled = true;
            }
        }

        private void OnKeyDown_ListItem(object sender, KeyEventArgs e)
        {
            var key = (e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key));

            // We capture only the RETURN KeyUp bubbling-up from UI descendants
            if (key != Key.Return || !(sender is ListViewItem))
            {
                return;
            }

            // If RETURN was pressed on the ListItem itself,
            // then we focus down into the editor UI (using a TAB gesture)
            if (e.OriginalSource is ListViewItem)
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new ThreadStart(FUNC));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    (Action)(() =>
                    {
                        var ev = new KeyEventArgs(
                            Keyboard.PrimaryDevice,
                            Keyboard.PrimaryDevice.ActiveSource, //PresentationSource.FromVisual(this),
                            0, //Environment.TickCount, //(int)new DateTime(Environment.TickCount, DateTimeKind.Utc).Ticks
                            Key.Tab) { RoutedEvent = Keyboard.KeyDownEvent };

                        //RaiseEvent(ev);
                        //OnKeyDown(ev);

                        InputManager.Current.ProcessInput(ev);
                    }));

                // We void the effect of the RETURN key
                // (which would normally close the parent dialog window by activating the default button: CANCEL)
                e.Handled = true;
            }
            // If RETURN was pressed on one of the editors,
            // then we re-focus on the ListItem
            else if (true || // Let's do it all the time !
                e.OriginalSource is KeyGestureSinkBox
                || e.OriginalSource is CheckBox
                || e.OriginalSource is TextBox
                || e.OriginalSource is ComboBoxColor)
            {
                FocusHelper.Focus(((UIElement)sender), ((UIElement)sender));

                // We void the effect of the RETURN key
                // (which would normally close the parent dialog window by activating the default button: CANCEL)
                e.Handled = true;
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

        private void resetList()
        {
            AggregatedSettings = new List<SettingWrapper>();

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
        }
    }
}
