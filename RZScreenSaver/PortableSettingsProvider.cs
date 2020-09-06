using System;
using System.Collections;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using SysDbg = System.Diagnostics.Debug;

namespace RZScreenSaver{
    public class PortableSettingsProvider : SettingsProvider{
        const string SettingsRoot = "Settings";
        XmlDocument settingsXml;

        public override void Initialize(string name,System.Collections.Specialized.NameValueCollection config) {
            base.Initialize(ApplicationName,config);
        }
        #region Overrides of SettingsProvider
        
        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection){
            var values = new SettingsPropertyValueCollection();
            foreach (SettingsProperty property in collection){
                var value = new SettingsPropertyValue(property){
                                                                   IsDirty = false,
                                                                   SerializedValue = getValue(property),
                                                               };
                values.Add(value);
            }
            return values;
        }
        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection){
            foreach (SettingsPropertyValue propVal in collection){
                setValue(propVal);
            }
            try{
                SettingsXml.Save(Path.Combine(AppSettingsPath, AppSettingsFilename));
            }catch (IOException e){
                SysDbg.WriteLine("Xml saving error: " + e);
            }
        }
        public override string ApplicationName{
            get { return Assembly.GetCallingAssembly().GetName().Name; }
            set{
                System.Diagnostics.Debug.WriteLine("Application name " + value + " is set.");
            }
        }
        #endregion
        XmlDocument SettingsXml{
            get{
                if (settingsXml == null){
                    settingsXml = new XmlDocument();
                    try{
                        var settingFilePath = Path.Combine(AppSettingsPath, AppSettingsFilename);
                        System.Diagnostics.Debug.WriteLine("Setting file is @ " + settingFilePath);
                        settingsXml.Load(settingFilePath);
                    }
                    catch(Exception ex){
                        if (!(ex is FileNotFoundException) && !(ex is NotSupportedException))
                            throw;
                        var dec = settingsXml.CreateXmlDeclaration("1.0", "utf-8", String.Empty);
                        settingsXml.AppendChild(dec);
                        var nodeRoot = settingsXml.CreateNode(XmlNodeType.Element, SettingsRoot, string.Empty);
                        settingsXml.AppendChild(nodeRoot);
                    }
                }
                return settingsXml;
            }
        }
        protected virtual string AppSettingsPath{
            get{
                var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (String.IsNullOrEmpty(path))
                    return Application.LocalUserAppDataPath;
                else{
                    var asmName = typeof (MainApp).Assembly.GetName().Name;
                    path = String.Concat(path, "\\Zodiac\\", asmName);
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                    return path;
                }
            }
        }
        protected virtual string AppSettingsFilename{
            get { return ApplicationName + ".settings"; }
        }
        static bool isRoaming(SettingsProperty prop){
            return prop.Attributes.Cast<DictionaryEntry>().Any(d => d.Value is SettingsManageabilityAttribute);
        }
        string getValue(SettingsProperty setting){
            var propertyNode = getSettingsNode(setting, setting.Name);
            var ret = (propertyNode != null)
                          ? propertyNode.InnerText
                          : (setting.DefaultValue != null) ? setting.DefaultValue.ToString() : String.Empty;
            return ret;
        }
        void setValue(SettingsPropertyValue propVal){
            var settingNode = getSettingsNode(propVal.Property, propVal.Name) as XmlElement;
            if (settingNode != null){
                settingNode.InnerText = propVal.SerializedValue.ToString();
            }else if (isRoaming(propVal.Property)){
                settingNode = SettingsXml.CreateElement(propVal.Name);
                settingNode.InnerText = propVal.SerializedValue.ToString();
                SettingsXml.SelectSingleNode(SettingsRoot).AppendChild(settingNode);
            } else{
                // it's machine specific, store as an element of the machine name node.
                var machineNode = SettingsXml.SelectSingleNode(SettingsRoot + '/' + Environment.MachineName) as XmlElement;
                if (machineNode == null){
                    machineNode = SettingsXml.CreateElement(Environment.MachineName);
                    SettingsXml.SelectSingleNode(SettingsRoot).AppendChild(machineNode);
                }
                settingNode = SettingsXml.CreateElement(propVal.Name);
                settingNode.InnerText = propVal.SerializedValue.ToString();
                machineNode.AppendChild(settingNode);
            }
        }
        XmlNode getSettingsNode(SettingsProperty setting, string name){
            return isRoaming(setting)
                       ? SettingsXml.SelectSingleNode(SettingsRoot + '/' + name)
                       : SettingsXml.SelectSingleNode(SettingsRoot + '/' + Environment.MachineName + '/' + name);
        }
    }
}