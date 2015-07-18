using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staticar
{
    internal class Uploader
    {
        internal void Upload()
        {
            copyToFtp();
        }

        private void copyToFtp()
        {
            UploadState uploaded = null;
            var stateFile = Path.Combine(Config.destdir, Config.statefile);
            if (File.Exists(stateFile))
            {
                try
                {
                    uploaded = Util.Deserialize(stateFile) as UploadState;
                }
                catch { }
            }
            if (uploaded == null)
            {
                uploaded = new UploadState();
            }

            var changes = uploaded.GetChanges(Config.destdir);

            var ftp = new FTP(Config.ftproot, Config.ftpuser, Config.ftppass);
            foreach (var destdir in changes.Directories)
            {
                var ftpdir = destdir.Replace(Config.destdir, "").Trim('\\').Replace("\\", "/");
                try
                {
                    ftp.createDirectory(ftpdir);
                }
                catch
                {
                    //ne brinemo ako to ne prođe
                }
            }
            foreach (var destfile in changes.Files)
            {
                var ftppath = destfile.Replace(Config.destdir, "").Trim('\\').Replace("\\", "/");
                ftp.Upload(destfile, ftppath);
            }
            Util.Serialize(uploaded, stateFile);
        }
    }
}