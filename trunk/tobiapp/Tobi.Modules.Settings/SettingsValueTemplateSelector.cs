using System;
using System.Windows;
using System.Windows.Controls;
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
            }
            var defaultTemplate = ((ContentPresenter)container).FindResource("SettingEditTemplate_Text") as DataTemplate;
            return defaultTemplate;
        }
    }
}
