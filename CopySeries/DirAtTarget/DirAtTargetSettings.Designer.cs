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
    internal sealed partial class DirAtTargetSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static DirAtTargetSettings defaultInstance = ((DirAtTargetSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new DirAtTargetSettings())));
        
        public static DirAtTargetSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\target")]
        public string TargetPath {
            get {
                return ((string)(this["TargetPath"]));
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
    }
}
