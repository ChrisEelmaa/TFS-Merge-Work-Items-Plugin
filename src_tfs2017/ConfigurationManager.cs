using Microsoft.TeamFoundation.Framework.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Inmeta.TFS.MergeWorkItemsEventHandler
{
    public static class ConfigurationManager
    {
        private static XDocument _ConfigDocument;
        private static FileSystemWatcher _Watcher;
        private static readonly string _XmlFileName;

        static ConfigurationManager()
        {
            try
            {
                var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Substring(8));
                _XmlFileName = Path.Combine(assemblyDir, "Inmeta.TFS.MergeWorkItemsEventHandler.dll.config");

                MonitorConfigFile(_XmlFileName);
                LoadDocument(_XmlFileName);
            }
            catch (Exception ex)
            {
                TeamFoundationApplicationCore.LogException("Inmeta.TFS.MergeWorkItemEventHandler encountered an exception", ex);
            }
        }

        private static void LoadDocument(string xmlFileName)
        {
            _ConfigDocument = XDocument.Load(xmlFileName);
            VerboseLog = GetVerboseLogFromXml();
        }

        private static void MonitorConfigFile(string xmlFileName)
        {
            _Watcher = new FileSystemWatcher(Path.GetDirectoryName(xmlFileName), Path.GetFileName(xmlFileName));
            _Watcher.Changed += WatcherOnChanged;
            _Watcher.EnableRaisingEvents = true;
        }

        private static void WatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            TeamFoundationApplicationCore.Log("Inmeta.TFS.MergeWorkItemEventHandler configuration file changed. Reloading...", 1, EventLogEntryType.Information);
            LoadDocument(_XmlFileName);
        }
        

        public static bool VerboseLog { get; private set; }


        public static string AppSettings(string key)
        {
            var elem = _ConfigDocument.XPathSelectElement($"/configuration/appSettings/add[@key='{key}']");
            var value = elem.Attribute("value").Value;

            return value;
        }

        private static bool GetVerboseLogFromXml()
        {
            var attribute = _ConfigDocument.Document.Root.Attribute("verboseLog");
            return (attribute != null) && (attribute.Value.Equals("true", StringComparison.OrdinalIgnoreCase));
        }
    }
}
