using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tobi.Common.UI;
using Tobi.Common.UI.XAML;

namespace Tobi.Plugin.Settings
{
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

        protected ValidationResult Valid(string message)
        {
            var currentSetting = (SettingWrapper)DataContextSpy.DataContext;
            currentSetting.Message = message;
            return new ValidationResult(true, null);
        }

        protected ValidationResult Valid()
        {
            return Valid(null);
        }
    }

    public class KeyGestureValidationRule : DataContextValidationRuleBase
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var str = value as string;
            if (String.IsNullOrEmpty(str))
            {
                return NotValid("Value cannot be empty.");        // TODO LOCALIZE ValueNotEmpty
            }

            KeyGesture val = KeyGestureStringConverter.Convert(str);
            if (val == null)
            {
                return NotValid("Invalid keyboard shortcut.");      //  TODO LOCALIZE InvalidKeyboardShortcut
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
                    var keyG = (KeyGesture)aggregatedSetting.Value;

                    if (keyG != null && val.Key == keyG.Key)
                    {
                        if (val.Modifiers.Equals(keyG.Modifiers))
                        {
                            strSettingsAlreadyUsingKeyG += aggregatedSetting.Name;
                            strSettingsAlreadyUsingKeyG += " ";
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(strSettingsAlreadyUsingKeyG))
            {
                return Valid("Shortcut already used by: " + strSettingsAlreadyUsingKeyG);     // TODO LOCALIZE ShortcutAlreadyUsed
            }

            return Valid();
        }
    }

    public class TextValidationRule : DataContextValidationRuleBase
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var str = value as string;
            if (String.IsNullOrEmpty(str))
            {
                return NotValid("String cannot be empty.");               // TODO LOCALIZE StringCannotBeEmpty
            }
            if (str.Trim().Length == 0)
            {
                return NotValid("String cannot just be separators (e.g. space characters).");      // TODO LOCALIZE StringCannotJustBeSeparators
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
                return NotValid("Value cannot be empty.");            // TODO LOCALIZE String already present
            }

            double val;
            if (!Double.TryParse(str, out val))
            {
                return NotValid("Invalid numeric value.");                 // TODO LOCALIZE InvalidNumericValue
            }

            var currentSetting = (SettingWrapper)DataContextSpy.DataContext;
            string lowName = currentSetting.Name.ToLower();

            //TODO: remove this Super-master hack !
            bool mustBePositive = lowName.Contains("width") || lowName.Contains("height");
            mustBePositive = true;

            if (mustBePositive)
            {
                if (val < 0 || val > 9999)
                {
                    return NotValid("Numeric value is out of range [0, 9999].");      // TODO LOCALIZE NumericValueOutOfRange
                }
            }
            else
            {
                if (val < -9999 || val > 9999)
                {
                    return NotValid("Numeric value is out of range [-9999, 9999].");           // TODO LOCALIZE NumericValueOutOfRange9999
                }
            }

            return Valid();
        }
    }
}
