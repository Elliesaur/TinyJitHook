using System;

namespace TinyJitHook
{
    public static class Logger
    {
        /// <summary>
        /// Whether or not to suppress info messages.
        /// </summary>
        public static bool SuppressInfo { get; set; }
        /// <summary>
        /// Whether or not to suppress warning messages.
        /// </summary>
        public static bool SuppressWarn { get; set; }
        /// <summary>
        /// Whether or not to suppress all logging.
        /// </summary>
        public static bool SuppressLogging { get; set; }

        /// <summary>
        /// Generic log to console with no color.
        /// </summary>
        /// <param name="t">The type (will print name of type) that is calling.</param>
        /// <param name="msg">The message to log.</param>
        public static void Log(Type t, string msg)
        {
            if (SuppressLogging)
            {
                return;
            }
            string data = $"[{(t == null ? "GLOBAL" : t.Name)}] {msg}";
            Console.WriteLine(data);
        }
        /// <summary>
        /// Log a success message (<see cref="ConsoleColor.Green"/>) to console.
        /// </summary>
        /// <param name="t">The type (will print name of type) that is calling.</param>
        /// <param name="msg">The message to log.</param>
        public static void LogSuccess(Type t, string msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Log(t, msg);
            Console.ResetColor();
        }
        /// <summary>
        /// Log a warning message (<see cref="ConsoleColor.Yellow"/>) to console.
        /// </summary>
        /// <param name="t">The type (will print name of type) that is calling.</param>
        /// <param name="msg">The message to log.</param>
        public static void LogWarn(Type t, string msg)
        {
            if (SuppressWarn)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Log(t, msg);
            Console.ResetColor();
        }
        /// <summary>
        /// Log an info message (<see cref="ConsoleColor.Cyan"/>) to console.
        /// </summary>
        /// <param name="t">The type (will print name of type) that is calling.</param>
        /// <param name="msg">The message to log.</param>
        public static void LogInfo(Type t, string msg)
        {
            if (SuppressInfo)
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Log(t, msg);
            Console.ResetColor();
        }
        /// <summary>
        /// Log an error message (<see cref="ConsoleColor.Red"/>) to console.
        /// </summary>
        /// <param name="t">The type (will print name of type) that is calling.</param>
        /// <param name="msg">The message to log.</param>
        public static void LogError(Type t, string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log(t, msg);
            Console.ResetColor();
        }

    }
}
