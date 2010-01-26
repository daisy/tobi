using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Configuration;
using Tobi.Common;

namespace Tobi.Plugin.AudioPane
{
    [Export(typeof(ISettingsProvider)), PartCreationPolicy(CreationPolicy.Shared)]
    public class SettingsProvider : ISettingsProvider, IPartImportsSatisfiedNotification
    {

#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        public ApplicationSettingsBase Settings
        {
            get
            {
                return AudioPane.Settings.Default;
            }
        }
    }


    [Export(typeof(ISettingsProvider)), PartCreationPolicy(CreationPolicy.Shared)]
    public class SettingsProvider_KeyGestures : ISettingsProvider, IPartImportsSatisfiedNotification
    {

#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif
        }

        public ApplicationSettingsBase Settings
        {
            get { return Settings_KeyGestures.Default; }
        }
    }

    internal sealed partial class Settings
    {
        public Settings()
        {
            //PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Settings_PropertyChanged);
        }
        //~Settings()
        //{
        //    Save();
        //}
        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //TODO: 
        }

    }

    internal sealed partial class Settings_KeyGestures
    {
        public Settings_KeyGestures()
        {
            //PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Settings_PropertyChanged);
        }
        //~Settings_KeyGestures()
        //{
        //    Save();
        //}
        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //TODO: 
        }
    }


}
