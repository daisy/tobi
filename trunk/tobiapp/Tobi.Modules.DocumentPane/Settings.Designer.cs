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
    public sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Times New Roman")]
        public global::System.Windows.Media.FontFamily Document_Font {
            get {
                return ((global::System.Windows.Media.FontFamily)(this["Document_Font"]));
            }
            set {
                this["Document_Font"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("110")]
        public double Document_Zoom {
            get {
                return ((double)(this["Document_Zoom"]));
            }
            set {
                this["Document_Zoom"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool Document_ButtonBarVisible {
            get {
                return ((bool)(this["Document_ButtonBarVisible"]));
            }
            set {
                this["Document_ButtonBarVisible"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("#ff000000")]
        public global::System.Windows.Media.Color Document_Color_Font_Audio {
            get {
                return ((global::System.Windows.Media.Color)(this["Document_Color_Font_Audio"]));
            }
            set {
                this["Document_Color_Font_Audio"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("#ffa9a9a9")]
        public global::System.Windows.Media.Color Document_Color_Font_NoAudio {
            get {
                return ((global::System.Windows.Media.Color)(this["Document_Color_Font_NoAudio"]));
            }
            set {
                this["Document_Color_Font_NoAudio"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("#ffff4500")]
        public global::System.Windows.Media.Color Document_Color_Selection_UnderOverLine {
            get {
                return ((global::System.Windows.Media.Color)(this["Document_Color_Selection_UnderOverLine"]));
            }
            set {
                this["Document_Color_Selection_UnderOverLine"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("#ff000000")]
        public global::System.Windows.Media.Color Document_Color_Selection_Font {
            get {
                return ((global::System.Windows.Media.Color)(this["Document_Color_Selection_Font"]));
            }
            set {
                this["Document_Color_Selection_Font"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("#ffff4500")]
        public global::System.Windows.Media.Color Document_Color_Selection_Border {
            get {
                return ((global::System.Windows.Media.Color)(this["Document_Color_Selection_Border"]));
            }
            set {
                this["Document_Color_Selection_Border"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("#fffafad2")]
        public global::System.Windows.Media.Color Document_Color_Selection_Back1 {
            get {
                return ((global::System.Windows.Media.Color)(this["Document_Color_Selection_Back1"]));
            }
            set {
                this["Document_Color_Selection_Back1"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("#ffffff00")]
        public global::System.Windows.Media.Color Document_Color_Selection_Back2 {
            get {
                return ((global::System.Windows.Media.Color)(this["Document_Color_Selection_Back2"]));
            }
            set {
                this["Document_Color_Selection_Back2"] = value;
            }
        }
    }
}