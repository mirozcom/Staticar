using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Staticar
{
    internal class Generator
    {
        internal void Start()
        {
            var srcarticles = new List<ArticleData>();

            var allDirs = Directory.GetDirectories(Config.srcdir, "*", SearchOption.AllDirectories);
            var filesToCopy = new List<string>();

            foreach (var srcfile in Directory.GetFiles(Config.srcdir, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(srcfile).Equals(".txt", StringComparison.CurrentCultureIgnoreCase))
                {
                    srcarticles.Add(ParseArticleData(srcfile));
                }
                else
                {
                    filesToCopy.Add(srcfile);
                }
            }

            foreach (var file in filesToCopy)
            {
                onlyCopy(file);
            }

            srcarticles = srcarticles.OrderByDescending(f => f.Created).ToList();

            var indexOfArticlesAsHtml = new StringBuilder();
            string indexPrefix = string.Empty;
            foreach (var article in srcarticles)
            {
                indexOfArticlesAsHtml.AppendLine(indexPrefix + article.Content);
                indexPrefix = "<hr class='separator'/>";

                var destArticlePath = Path.Combine(Config.destdir, article.Slug + ".html");
                var articleAsHtml = encloseInHtml(article, "template-article");
                File.WriteAllText(destArticlePath, articleAsHtml);
            }
            string destlistpath = Path.Combine(Config.destdir, "index.html");
            var indexContent = encloseInHtml(indexOfArticlesAsHtml.ToString(), "template-index");
            File.WriteAllText(destlistpath, indexContent);

            if (Config.ToFTP)
            {
                copyToFtp();
            }
        }

        private ArticleData ParseArticleData(string srcfile)
        {
            var a = new ArticleData();
            a.Path = srcfile;
            a.Slug = Path.GetFileNameWithoutExtension(srcfile);
            a.Created = File.GetCreationTime(srcfile);
            a.Lines = File.ReadAllLines(srcfile);
            a.Content = convertToHtmlArticle(a);
            return a;
        }

        private void copyToFtp()
        {
            var ftp = new FTP(Config.ftproot, Config.ftpuser, Config.ftppass);
            foreach (var destdir in Directory.GetDirectories(Config.destdir, "*", SearchOption.AllDirectories))
            {
                var ftpdir = destdir.Replace(Config.destdir, "").Trim('\\').Replace("\\", "/");
                ftp.createDirectory(ftpdir);
            }
            foreach (var destfile in Directory.GetFiles(Config.destdir, "*", SearchOption.AllDirectories))
            {
                var ftppath = destfile.Replace(Config.destdir, "").Trim('\\').Replace("\\", "/");
                ftp.Upload(destfile, ftppath);
            }
        }

        private void onlyCopy(string srcpath)
        {
            var destination = srcpath.Replace(Config.srcdir, Config.destdir);
            var destdir = Path.GetDirectoryName(destination);
            if (!Directory.Exists(destdir))
            {
                Directory.CreateDirectory(destdir);
            }
            File.Copy(srcpath, destination, true);
        }

        private string convertToHtmlArticle(ArticleData a)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<article>");
            var h1regex = @"^#{1}.+$";
            var hregex = @"^#+.+$";
            var numberingRegex = @"^\d+\..*";
            var bulletingRegex = @"^[\*\-].*";
            bool olstarted = false;
            bool ulstarted = false;
            foreach (var line in a.Lines)
            {
                if (line.Trim() == string.Empty)
                {
                    continue;
                }
                if (olstarted && !Regex.IsMatch(line, numberingRegex))
                {
                    olstarted = false;
                    sb.AppendLine("</ol>");
                }
                if (ulstarted && !Regex.IsMatch(line, bulletingRegex))
                {
                    ulstarted = false;
                    sb.AppendLine("</ul>");
                }
                if (Regex.IsMatch(line, h1regex))
                {
                    a.Title = line.Substring(1);
                    var htags = string.Format("<h1><a href='{1}'>{0}</a></h1>", a.Title, a.Slug);
                    sb.AppendLine(htags);
                    sb.AppendLine("<div class='undertitle'>" + ToDateString(a.Created) + "</div>");
                }
                else if (Regex.IsMatch(line, hregex))
                {
                    var htags = hashedToHTags(line);
                    sb.AppendLine(htags);
                }
                else if (Regex.IsMatch(line, numberingRegex))
                {
                    if (!olstarted)
                    {
                        sb.AppendLine("<ol>");
                        olstarted = true;
                    }
                    var lionly = Regex.Replace(line, @"^\d+\.", string.Empty).Trim();
                    sb.AppendLine(string.Format("<li>{0}</li>", lionly));
                }
                else if (Regex.IsMatch(line, bulletingRegex))
                {
                    if (!ulstarted)
                    {
                        sb.AppendLine("<ul>");
                        ulstarted = true;
                    }
                    var lionly = Regex.Replace(line, @"^[\*\-]", string.Empty).Trim();
                    sb.AppendLine(string.Format("<li>{0}</li>", lionly));
                }
                else
                {
                    sb.AppendLine(string.Format("<p>{0}</p>", line));
                }
            }
            if (olstarted) { sb.AppendLine("</ol>"); }
            if (ulstarted) { sb.AppendLine("</ul>"); }
            sb.AppendLine("</article>");
            return sb.ToString();
        }

        private string ToDateString(DateTime dateTime)
        {
            return dateTime.ToString("dd.MM.yyyy");
        }

        private string hashedToHTags(string line)
        {
            int index = 0;
            while (line[index] == '#')
            {
                index++;
            }
            return string.Format("<h{0}>{1}</h{0}>", index, line.Substring(index));
        }

        private string encloseInHtml(ArticleData a, string templatename)
        {
            var template = File.ReadAllText(Path.Combine(Config.srcdir, templatename + ".html"));
            template = template.Replace("{title}", a.Title);
            return template.Replace("{content}", a.Content);
        }

        private string encloseInHtml(string content, string templatename)
        {
            var template = File.ReadAllText(Path.Combine(Config.srcdir, templatename + ".html"));
            return template.Replace("{content}", content);
        }
    }
}