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
using System.Windows.Input;
using System.Windows.Markup;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
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
            get; private set;
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

    public class SettingWrapper : INotifyPropertyChangedEx //, IDataErrorInfo
    {
        public readonly ApplicationSettingsBase m_settingBase;
        public readonly SettingsProperty m_settingProperty;

        public SettingWrapper(ApplicationSettingsBase settingBase, SettingsProperty settingProperty)
        {
            m_settingBase = settingBase;
            m_settingProperty = settingProperty;

            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);

            m_OriginalValue = Value;
        }

        private string m_Message;
        public string Message
        {
            get { return m_Message; }
            set
            {
                if (value != m_Message)
                {
                    m_Message = value;
                    m_PropertyChangeHandler.RaisePropertyChanged(() => Message);
                }
            }
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

        [NotifyDependsOn("Name")]
        [NotifyDependsOn("Value")]
        [NotifyDependsOn("DefaultValue")]
        [NotifyDependsOn("Message")]
        public string FullDescription
        {
            get
            {
                return Name + "=" + Value + " '" + Message + "' (default: " + DefaultValue + ") ";
            }
        }

        private object m_OriginalValue = null;

        [NotifyDependsOn("Value")]
        public bool IsChanged
        {
            get
            {
                return !m_OriginalValue.Equals(Value);
            }
        }

        //private bool m_IsChanged;
        //public bool IsChanged
        //{
        //    get { return m_IsChanged; }
        //    set
        //    {
        //        if (value != m_IsChanged)
        //        {
        //            m_IsChanged = value;
        //            m_PropertyChangeHandler.RaisePropertyChanged(() => IsChanged);
        //        }
        //    }
        //}

        public void NotifyValueChanged()
        {
            m_PropertyChangeHandler.RaisePropertyChanged(() => Value);

            //IsChanged = Value != m_OriginalValue;

            Message = IsChanged ? "Modified (" + m_OriginalValue +")" : null;
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


        //public string this[string propertyName]
        //{
        //    get
        //    {
        //        if (propertyName == PropertyChangedNotifyBase.GetMemberName(() => Value))
        //        {
        //            if (Value.GetType() == typeof(Double))
        //            {
        //                if ((Double)Value < 0.0
        //                    || (Double)Value > 10000.0)
        //                return "Invalid screen pixel value";
        //            }
        //        }

        //        return null; // no error
        //    }
        //}

        //public string Error
        //{
        //    get { throw new NotImplementedException(); }
        //}
    }

    public abstract class DataContextValidationRuleBase : ValidationRule
    {
        public FrameworkElement DataContextBridge
        {
            get;
            set;
        }

        public DataContextSpy DataContextSpy
        {
            get;
            set;
        }

        protected ValidationResult NotValid(string msg)
        {
            var currentSetting = (SettingWrapper)DataContextSpy.DataContext;
            currentSetting.Message = msg;
            return new ValidationResult(false, msg);
        }

        protected ValidationResult Valid()
        {
            var currentSetting = (SettingWrapper)DataContextSpy.DataContext;
            currentSetting.Message = "";
            return new ValidationResult(true, null);
        }
    }

    public class KeyGestureValidationRule : DataContextValidationRuleBase
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var str = value as string;
            if (String.IsNullOrEmpty(str))
            {
                return NotValid("Value cannot be empty.");
            }

            KeyGesture val = KeyGestureStringConverter.Convert(str);
            if (val == null)
            {
                return NotValid("Invalid keyboard shortcut.");
            }

            var currentSetting = (SettingWrapper)DataContextSpy.DataContext;

            string strSettingsAlreadyUsingKeyG = "";
            foreach (var aggregatedSetting in ((SettingsView)DataContextBridge.DataContext).AggregatedSettings)
            {
                if (currentSetting == aggregatedSetting)
                {
                    continue;
                }

                if (aggregatedSetting.ValueType == typeof(KeyGestureString))
                {
                    if (val.Key == ((KeyGesture)aggregatedSetting.Value).Key)
                    {
                        if (val.Modifiers.Equals(((KeyGesture)aggregatedSetting.Value).Modifiers))
                        {
                            strSettingsAlreadyUsingKeyG += aggregatedSetting.Name;
                            strSettingsAlreadyUsingKeyG += " ";
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(strSettingsAlreadyUsingKeyG))
            {
                return NotValid("Shortcut already used by: " + strSettingsAlreadyUsingKeyG);
            }

            return Valid();
        }
    }

    public class DoubleValidationRule : DataContextValidationRuleBase
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var str = value as string;
            if (String.IsNullOrEmpty(str))
            {
                return NotValid("Value cannot be empty.");
            }

            double val;
            if (!Double.TryParse(str, out val))
            {
                return NotValid("Invalid numeric value.");
            }

            var currentSetting = (SettingWrapper)DataContextSpy.DataContext;
            string lowName = currentSetting.Name.ToLower();

            // Super-master hack ! :)
            bool mustBePositive = lowName.Contains("width") || lowName.Contains("height");

            if (mustBePositive)
            {
                if (val < 0 || val > 9999)
                {
                    return NotValid("Numeric value is out of range [0, 9999].");
                }
            }
            else
            {
                if (val < -1 || val > 9999)
                {
                    return NotValid("Numeric value is out of range [-9999, 9999].");
                }
            }

            return Valid();
        }
    }

    public class ValidationErrorConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var error = value as ValidationError;

            if (error != null)
                return error.ErrorContent;

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class TextToKeyGestureConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return KeyGestureStringConverter.Convert((KeyGesture)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return KeyGestureStringConverter.Convert((string)value);
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
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
            try
            {
                return Double.Parse((string)value);
            }
            catch
            {
                return String.Empty;
            }
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
                    var t1 = ((ContentPresenter)container).FindResource("SettingEditTemplate_Boolean") as DataTemplate;
                    return t1;
                }
                if (((SettingWrapper)item).ValueType == typeof(Double))
                {
                    var t2 = ((ContentPresenter)container).FindResource("SettingEditTemplate_Double") as DataTemplate;
                    return t2;
                }
                if (((SettingWrapper)item).ValueType == typeof(KeyGestureString))
                {
                    var t3 = ((ContentPresenter)container).FindResource("SettingEditTemplate_KeyGesture") as DataTemplate;
                    return t3;
                }
            }
            var defaultTemplate = ((ContentPresenter)container).FindResource("SettingEditTemplate_Text") as DataTemplate;
            return defaultTemplate;
        }
    }

    public class DataContextSpy
       : Freezable // Enable ElementName and DataContext bindings
    {
        public DataContextSpy()
        {
            // This binding allows the spy to inherit a DataContext.
            BindingOperations.SetBinding(this, DataContextProperty, new Binding());

            this.IsSynchronizedWithCurrentItem = true;
        }

        /// <summary>
        /// Gets/sets whether the spy will return the CurrentItem of the 
        /// ICollectionView that wraps the data context, assuming it is
        /// a collection of some sort. If the data context is not a 
        /// collection, this property has no effect. 
        /// The default value is true.
        /// </summary>
        public bool IsSynchronizedWithCurrentItem { get; set; }

        public object DataContext
        {
            get { return (object)GetValue(DataContextProperty); }
            set { SetValue(DataContextProperty, value); }
        }

        // Borrow the DataContext dependency property from FrameworkElement.
        public static readonly DependencyProperty DataContextProperty =
            FrameworkElement.DataContextProperty.AddOwner(
            typeof(DataContextSpy),
            new PropertyMetadata(null, null, OnCoerceDataContext));

        static object OnCoerceDataContext(DependencyObject depObj, object value)
        {
            DataContextSpy spy = depObj as DataContextSpy;
            if (spy == null)
                return value;

            if (spy.IsSynchronizedWithCurrentItem)
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(value);
                if (view != null)
                    return view.CurrentItem;
            }

            return value;
        }

        protected override Freezable CreateInstanceCore()
        {
            // We are required to override this abstract method.
            throw new NotImplementedException();
        }
    }
}
