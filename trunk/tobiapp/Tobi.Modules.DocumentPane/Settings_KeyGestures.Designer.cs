﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30128.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Tobi.Plugin.DocumentPane {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "10.0.0.0")]
    public sealed partial class Settings_KeyGestures : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings_KeyGestures defaultInstance = ((Settings_KeyGestures)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings_KeyGestures())));
        
        public static Settings_KeyGestures Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[ CTRL ] OemComma (,)")]
        public global::Tobi.Common.UI.KeyGestureString Keyboard_Doc_Event_SwitchPrevious {
            get {
                return ((global::Tobi.Common.UI.KeyGestureString)(this["Keyboard_Doc_Event_SwitchPrevious"]));
            }
            set {
                this["Keyboard_Doc_Event_SwitchPrevious"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[ CTRL ] OemPeriod (.)")]
        public global::Tobi.Common.UI.KeyGestureString Keyboard_Doc_Event_SwitchNext {
            get {
                return ((global::Tobi.Common.UI.KeyGestureString)(this["Keyboard_Doc_Event_SwitchNext"]));
            }
            set {
                this["Keyboard_Doc_Event_SwitchNext"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[ SHIFT CTRL ] OemComma (,)")]
        public global::Tobi.Common.UI.KeyGestureString Keyboard_StructureSelectUp {
            get {
                return ((global::Tobi.Common.UI.KeyGestureString)(this["Keyboard_StructureSelectUp"]));
            }
            set {
                this["Keyboard_StructureSelectUp"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[ SHIFT CTRL ] OemPeriod (.)")]
        public global::Tobi.Common.UI.KeyGestureString Keyboard_StructureSelectDown {
            get {
                return ((global::Tobi.Common.UI.KeyGestureString)(this["Keyboard_StructureSelectDown"]));
            }
            set {
                this["Keyboard_StructureSelectDown"] = value;
            }
        }
    }
}
