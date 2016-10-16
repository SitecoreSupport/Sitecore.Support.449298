using System;

namespace Sitecore.Support
{
    public class Trace
    {
        public static void Info(string message)
        {
            Sitecore.Diagnostics.Log.Info(Contextualize(message), new object());
        }

        public static void Debug(string message)
        {
            Sitecore.Diagnostics.Log.Debug(Contextualize(message), new object());
        }
        public static void Warn(string message)
        {
            Sitecore.Diagnostics.Log.Warn(Contextualize(message), new object());
        }
        public static void Warn(string message, Exception exception)
        {
            Sitecore.Diagnostics.Log.Warn(Contextualize(message), exception, new object());
        }

        private static string Contextualize(string message)
        {
            return String.Format("[#449298]: {0}", message);
        }
    }

}