using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Configuration;
using Tobi.Common;

namespace Tobi.Plugin.AudioPane
    {
    [Export ( typeof ( ISettingsProvider ) ), PartCreationPolicy ( CreationPolicy.Shared )]
    public class AudioSettingsProvider : ISettingsProvider, IPartImportsSatisfiedNotification
        {

#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied ()
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
                return AudioPluginSettings.Default;
                }
            }
        }


    [Export ( typeof ( ISettingsProvider ) ), PartCreationPolicy ( CreationPolicy.Shared )]
    public class SettingsProvider_KeyGestures : ISettingsProvider, IPartImportsSatisfiedNotification
        {

#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied ()
#pragma warning restore 1591
            {
            //#if DEBUG
            //            Debugger.Break();
            //#endif
            }

        public ApplicationSettingsBase Settings
            {
            get { return AudioPlugin_KeyGestures.Default; }
            }
        }

    internal sealed partial class AudioPluginSettings
        {

        public AudioPluginSettings ()
            {
            PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler ( Settings_PropertyChanged );
            }
        //~Settings()
        //{
        //    Save();
        //}
        private void Settings_PropertyChanged ( object sender, PropertyChangedEventArgs e )
            {
            //TODO: 
            }

        }

    internal sealed partial class AudioPlugin_KeyGestures
        {

        public AudioPlugin_KeyGestures ()
            {
            PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler ( Settings_PropertyChanged );
            }
        //~Settings_KeyGestures()
        //{
        //    Save();
        //}
        private void Settings_PropertyChanged ( object sender, PropertyChangedEventArgs e )
            {
            //TODO: 
            }
        }


    }
