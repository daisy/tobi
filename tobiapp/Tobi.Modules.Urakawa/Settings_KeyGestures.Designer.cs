﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.3603
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Tobi.Plugin.Urakawa {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "9.0.0.0")]
    internal sealed partial class Settings_KeyGestures : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings_KeyGestures defaultInstance = ((Settings_KeyGestures)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings_KeyGestures())));
        
        public static Settings_KeyGestures Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[ CTRL ] O")]
        public global::Tobi.Common.UI.KeyGestureString Keyboard_Open {
            get {
                return ((global::Tobi.Common.UI.KeyGestureString)(this["Keyboard_Open"]));
            }
            set {
                this["Keyboard_Open"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[ CTRL ] W")]
        public global::Tobi.Common.UI.KeyGestureString Keyboard_Close {
            get {
                return ((global::Tobi.Common.UI.KeyGestureString)(this["Keyboard_Close"]));
            }
            set {
                this["Keyboard_Close"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[ CTRL ] Z")]
        public global::Tobi.Common.UI.KeyGestureString Keyboard_Undo {
            get {
                return ((global::Tobi.Common.UI.KeyGestureString)(this["Keyboard_Undo"]));
            }
            set {
                this["Keyboard_Undo"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[ CTRL ] Y")]
        public global::Tobi.Common.UI.KeyGestureString Keyboard_Redo {
            get {
                return ((global::Tobi.Common.UI.KeyGestureString)(this["Keyboard_Redo"]));
            }
            set {
                this["Keyboard_Redo"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[ CTRL ] S")]
        public global::Tobi.Common.UI.KeyGestureString Keyboard_Save {
            get {
                return ((global::Tobi.Common.UI.KeyGestureString)(this["Keyboard_Save"]));
            }
            set {
                this["Keyboard_Save"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[ CTRL ] E")]
        public global::Tobi.Common.UI.KeyGestureString Keyboard_Export {
            get {
                return ((global::Tobi.Common.UI.KeyGestureString)(this["Keyboard_Export"]));
            }
            set {
                this["Keyboard_Export"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("[ SHIFT CTRL ] S")]
        public global::Tobi.Common.UI.KeyGestureString Keyboard_SaveAs {
            get {
                return ((global::Tobi.Common.UI.KeyGestureString)(this["Keyboard_SaveAs"]));
            }
            set {
                this["Keyboard_SaveAs"] = value;
            }
        }
    }
}
