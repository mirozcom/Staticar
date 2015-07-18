using System;
using System.Collections.Generic;
using System.Linq;

namespace Staticar
{
    internal static class Config
    {
        public static string srcdir;
        public static string destdir;
        public static string statefile = "state.dat";
        public static string ftproot;
        public static string ftpuser;
        public static string ftppass;

        public static bool ToFTP = true;
    }
}