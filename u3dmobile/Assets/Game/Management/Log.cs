using System;
using System.Text;
using UnityEngine;

namespace U3DMobile
{
    public static class Log
    {
        public static void WriteInfo(string message)
        {
            Debug.Log(message);
        }

        public static void WriteError(string message)
        {
            Debug.LogError(message);
        }

        private static string Format(string format, object[] parameters)
        {
            try
            {
                return string.Format(format, parameters);
            }
            catch (Exception)
            {
                StringBuilder builder = new StringBuilder();

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
    }
}