﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RZScreenSaver.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.7.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int SlideShowDelay {
            get {
                return ((int)(this["SlideShowDelay"]));
            }
            set {
                this["SlideShowDelay"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Random")]
        public global::RZScreenSaver.SlideMode SlideMode {
            get {
                return ((global::RZScreenSaver.SlideMode)(this["SlideMode"]));
            }
            set {
                this["SlideMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("OriginalOrFit")]
        public global::RZScreenSaver.DisplayMode DisplayMode {
            get {
                return ((global::RZScreenSaver.DisplayMode)(this["DisplayMode"]));
            }
            set {
                this["DisplayMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int LastImageIndex {
            get {
                return ((int)(this["LastImageIndex"]));
            }
            set {
                this["LastImageIndex"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool ShowTitle {
            get {
                return ((bool)(this["ShowTitle"]));
            }
            set {
                this["ShowTitle"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string LastSelectedFolder {
            get {
                return ((string)(this["LastSelectedFolder"]));
            }
            set {
                this["LastSelectedFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("SlideShow")]
        public global::RZScreenSaver.SaverMode SaverMode {
            get {
                return ((global::RZScreenSaver.SaverMode)(this["SaverMode"]));
            }
            set {
                this["SaverMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int PictureSetSelected {
            get {
                return ((int)(this["PictureSetSelected"]));
            }
            set {
                this["PictureSetSelected"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int BackgroundPictureSetSelected {
            get {
                return ((int)(this["BackgroundPictureSetSelected"]));
            }
            set {
                this["BackgroundPictureSetSelected"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("40")]
        public int PhotoCollageAngle {
            get {
                return ((int)(this["PhotoCollageAngle"]));
            }
            set {
                this["PhotoCollageAngle"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.25")]
        public double MinSquareCardSize {
            get {
                return ((double)(this["MinSquareCardSize"]));
            }
            set {
                this["MinSquareCardSize"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.5")]
        public double MaxSquareCardSize {
            get {
                return ((double)(this["MaxSquareCardSize"]));
            }
            set {
                this["MaxSquareCardSize"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string BackgroundPicturePath {
            get {
                return ((string)(this["BackgroundPicturePath"]));
            }
            set {
                this["BackgroundPicturePath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"
                    <ArrayOfArrayOfFolderInclusion xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
                        <ArrayOfFolderInclusion />
                    </ArrayOfArrayOfFolderInclusion>
                ")]
        public global::RZScreenSaver.FolderCollectionSet PicturePaths {
            get {
                return ((global::RZScreenSaver.FolderCollectionSet)(this["PicturePaths"]));
            }
            set {
                this["PicturePaths"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int LastShownIndex {
            get {
                return ((int)(this["LastShownIndex"]));
            }
            set {
                this["LastShownIndex"] = value;
            }
        }
    }
}