using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows.Forms;

namespace Staticar
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            //System.IO.File.SetCreationTime(@"C:\dev\miroz.com.hr\static-src\project_economix_1.txt", new DateTime(2014, 12, 11, 18, 2, 3));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());
            try
            {
                if (args.Any(a => a.Trim().StartsWith("toftp", StringComparison.CurrentCultureIgnoreCase)))
                {
                    Config.ToFTP = true;
                }

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

                var gen = new Generator();
                gen.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}