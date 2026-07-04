using BepInEx.Logging;

namespace StatDisplay
{
    internal static class DinoLogger
    {
        private static ManualLogSource logger = Logger.CreateLogSource(EntryPoint.MODNAME);

        public static void Log(string format, params object[] args)
        {
            Log(string.Format(format, args));
        }

        public static void Log(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Message, str);
        }

        public static void Warning(string format, params object[] args)
        {
            Warning(string.Format(format, args));
        }

        public static void Warning(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Warning, str);
        }

        public static void Error(string format, params object[] args)
        {
            Error(string.Format(format, args));
        }

        public static void Error(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Error, str);
        }

        public static void Debug(string format, params object[] args)
        {
            Debug(string.Format(format, args));
        }

        public static void Debug(string str)
        {
            if (logger == null) return;

            logger.Log(LogLevel.Debug, str);
        }
    }

}