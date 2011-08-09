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
            currentSetting.IsValid = false;
            currentSetting.Message = msg;
            return new ValidationResult(false, msg);
        }

        protected ValidationResult Valid(string message)
        {
            var currentSetting = (SettingWrapper)DataContextSpy.DataContext;
            currentSetting.IsValid = true;
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
                return NotValid(Tobi_Plugin_Settings_Lang.ValueNotEmpty);
            }

            KeyGesture val = KeyGestureStringConverter.Convert(str);
            if (val == null)
            {
                return NotValid(Tobi_Plugin_Settings_Lang.InvalidKeyboardShortcut); 
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
                return NotValid(String.Format(Tobi_Plugin_Settings_Lang.ShortcutAlreadyUsed, strSettingsAlreadyUsingKeyG));
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
                return NotValid(Tobi_Plugin_Settings_Lang.StringCannotBeEmpty);
            }
            if (str.Trim().Length == 0)
            {
                return NotValid(Tobi_Plugin_Settings_Lang.StringCannotJustBeSeparators);
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
                return NotValid(Tobi_Plugin_Settings_Lang.ValueNotEmpty);
            }

            double val;
            if (!Double.TryParse(str, out val))
            {
                return NotValid(Tobi_Plugin_Settings_Lang.InvalidNumericValue);
            }

            var currentSetting = (SettingWrapper)DataContextSpy.DataContext;
            string lowName = currentSetting.Name.ToLower();


            //bool mustBePositive = lowName.Contains("width") || lowName.Contains("height");
            bool mustBePositive = true;

            if (mustBePositive)
            {
                if (val < 0 || val > 9999)
                {
                    return NotValid(Tobi_Plugin_Settings_Lang.NumericValueOutOfRange);
                }
            }
            else
            {
                if (val < -9999 || val > 9999)
                {
                    return NotValid(Tobi_Plugin_Settings_Lang.NumericValueOutOfRange9999);
                }
            }

            return Valid();
        }
    }
    public class EnumValidationRule : DataContextValidationRuleBase
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var str = value as string;
            if (String.IsNullOrEmpty(str))
            {
                return NotValid(Tobi_Plugin_Settings_Lang.ValueNotEmpty);
            }

            var currentSetting = (SettingWrapper)DataContextSpy.DataContext;
            //string lowName = currentSetting.Name.ToLower();

            try
            {
                var obj = Enum.Parse(currentSetting.ValueType, str, true);
                if (!Enum.IsDefined(currentSetting.ValueType, obj))
                {
                    throw new Exception();
                }
            }
            catch
            {
                var strAppend = "";
                if (typeof(Enum).IsAssignableFrom(currentSetting.ValueType))
                {
                    var names = Enum.GetNames(currentSetting.ValueType);
                    strAppend = "";
                    foreach (var name in names)
                    {
                        strAppend += " ";
                        strAppend += name;
                    }
                    strAppend += " ";
                }
                return NotValid(string.Format(Tobi_Plugin_Settings_Lang.InvalidEnumValue, strAppend));
            }

            return Valid();
        }
    }

    //public class TextAlignmentValidationRule : DataContextValidationRuleBase
    //{
    //    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    //    {
    //        var str = value as string;
    //        if (String.IsNullOrEmpty(str))
    //        {
    //            return NotValid(Tobi_Plugin_Settings_Lang.ValueNotEmpty);
    //        }

    //        TextAlignment val;
    //        if (!Enum.TryParse(str, out val))
    //        {
    //            var converter = new TextAlignmentToStringConverter();
    //            bool worked = false;
    //            try
    //            {
    //                var align =
    //                    (TextAlignment)
    //                    converter.ConvertBack(str, typeof(TextAlignment), null, CultureInfo.InvariantCulture);
    //                worked = true;
    //            }
    //            catch
    //            {
    //                //ignore
    //            }
    //            if (!worked)
    //            {
    //                return NotValid(Tobi_Plugin_Settings_Lang.InvalidTextAlignment);
    //            }
    //        }

    //        //var currentSetting = (SettingWrapper)DataContextSpy.DataContext;
    //        //string lowName = currentSetting.Name.ToLower();

    //        return Valid();
    //    }
    //}
}
