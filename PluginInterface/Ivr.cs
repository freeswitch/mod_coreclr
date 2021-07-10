using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FreeSWITCH;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace FreeSWITCH.Helpers
{

    // Enum of prompt types 
    public enum PromptType
    {
        File,
        NumberPronounced,
        NumberIterated
    }

    // Class to wrap prompt info
    public class Prompt
    {
        public static String BaseFileDir;   // Base location for wave files

        public static Prompt NumberPrononced(Object theNumber)
        {
            return new Prompt(PromptType.NumberPronounced, theNumber);
        }

        public static Prompt NumberIterated(Object theNumber)
        {
            return new Prompt(PromptType.NumberIterated, theNumber);
        }

        public static Prompt PromptFile(String fname)
        {
            return new Prompt(PromptType.File, fname);
        }

        public PromptType Type { get; set; }
        public Object Value { get; set; }

        public Prompt()
        { }

        public Prompt(PromptType pt, Object value)
        {
            Type = pt;
            Value = value;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case PromptType.File:
                    return String.Format("{0}/{1}.wav", BaseFileDir, Value); // return the path

                case PromptType.NumberIterated:
                case PromptType.NumberPronounced:
                    {
                        var way = Type == PromptType.NumberIterated ? "iterated" : "pronounced";
                        var a = new Api(null);
                        var flist = a.ExecuteString(String.Format(
                            "say_string en.wav en number {0} {1}", way, Value))
                            .Substring(14);
                        return flist;
                    }
                    break;

                default:
                    return String.Empty;
            }
        }
    }

    // Class to contain a collection of prompts
    public class PromptList : List<Prompt>
    {
        public void AddPronouncedNumber(Object theNumber)
        {
            this.Add(Prompt.NumberPrononced(theNumber));
        }

        public void AddIteratedNumber(Object theNumber)
        {
            this.Add(Prompt.NumberIterated(theNumber));
        }

        public void AddFile(String fname)
        {
            this.Add(Prompt.PromptFile(fname));
        }

        public override string ToString()
        {
            var ret1 = String.Join("!", this.Select(e => e.ToString())
                                            .AsEnumerable());
            return "file_string://" + ret1;
        }
    }

    // Helper class to make ivr handling on a session work more like Dialogic and other IVR systems
    public class Ivr
    {
        // Beeded for new method to handle DTMF Input
        private delegate string InputCallback(string dtmf);

        // Instance members
        private ManagedSession _ses;
        private String _buf = String.Empty; // digit buffer
        private Boolean _captureDtmf = false;
        private Boolean _breakDtmf = false; // break on dtmf
        private String _regExMatch;  // used to match regex of current input
        private Char? _delKey = null; // current delete key for dtmf input
        private Char? _termKey = null; // current end of line char for dtmf input
        private int _dtmfTimeout; // dtmf timeout in MS for first digit
        private int _dtmfInterDigit; // dtmf interdigit timeout
        private int _minDigits; // Minimum digits for input
        private int _maxDigits; // Maximum digits for input
        private Object _lock = new Object();
        private Func<char, TimeSpan, String> oldDtmfCallback;

        private void TermOn()
        {
            this["dtmf_terminators"] = "*#0123456789";
        }

        private void TermOff()
        {
            this["dtmf_terminators"] = "none";
        }

        private String GetDtmfMatch()
        {
            String rv = null;
            if (!_ses.Ready())
            {
                throw new IOException("Channel Hung up");
            }
            lock (_lock)
            {
                if (_buf.Length == 0)
                    return null; // we can never match if there is nothing in the buffer
                var chars = new List<Char>(_buf.ToCharArray());

                // first process any digits to kill in the buffer because of the delete key
                if (_delKey.HasValue && chars.Contains(_delKey.Value))
                {
                    var delindex = chars.IndexOf(_delKey.Value);  // get the index of it
                    chars.RemoveRange(0, delindex + 1);  // strip it off and everything before it
                }
                // next see if we have the termination character in the buffer
                if (_termKey.HasValue && chars.Contains(_termKey.Value) && (chars.IndexOf(_termKey.Value) < _maxDigits))
                {
                    var t1 = chars.GetRange(0, chars.IndexOf(_termKey.Value)); // get the range
                    chars.RemoveRange(0, chars.IndexOf(_termKey.Value) + 1); // get rid of the whole range including the terminator
                    rv = new string(t1.ToArray()); // get it back as a string
                }
                else if (chars.Count < _minDigits)
                {
                    return null;  // no need to modify anything so skip the code that does it
                }
                else if (chars.Count >= _maxDigits)
                {
                    var t1 = chars.GetRange(0, _maxDigits); // get the subset of them
                    chars.RemoveRange(0, _maxDigits);   // delete them from the main buffer
                    rv = new string(t1.ToArray());  // return this string
                }
                else if (!String.IsNullOrEmpty(_regExMatch) && Regex.IsMatch(new string(chars.ToArray()), _regExMatch))
                {
                    rv = new string(chars.ToArray());
                    chars.Clear();
                }
                _buf = chars.Count == 0 ? String.Empty : new string(chars.ToArray());
                return rv;
            }
        }

        private String ProcessDtmfEvent(string digit)
        {
            lock (_lock)
            {
                _captureDtmf = true;
                _buf += digit.Trim();
                //Log.WriteLine(LogLevel.Critical, "Processdtmf: {0} {1}", digit, _buf);
                if (_breakDtmf)
                    return "break";
                return "true";
            }
        }

        private InputCallback DtmfCalback = null;

        public Ivr(ManagedSession session)
        {
            _ses = session;
            _buf = String.Empty;
            //oldDtmfCallback = _ses.DtmfReceivedFunction;
            //_ses.DtmfReceivedFunction = ProcessDtmfEvent;
            DtmfCalback = new InputCallback(ProcessDtmfEvent);
            var cb = Marshal.GetFunctionPointerForDelegate<InputCallback>(DtmfCalback);
            session.setDTMFCallback(new SWIGTYPE_p_void(cb, false), string.Empty);

        }

        // Indexer for variables
        [IndexerName("Variables")]
        public String this[String index]
        {
            get
            {
                var rv = _ses.GetVariable(index);
                return (String.IsNullOrEmpty(rv) ? null : rv);
            }
            set
            {
                if (value != null)
                    _ses.SetVariable(index, value);
                else
                    _ses.Execute("unset", index);
            }
        }

        // Flush DTMF both us and fs
        public void FlushDtmf()
        {
            _ses.flushDigits();
            _buf = String.Empty;
        }

        public String PlayPrompt(String promptName,
            String invalid,
            int retries,
            int minDigits,
            int maxDigits,
            String regEx = null,
            Char? termKey = null,
            Char? delKey = null,
            int? timeout = null,
            int? interDigit = null)
        {
            FlushDtmf(); // flush out buffer
            for (var x = 0; x < retries; x++)
            {
                if (!_ses.Ready())
                    throw new IOException("Channel hung up");
                Play(promptName, true);  // play prompt
                var dig = GetDtmf(minDigits, maxDigits, regEx, termKey, delKey, timeout, interDigit);
                if (dig != null && (regEx == null || Regex.IsMatch(dig, regEx)))
                    return dig;
                FlushDtmf();
                Play(invalid, true);
            }
            return String.Empty;  // exceded max retry 
        }

        public String PlayPrompt(Prompt promptName,
            String invalid,
            int retries,
            int minDigits,
            int maxDigits,
            String regEx = null,
            Char? termKey = null,
            Char? delKey = null,
            int? timeout = null,
            int? interDigit = null)
        {
            FlushDtmf(); // flush out buffer
            for (var x = 0; x < retries; x++)
            {
                if (!_ses.Ready())
                    throw new IOException("Channel Hung UP");
                Play(promptName, true);  // play prompt
                var dig = GetDtmf(minDigits, maxDigits, regEx, termKey, delKey, timeout, interDigit);
                if (dig != null && (regEx == null || Regex.IsMatch(dig, regEx)))
                    return dig;
                FlushDtmf();
                Play(invalid, true);
            }
            return String.Empty;  // exceded max retry 
        }

        public String PlayPrompt(PromptList promptName,
            String invalid,
            int retries,
            int minDigits,
            int maxDigits,
            String regEx = null,
            Char? termKey = null,
            Char? delKey = null,
            int? timeout = null,
            int? interDigit = null)
        {
            FlushDtmf(); // flush out buffer
            for (var x = 0; x < retries; x++)
            {
                if (!_ses.Ready())
                    throw new IOException("Channel Hung UP");
                Play(promptName, true);  // play prompt
                var dig = GetDtmf(minDigits, maxDigits, regEx, termKey, delKey, timeout, interDigit);
                if (dig != null && (regEx == null || Regex.IsMatch(dig, regEx)))
                    return dig;
                FlushDtmf();
                Play(invalid, true);
            }
            return String.Empty;  // exceded max retry 
        }

        public String GetDtmf(int minDigits, int maxDigits, String regEx = null, Char? termKey = null, Char? delKey = null, int? timeOut = null, int? interDigit = null)
        {
            //_ses.DtmfReceivedFunction = ProcessDtmfEvent;
            TermOff();
            _minDigits = minDigits;
            _maxDigits = maxDigits;
            _delKey = delKey;
            _termKey = termKey;
            _regExMatch = regEx;
            _dtmfTimeout = timeOut.HasValue ? timeOut.Value : 10000; // Make default value 30 secs
            _dtmfInterDigit = interDigit.HasValue ? interDigit.Value : _dtmfTimeout;
            // use timeout as interdigit timeout
            String rv = null;
            _breakDtmf = true;
            try
            {
                do
                {
                    if (!_ses.Ready())
                        throw new IOException("Channel Hung up");
                    rv = GetDtmfMatch();
                    //Log.WriteLine(LogLevel.Critical, "GetDTMFMatch called returned {0}", rv);
                    if (rv != null)
                        return rv;
                    var sval = _buf.Length == 0 ? _dtmfTimeout : _dtmfInterDigit;
                    _captureDtmf = false;
                    TermOn();

                    if(!_ses.Ready())
                        throw new IOException("Channel Hung UP");

                    _ses.sleep(sval, 0);
                    TermOff();
                    if (!_captureDtmf)
                        return null;
                } while (true);
            }
            catch (IOException)
            {
                throw;
            }

            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Critical, "GetDTMF execption {0}", ex.Message);
                return null;
            }
            finally
            {
                _breakDtmf = false;
            }
        }

        private void _Play(String FileString, Boolean interuptable = false)
        {
            if(!_ses.Ready())
                throw new IOException("Channel Hung UP");

            if (interuptable && _buf.Length > 0)
                return;
            _breakDtmf = interuptable;
            _ses.StreamFile(FileString, 0);
            _breakDtmf = false;
        }

        public void Play(Prompt p, Boolean i = false)
        {
            _Play("file_string://" + p.ToString(), i);
        }

        public void Play(PromptList pl, Boolean i = false)
        {
            _Play(pl.ToString(), i);
        }

        public void Play(String promptName, Boolean i = false)
        {
            _Play(Prompt.PromptFile(promptName).ToString(), i);
        }

        public void Play(Object entity, PromptType pt, Boolean i = false)
        {
            _Play("file_string://" + new Prompt(pt, entity).ToString(), i);
        }

        public void PlayTone(String tone, Boolean i = false)
        {
            _Play("tone_stream://" + tone, i);
        }

        public void Shutdown()
        {
            //_ses.DtmfReceivedFunction = oldDtmfCallback;
        }

        public String GetUuid()
        {
            return _ses.GetUuid();
        }

        public void Record(String filename, int maxTime, int silThres = 20, int hits = 200)
        {
            var fn = Prompt.PromptFile(filename).ToString(); // get the full filename
            _breakDtmf = true; // always break on dtmf
            _ses.RecordFile(fn, maxTime, silThres, hits);
            _breakDtmf = false;
        }

        //public void Reset()
        //{
        //    _ses.DtmfReceivedFunction = ProcessDtmfEvent;
        //    TermOff();
        //}
    }
}
