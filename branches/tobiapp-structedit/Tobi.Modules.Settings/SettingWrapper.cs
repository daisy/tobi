using System;
using System.ComponentModel;
using System.Configuration;
using Tobi.Common.MVVM;

namespace Tobi.Plugin.Settings
{
    public class SettingWrapper : INotifyPropertyChangedEx //, IDataErrorInfo
    {
        public readonly ApplicationSettingsBase m_settingBase;
        public readonly SettingsProperty m_settingProperty;
        private bool m_isMatch;
        public SettingWrapper(ApplicationSettingsBase settingBase, SettingsProperty settingProperty)
        {
            m_settingBase = settingBase;
            m_settingProperty = settingProperty;

            m_PropertyChangeHandler = new PropertyChangedNotifyBase();
            m_PropertyChangeHandler.InitializeDependentProperties(this);

            m_OriginalValue = Value;
            
            IsValid = true;
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
                string str = Name
                    + "="
                    + (Value ?? "N/A !")
                    + (string.IsNullOrEmpty(Message) ? "" : " '" + Message + "'")
                    + " (default: " + DefaultValue + ") ";
                return str;
            }
        }

        private readonly object m_OriginalValue = null;
        public object OriginalValue
        {
            get
            {
                return m_OriginalValue;
            }
        }

        [NotifyDependsOn("Value")]
        public bool IsChanged
        {
            get
            {
                if (OriginalValue == null || Value == null) return false;
                return !OriginalValue.Equals(Value);
            }
        }

        public bool IsValid
        {
            get; set;
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

            if (IsValid)
            {
                Message = IsChanged
                              ? String.Format(Tobi_Plugin_Settings_Lang.Settings_OriginalValue, OriginalValue)
                              : null;
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

        public bool SearchMatch
        {
            get { return m_isMatch; }
            set
            {
                if (m_isMatch == value) { return; }
                m_isMatch = value;
                m_PropertyChangeHandler.RaisePropertyChanged(() => SearchMatch);
            }
        }
        private bool m_isSelected;
        public bool IsSelected
        {
            get { return m_isSelected; }
            set
            {
                if (m_isSelected == value) { return; }
                m_isSelected = value;
                m_PropertyChangeHandler.RaisePropertyChanged(() => IsSelected);
            }
        }

        private bool m_isChecked;
        public bool IsChecked
        {
            get { return m_isChecked; }
            set
            {
                if (m_isChecked == value) { return; }
                m_isChecked = value;
                m_PropertyChangeHandler.RaisePropertyChanged(() => IsChecked);
            }
        }

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
}
