using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeSWITCH
{
    public static class Log
    {
        public static void Write(LogLevel level, string message)
        {
            Native.console_log(level.ToLogString(), message);
        }
        public static void Write(LogLevel level, string format, params object[] args)
        {
            Native.console_log(level.ToLogString(), string.Format(format, args));
        }
        public static void WriteLine(LogLevel level, string message)
        {
            Native.console_log(level.ToLogString(), message + Environment.NewLine);
        }
        public static void WriteLine(LogLevel level, string format, params object[] args)
        {
            Native.console_log(level.ToLogString(), string.Format(format, args) + Environment.NewLine);
        }

        public static string ToLogString(this LogLevel level)
        {
            switch (level) {
                case LogLevel.Console: return "CONSOLE";
                case LogLevel.Alert: return "ALERT";
                case LogLevel.Critical: return "CRIT";
                case LogLevel.Debug: return "DEBUG";
                case LogLevel.Error: return "ERR";
                case LogLevel.Info: return "INFO";
                case LogLevel.Notice: return "NOTICE";
                case LogLevel.Warning: return "WARNING";
                default:
                    System.Diagnostics.Debug.Fail("Invalid LogLevel: " + level.ToString() + " (" + (int)level+ ").");
                    return "INFO";
            }
        }
    }

    public enum LogLevel
    {
        Console,
        Debug,
        Info,
        Error,
        Critical,
        Alert,
        Warning,
        Notice,
    }
}
