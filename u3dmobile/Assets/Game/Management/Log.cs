using System;
using System.Text;
using UnityEngine;

namespace U3DMobile
{
    public static class Log
    {
        public static void I(string format, params object[] parameters)
        {
            string message = Format(format, parameters);
            WriteInfo(message);
        }

        public static void Error(string format, params object[] parameters)
        {
            string message = Format(format, parameters);
            WriteError(message);
        }

        private static string Format(string format, object[] parameters)
        {
            try
            {
                return string.Format(format, parameters);
            }
            catch (Exception)
            {
                var builder = new StringBuilder();

                builder.Append("\"");
                builder.Append(format);
                builder.Append("\"");

                foreach (object item in parameters)
                {
                    builder.Append(", ");
                    builder.Append(item);
                }

                return builder.ToString();
            }
        }

        //NOTE: these methods are prepared for language bridge.
        //different languages will use different formatting specifiers.
        public static void WriteInfo (string m) { Debug.Log     (m); }
        public static void WriteError(string m) { Debug.LogError(m); }
    }
}
