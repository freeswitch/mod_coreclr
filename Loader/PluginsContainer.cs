using PluginInterface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FreeSWITCH
{
    public static class PluginsContainer
    {
        public static List<PluginLoadContext> pluginLoadContexts = new List<PluginLoadContext>();


        public static bool LoadPlugin(string pluginPath)
        {
            var pluginLocation = Path.GetFullPath(pluginPath.Replace('\\', Path.DirectorySeparatorChar));
            Log.WriteLine(LogLevel.Console, $"Loader: Attempting to load module {pluginLocation}");
            var loadContext = new PluginLoadContext(pluginLocation);
            var aname = new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation));
            var loadedAssembly = loadContext.LoadFromAssemblyName(aname);
            if (loadedAssembly != null)
            {
                BuildDispatchers(loadedAssembly, loadContext);
            }
            if (loadContext.Dispatchers.Count == 0)
            {
                return false;
            }
            pluginLoadContexts.Add(loadContext);
            Console.WriteLine($"LoadedPlugin - {pluginLocation}");
            Console.WriteLine($"APIs: {string.Join(',', loadContext.Dispatchers.SelectMany(d => d.GetApiNames()))}");
            Console.WriteLine($"DPApps: {string.Join(',', loadContext.Dispatchers.SelectMany(d => d.GetDPNames()))}");
            return true;
        }

        public static void LoadPluginsFromSubDirs(string path)
        {
            var dirs = Directory.GetDirectories(path);
            foreach (var d in dirs)
            {
                foreach (var f in Directory.GetFiles(d))
                {
                    var filename = Path.GetFileName(f);
                    if (filename == Path.GetFileName(d) + ".dll")
                    {
                        LoadPlugin(f);
                    }
                }
            }
        }

        public static int DispatchAPI(string args, Stream stream, ManagedSession session)
        {
            var argTokens = args.Trim().Split(" ".ToCharArray());
            if (string.Equals(argTokens[0], "loadall", StringComparison.OrdinalIgnoreCase))
            {
                var myLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                LoadPluginsFromSubDirs(myLocation);
                return 0;
            }

            var dispatcher = pluginLoadContexts.SelectMany(c => c.Dispatchers).FirstOrDefault(d => d.GetApiNames().Contains(argTokens[0]));
            if (dispatcher == null)
            {
                Log.WriteLine(LogLevel.Info, $"mod_coreclr unknown dotnet command: {args}");
                return 0;
            }
            return dispatcher.DispatchAPI(args, stream, session);
        }

        public static bool DispatchDialPlanApp(string args, ManagedSession session)
        {
            var argTokens = args.Trim().Split(" ".ToCharArray());
            var dispatcher = pluginLoadContexts.SelectMany(c => c.Dispatchers).FirstOrDefault(d => d.GetDPNames().Contains(argTokens[0]));
            if (dispatcher == null)
            {
                return false;
            }
            dispatcher.DispatchDialPlan(args, session);
            return true;
        }

        public static string DispatchXMLCallback(string section, string tag, string key, string value, Event evt)
        {
            foreach (var dispatcher in pluginLoadContexts.SelectMany(c => c.Dispatchers))
            {
                var result = dispatcher.DispatchXMLCallback(section, tag, key, value, evt);
                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
            }
            return null;
        }

        private static void BuildDispatchers(Assembly assembly, PluginLoadContext context)
        {
            foreach (Type type in assembly.GetTypes())
            {
                Log.WriteLine(LogLevel.Console, $"Trying type {type.Name}");
                if (typeof(IPluginDispatcher).IsAssignableFrom(type))
                {
                    IPluginDispatcher result = Activator.CreateInstance(type) as IPluginDispatcher;
                    if (result != null)
                    {
                        try
                        {
                            if (result.Onload())
                            {
                                context.Dispatchers.Add(result);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.WriteLine(LogLevel.Error, $"Exception in OnLoad() {e.Message}");
                        }
                    }
                }
            }
        }

    }
}
