using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Tobi.Common.UI;

namespace Tobi.Plugin.Settings
{
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
                if (((SettingWrapper)item).ValueType == typeof(Color))
                {
                    var t4 = ((ContentPresenter)container).FindResource("SettingEditTemplate_Color") as DataTemplate;
                    return t4;
                }
                if (((SettingWrapper)item).ValueType == typeof(FontFamily))
                {
                    var t5 = ((ContentPresenter)container).FindResource("SettingEditTemplate_FontFamily") as DataTemplate;
                    return t5;
                }

                // see new ENUM support below
                //if (((SettingWrapper)item).ValueType == typeof(TextAlignment))
                //{
                //    var t6 = ((ContentPresenter)container).FindResource("SettingEditTemplate_TextAlignment") as DataTemplate;
                //    return t6;
                //}

                if (typeof(Enum).IsAssignableFrom(((SettingWrapper)item).ValueType))
                {
                    var t7 = ((ContentPresenter)container).FindResource("SettingEditTemplate_Enum") as DataTemplate;
                    return t7;
                }
            }
            var defaultTemplate = ((ContentPresenter)container).FindResource("SettingEditTemplate_Text") as DataTemplate;
            return defaultTemplate;
        }
    }
}
