﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DoenaSoft.CopySeries {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.1.0.0")]
    internal sealed partial class DirAtSourceSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static DirAtSourceSettings defaultInstance = ((DirAtSourceSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new DirAtSourceSettings())));
        
        public static DirAtSourceSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\source")]
        public string LocalDir {
            get {
                return ((string)(this["LocalDir"]));
            }
            set {
                this["LocalDir"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("S:\\")]
        public string StickDrive {
            get {
                return ((string)(this["StickDrive"]));
            }
            set {
                this["StickDrive"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\Program Files (x86)\\Beyond Compare 4\\BCompare.exe")]
        public string BeyondCompare {
            get {
                return ((string)(this["BeyondCompare"]));
            }
            set {
                this["BeyondCompare"] = value;
            }
        }
    }
}
