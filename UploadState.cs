using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Staticar
{
    [Serializable]
    internal class UploadState
    {
        public List<string> UploadedDirs;
        public List<FileUploaded> UploadedFiles;

        public UploadState()
        {
            UploadedDirs = new List<string>();
            UploadedFiles = new List<FileUploaded>();
        }

        internal StateResult GetChanges(string root)
        {
            var alldirs = Directory.GetDirectories(Config.destdir, "*", SearchOption.AllDirectories);
            var allfiles = Directory.GetFiles(Config.destdir, "*", SearchOption.AllDirectories);

            var unrootedDirs = Unroot(root, alldirs);
            var unrootedFiles = Unroot(root, allfiles).Except(new string[] { Config.statefile }).ToArray();

            var newDirs = unrootedDirs.Except(UploadedDirs).ToArray();
            UploadedDirs.AddRange(newDirs);

            var changedFiles = new List<string>();
            foreach (var file in unrootedFiles)
            {
                var uploaded = UploadedFiles.FirstOrDefault(f => f.Path.Equals(file));
                string hash = GetHashString(File.ReadAllText(Path.Combine(root, file)));
                if (uploaded == null)
                {
                    changedFiles.Add(Path.Combine(root, file));
                    var up = new FileUploaded();
                    up.Path = file;
                    up.Hash = hash;
                    UploadedFiles.Add(up);
                }
                else
                {
                    if (uploaded.Hash != hash)
                    {
                        changedFiles.Add(Path.Combine(root, file));
                        uploaded.Hash = hash;
                    }
                }
            }

            var result = new StateResult();
            result.Directories = newDirs;
            result.Files = changedFiles.ToArray();
            return result;
        }

        private static string[] Unroot(string root, string[] paths)
        {
            //daj sve putanje bez roota
            var unrooted = paths.Select(p => Path.GetFullPath(p)).Select(p => p.Remove(0, root.Length) + Path.DirectorySeparatorChar).ToArray();
            return unrooted.Select(u => u.TrimEnd(Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar)).ToArray();
        }

        public static byte[] GetHash(string inputString)
        {
            HashAlgorithm algorithm = MD5.Create();  //or use SHA1.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
    }

    internal class StateResult
    {
        public string[] Directories { get; set; }

        public string[] Files { get; set; }
    }

    [Serializable]
    internal class FileUploaded
    {
        public string Path { get; set; }

        public string Hash { get; set; }
    }
}