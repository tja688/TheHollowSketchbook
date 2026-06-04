using System;
using System.Collections.Generic;

namespace Game.Core.Logging
{
    public enum LogLevel
    {
        Trace,
        Info,
        Warning,
        Error
    }

    public sealed class GameException : Exception
    {
        public GameException(string message)
            : base(message)
        {
        }
    }

    public static class Log
    {
        private static readonly List<string> Entries = new List<string>();

        public static IReadOnlyList<string> Messages
        {
            get { return Entries; }
        }

        public static event Action<LogLevel, string> MessageLogged;

        public static void Clear()
        {
            Entries.Clear();
        }

        public static void Trace(string message)
        {
            Write(LogLevel.Trace, message);
        }

        public static void Info(string message)
        {
            Write(LogLevel.Info, message);
        }

        public static void Warning(string message)
        {
            Write(LogLevel.Warning, message);
        }

        public static void Error(string message)
        {
            Write(LogLevel.Error, message);
        }

        private static void Write(LogLevel level, string message)
        {
            string entry = "[" + level + "] " + message;
            Entries.Add(entry);
            MessageLogged?.Invoke(level, message);
        }
    }
}
