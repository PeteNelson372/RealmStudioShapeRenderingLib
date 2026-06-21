using log4net;

namespace RealmStudioShapeRenderingLib.Logging
{
    public static class RealmStudioXLogger
    {
        private static readonly ILog Log = LogManager.GetLogger("RealmStudioX");

        public static void Info(string message)
        {
            Log.Info(message);
        }

        public static void Warning(string message)
        {
            Log.Warn(message);
        }

        public static void Error(string message)
        {
            Log.Error(message);
        }

        public static void Exception(string message, Exception ex)
        {
            Log.Error($"{message}", ex);
        }
    }
}
