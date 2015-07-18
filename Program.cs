using System;
using System.Collections.Generic;

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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Config.ReadFromFile();
                if (args.Any(a => a.Trim().StartsWith("toftp", StringComparison.CurrentCultureIgnoreCase)))
                {
                    Config.ToFTP = true;
                }

                var gen = new Generator();
                gen.Generate();

                if (Config.ToFTP)
                {
                    var uploader = new Uploader();
                    uploader.Upload();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}