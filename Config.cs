using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Staticar
{
    internal static class Config
    {
        public static string srcdir = null;
        public static string destdir = null;
        public static string statefile = "state.dat";
        public static string ftproot = null;
        public static string ftpuser = null;
        public static string ftppass = null;

        public static bool ToFTP = false;

        internal static void ReadFromFile()
        {
            var configtype = System.Reflection.Assembly.GetAssembly(typeof(Config)).GetModules()[0].GetTypes().First(t => t.Name == "Config");
            var invariantFormat = System.Globalization.CultureInfo.InvariantCulture;
            foreach (string key in ConfigurationManager.AppSettings.AllKeys)
            {
                var f = configtype.GetField(key, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (f == null) { continue; }
                var value = ConfigurationManager.AppSettings[key];
                object varvalue = null;
                if (f.FieldType == typeof(Int32)) { varvalue = Convert.ToInt32(value, invariantFormat); }
                if (f.FieldType == typeof(float)) { varvalue = (float)Convert.ToDouble(value, invariantFormat); }
                if (f.FieldType == typeof(bool)) { varvalue = Convert.ToBoolean(value, invariantFormat); }
                if (f.FieldType == typeof(string)) { varvalue = Convert.ToString(value); }
                if (varvalue == null) { continue; }
                f.SetValue(null, varvalue);
            }
        }
    }
}