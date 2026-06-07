using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MyMonitorHub.Agent.Core
{
    public class GroupPlugin : AbstractPlugin
    {
        private static readonly ILogger Logger = Log.ForContext<GroupPlugin>();
        private readonly ArrayList _pluginList = new ArrayList();

        public override void Initialize(JToken groupPluginElement, string threadTitle)
        {
            base.Initialize(groupPluginElement, threadTitle);
            var pluginNodeList = groupPluginElement["plugin"] as JArray;

            if (pluginNodeList != null)
            {
                var length = pluginNodeList.Count;
                for (var index = 0; index < length; index++)
                {
                    var pluginElement = pluginNodeList[index];

                    var className = pluginElement["class-name"]?.ToString();
                    var assemblyName = pluginElement["assembly"]?.ToString();

                    var assembly = assemblyName == null ? GetType().Assembly : LoadPluginAssembly(assemblyName);

                    // create an instance of the class from the assembly
                    var o = assembly.CreateInstance(className!);

                    if (!(o is AbstractPlugin))
                    {
                        throw new Exception($"class ({className})must implement AbstractPlugin");
                    }
                    var plugin = (AbstractPlugin)o;
                    plugin.Initialize(pluginElement, Title ?? string.Empty);

                    _pluginList.Add(plugin);
                }
            }
        }

        /// <summary>
        /// Resolves a plugin assembly path relative to the install directory and validates
        /// it does not escape via path traversal. Prevents CWD-hijacking (A-H5).
        /// </summary>
        private static Assembly LoadPluginAssembly(string assemblyName)
        {
            var baseDir = AppContext.BaseDirectory;
            var fullPath = Path.GetFullPath(Path.Combine(baseDir, assemblyName));
            if (!fullPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"Plugin assembly path '{assemblyName}' resolves outside the install directory ('{baseDir}'); load rejected.");
            return Assembly.LoadFrom(fullPath);
        }

        public virtual void DisplayPlugins()
        {
            foreach (AbstractPlugin plugin in _pluginList)
            {
                Logger.Information("    Plugin: " + plugin.Title + " - runs every " + plugin.HibernateSeconds +
                                  " seconds");
            }
        }

        protected override void Execute()
        {
            // this thread runs every 30 seconds as defined in GroupPluginThread.hibernateSeconds however each plugin runs as
            // every 2 minutes or longer

            foreach (AbstractPlugin plugin in _pluginList)
            {
                if (plugin.CanRun())
                {
                    plugin.Start();
                }
            }
        }
    }
}