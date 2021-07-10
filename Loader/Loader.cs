using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using PluginInterface;
using FreeSWITCH.Helpers;
using System.Reflection;

namespace FreeSWITCH
{
    public static class Loader
    {
        // The primary entry point delegate for obtaining the native callbacks during host initialization
        private delegate NativeCallbacks LoadDelegate();

        // The FreeSWITCH API interface callback delegate
        private delegate int NativeAPICallback(string command, IntPtr sessionptr, IntPtr streamptr);

        // The FreeSWITCH APP interface callback delegate
        private delegate void NativeAPPCallback(IntPtr sessionptr, string data);

        // The FreeSWITCH APP interface callback delegate
        private delegate string NativeXMLCallback(string section, string tag, string key, string value, IntPtr eventptr);

        // Contains all native callbacks that will be called from the native host, each callback
        // is produced by marshalling delegates to native function pointers
        // Important: Must maintain the same structure in native_callbacks_t in mod_coreclr.c
        [StructLayout(LayoutKind.Sequential)]
        public struct NativeCallbacks
        {
            public IntPtr NativeAPICallback;
            public IntPtr NativeAPPCallback;
            public IntPtr NativeXMLCallback;
        }

        private static NativeAPICallback sNativeAPICallback = null;
        private static NativeAPPCallback sNativeAPPCallback = null;
        private static NativeXMLCallback sNativeXMLCallback = null;

        // This is the only predefined entry point, this must match what mod_coreclr.c is looking for
        public static NativeCallbacks Load()
        {
            // Register some reserved Managed API's
            //sAPIRegistry.TryAdd("load", LoadAPI);
            //var myLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //PluginsContainer.LoadPluginsFromSubDirs(myLocation);

            sNativeAPICallback = new NativeAPICallback(NativeAPIHandler);
            sNativeAPPCallback = new NativeAPPCallback(NativeAPPHandler);
            sNativeXMLCallback = new NativeXMLCallback(NativeXMLHandler);
            // Return the marshalled callbacks for the native interfaces
            return new NativeCallbacks
            {
                NativeAPICallback = Marshal.GetFunctionPointerForDelegate<NativeAPICallback>(sNativeAPICallback),
                NativeAPPCallback = Marshal.GetFunctionPointerForDelegate<NativeAPPCallback>(sNativeAPPCallback),
                NativeXMLCallback = Marshal.GetFunctionPointerForDelegate<NativeXMLCallback>(sNativeXMLCallback)
            };
        }

        // The Managed API interface callback delegate
        public delegate string APICallback(string args, ManagedSession session);
        // The Managed API callback registry
        // TODO: value type subject to change to a more complex type including an APIAttribute configuration
        private static ConcurrentDictionary<string, APICallback> sAPIRegistry = new ConcurrentDictionary<string, APICallback>();

        // This is the FreeSWITCH API interface callback handler which is bound to "coreclr" API commands
        private static int NativeAPIHandler(string command, IntPtr sessionptr, IntPtr streamptr)
        {
            using ManagedSession session = new ManagedSession(new SWIGTYPE_p_switch_core_session_t(sessionptr, false));
            using Stream stream = new Stream(new SWIGTYPE_p_switch_stream_handle_t(streamptr, false));


            // for now just call other dispatcher and return Todo: remove the rest of this code
            return PluginsContainer.DispatchAPI(command, stream, session);

        }

        // The Managed APP interface callback delegate
        public delegate void APPCallback(string args, ManagedSession session);
        // The Managed APP callback registry
        // TODO: value type subject to change to a more complex type including an APPAttribute configuration
        private static ConcurrentDictionary<string, APPCallback> sAPPRegistry = new ConcurrentDictionary<string, APPCallback>();

        // This is the FreeSWITCH APP interface callback handler which is bound to "coreclr" APP commands
        private static void NativeAPPHandler(IntPtr sessionptr, string data)
        {
            using ManagedSession session = new ManagedSession(new SWIGTYPE_p_switch_core_session_t(sessionptr, false));

            PluginsContainer.DispatchDialPlanApp(data, session);
            return;

        }

        // The Managed XML interface callback delegate
        public delegate void XMLCallback(string section, string tag, string key, string value, Event evt, ref string result);

        // public static event XMLCallback OnXMLSearch;

        // This is the FreeSWITCH XML interface callback handler
        private static string NativeXMLHandler(string section, string tag, string key, string value, IntPtr eventptr)
        {
            using Event evt = new Event(new SWIGTYPE_p_switch_event_t(eventptr, false), 0);
            //Log.WriteLine(LogLevel.Info, "Managed XML Handler: {0} - {1} - {2} - {3}", section, tag, key, value);
            return PluginsContainer.DispatchXMLCallback(section, tag, key, value, evt);
        }
    }
}
