﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.3603
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Tobi.Plugin.Validator {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "9.0.0.0")]
    public sealed partial class Settings_KeyGestures : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings_KeyGestures defaultInstance = ((Settings_KeyGestures)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings_KeyGestures())));
        
        public static Settings_KeyGestures Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[ NONE ] F12")]
        public global::Tobi.Common.UI.KeyGestureString Keyboard_DisplayValidator {
            get {
                return ((global::Tobi.Common.UI.KeyGestureString)(this["Keyboard_DisplayValidator"]));
            }
            set {
                this["Keyboard_DisplayValidator"] = value;
            }
        }
    }
}
