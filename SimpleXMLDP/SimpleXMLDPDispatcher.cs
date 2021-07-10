using FreeSWITCH;
using FreeSWITCH.Helpers;
using PluginInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleXMLDP
{
    public class SimpleXMLDPDispatcher : IPluginDispatcher
    {
        private delegate string InputCallback(string dtmf);
        public int DispatchAPI(string args, Stream stream, ManagedSession session)
        {
            stream.Write($"Test module api dispatcher args: {args}\n\n");
            return 0;
        }

        public void DispatchDialPlan(string args, ManagedSession session)
        {
            var param = args.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            switch(param)
            {
                case "demo1":
                    session.Answer();
                    var cb = Marshal.GetFunctionPointerForDelegate<InputCallback>(dtmfCallback);
                    session.setDTMFCallback(new SWIGTYPE_p_void(cb, false), string.Empty);
                    session.sleep(2000, 0);
                    session.StreamFile("/testsounds/clrtest.wav", 0);
                    session.Hangup("NORMAL_CLEARING");
                    break;

                case "demo2":
                    session.Answer();
                    var i = new Ivr(session);
                    Prompt.BaseFileDir = "/testsounds";
                    i.FlushDtmf();
                    session.sleep(2000, 0);
                    i.Play("clrtest", true);
                    i.Play(new Prompt(PromptType.NumberIterated, 1234), true);
                    var res = i.GetDtmf(1, 1);
                    i.FlushDtmf();
                    i.Play(new Prompt(PromptType.NumberIterated, res), false);
                    session.Hangup("NORMAL_CLEARING");
                    break;
            }
        }

        private string dtmfCallback(string digit)
        {
            Log.WriteLine(LogLevel.Info, $"Received dtmf {digit}\n");
            return "break;";
        }

        public string DispatchXMLCallback(string section, string tag, string key, string value, Event evt)
        {
            if (section != "dialplan")
                return null;
            var context = evt.GetHeader("Hunt-Context"); // the context
            var destination = evt.GetHeader("Hunt-Destination-Number"); // the dialed number or "DID"
            var ani = evt.GetHeader("Hunt-ANI"); // The ANI/CallerID number

            Log.WriteLine(LogLevel.Console, $"SimpleXMLDP: lookup ctx = {context} dest = {destination} ani = {ani}");

            switch (destination)
            {
                case "1111":
                    var l = new List<string>();
                    l.Add("sleep,2000");
                    l.Add("ring_ready");
                    l.Add("sleep,6000");
                    l.Add("answer");
                    l.Add("sleep,5000");
                    l.Add("hangup,NORMAL_CLEARING");
                    return new FreeSWITCH.Helpers.fsDialPlanDocument(context, l).ToXMLString();

                case "2222":
                    l = new List<string>();
                    l.Add("sleep,2000");
                    l.Add("ring_ready");
                    l.Add("sleep,6000");
                    l.Add("dotnet,demo1");
                    return new FreeSWITCH.Helpers.fsDialPlanDocument(context, l).ToXMLString();

                case "3333":
                    l = new List<string>();
                    l.Add("sleep,2000");
                    l.Add("ring_ready");
                    l.Add("sleep,6000");
                    l.Add("dotnet,demo2");
                    return new FreeSWITCH.Helpers.fsDialPlanDocument(context, l).ToXMLString();

                default:
                    break;
            }

            return null;

        }

        public IEnumerable<string> GetApiNames()
        {
            return new[] { "test1", "test2" };
        }

        public IEnumerable<string> GetDPNames()
        {
            return new[] { "demo1", "demo2" };
        }

        public bool Onload()
        {
            Log.WriteLine(LogLevel.Console, "Plugin SimpleXMLDP loaded in OnLoad()");
            return true;
        }
    }
}
