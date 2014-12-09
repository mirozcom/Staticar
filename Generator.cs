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
            a.Lines = ParseLineData(File.ReadAllLines(srcfile));
            a.Content = convertToHtmlArticle(a);
            return a;
        }

        private LineData[] ParseLineData(string[] lines)
        {
            var ret = new List<LineData>();
            var h1regex = @"^#{1}.+$";
            var hregex = @"^#+.+$";
            var numberingRegex = @"^\d+\..*";
            var bulletingRegex = @"^[\*\-].*";
            var refRegex = @"^\[\d+?\].*";
            bool olstarted = false;
            bool ulstarted = false;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Trim() == string.Empty)
                {
                    continue;
                }
                if (olstarted && !Regex.IsMatch(line, numberingRegex))
                {
                    olstarted = false;
                    ret.Add(new LineData(LineType.OlClose));
                }
                if (ulstarted && !Regex.IsMatch(line, bulletingRegex))
                {
                    ulstarted = false;
                    ret.Add(new LineData(LineType.UlClose));
                }
                if (Regex.IsMatch(line, h1regex))
                {
                    ret.Add(new LineData(LineType.H1, line.Substring(1)));
                }
                else if (Regex.IsMatch(line, hregex))
                {
                    ret.Add(new LineData(LineType.H2, line.TrimStart('#')));
                }
                else if (Regex.IsMatch(line, numberingRegex))
                {
                    if (!olstarted)
                    {
                        var last = ret.Last();
                        if (last.Type == LineType.Text && last.Text.EndsWith(":"))
                        {
                            last.Type = LineType.ListOver;
                        }
                        ret.Add(new LineData(LineType.OlOpen));
                        olstarted = true;
                    }
                    var lionly = Regex.Replace(line, @"^\d+\.", string.Empty).Trim();
                    ret.Add(new LineData(LineType.Li, lionly));
                }
                else if (Regex.IsMatch(line, bulletingRegex))
                {
                    if (!ulstarted)
                    {
                        var last = ret.Last();
                        if (last.Type == LineType.Text && last.Text.EndsWith(":"))
                        {
                            last.Type = LineType.ListOver;
                        }
                        ret.Add(new LineData(LineType.UlOpen));
                        ulstarted = true;
                    }
                    var lionly = Regex.Replace(line, @"^[\*\-]", string.Empty).Trim();
                    ret.Add(new LineData(LineType.Li, lionly));
                }
                else if (Regex.IsMatch(line, refRegex))
                {
                    ret.Add(new LineData(LineType.Reference, line));
                }
                else
                {
                    ret.Add(new LineData(LineType.Text, line));
                }
            }
            if (olstarted) { ret.Add(new LineData(LineType.OlClose)); }
            if (ulstarted) { ret.Add(new LineData(LineType.UlClose)); }
            return ret.ToArray();
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
            var reflines = a.Lines.Where(l => l.Type == LineType.Reference).ToArray();
            var sb = new StringBuilder();
            sb.AppendLine("<article>");

            for (int i = 0; i < a.Lines.Length; i++)
            {
                var line = a.Lines[i];
                string text = null;
                if (line.Text != null) { text = markify(line.Text, reflines); }
                switch (line.Type)
                {
                    case LineType.H1:
                        a.Title = text;
                        var htags = string.Format("<h1>{0}</h1>", toLink(a.Title, a.Slug));
                        sb.AppendLine(htags);
                        sb.AppendLine("<div class='undertitle'>" + ToDateString(a.Created) + "</div>");
                        break;

                    case LineType.H2:
                        sb.AppendLine(string.Format("<h2>{0}</h2>", text));
                        break;

                    case LineType.Li:
                        sb.AppendLine(string.Format("<li>{0}</li>", text));
                        break;

                    case LineType.OlOpen:
                        sb.AppendLine("<ol>");
                        break;

                    case LineType.OlClose:
                        sb.AppendLine("</ol>");
                        break;

                    case LineType.UlOpen:
                        sb.AppendLine("<ul>");
                        break;

                    case LineType.UlClose:
                        sb.AppendLine("</ul>");
                        break;

                    case LineType.Text:
                        sb.AppendLine(string.Format("<p>{0}</p>", text));
                        break;

                    case LineType.ListOver:
                        sb.AppendLine(string.Format("<p class='overlist'>{0}</p>", text));
                        break;

                    case LineType.Reference:
                        //ništa za sad
                        break;

                    default: throw new Exception("Unknown line type: " + line.Type);
                }
            }

            sb.AppendLine("</article>");
            return sb.ToString();
        }

        private string markify(string text, LineData[] references)
        {
            string linkRegex = @"\[.+?\]\[.+?\]";
            string refRegex = @"\[.+?\]$";

            while (true)
            {
                var match = Regex.Match(text, linkRegex);
                if (match.Captures.Count == 0) { break; }
                if (match.Captures.Count > 1) { throw new Exception("HOW??"); }
                var cap = match.Captures[0];
                var refUp = Regex.Match(cap.Value, refRegex, RegexOptions.RightToLeft).Captures[0];
                var refDown = references.First(r => r.Text.StartsWith(refUp.Value, StringComparison.CurrentCultureIgnoreCase));
                text = text.Replace(cap.Value, toLink(cap.Value.Substring(1, cap.Value.Length - refUp.Length - 2), refDown.Text.Substring(refUp.Length + 2, refDown.Text.Length - refUp.Length - 3).Trim(), true));
            }
            return text;
        }

        private string toLink(string title, string href, bool newWindow = false)
        {
            return string.Format("<a href='{1}'{2}>{0}</a>", title, href, newWindow ? " target='{_blank}'" : string.Empty);
        }

        private string ToDateString(DateTime dateTime)
        {
            return dateTime.ToString("dd.MM.yyyy");
        }

        //private string hashedToHTags(string line)
        //{
        //    int index = 0;
        //    while (line[index] == '#')
        //    {
        //        index++;
        //    }
        //    return string.Format("<h{0}>{1}</h{0}>", index, line.Substring(index));
        //}

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