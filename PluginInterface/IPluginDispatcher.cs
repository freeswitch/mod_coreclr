using FreeSWITCH;
using System;
using System.Collections.Generic;
using System.Text;

namespace PluginInterface
{
    public interface IPluginDispatcher
    {
        bool Onload();

        void DispatchDialPlan(string args, ManagedSession session);

        int DispatchAPI(string args, Stream stream, ManagedSession session);

        string DispatchXMLCallback(string section, string tag, string key, string value, Event evt);

        IEnumerable<string> GetApiNames();

        IEnumerable<string> GetDPNames();
    }
}
