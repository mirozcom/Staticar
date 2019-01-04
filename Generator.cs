using HtmlAgilityPack;
using Markdig;
using Markdig.BadHeaders;
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
        internal void Generate()
        {
            var srcarticles = new List<ArticleData>();

            var allDirs = Directory.GetDirectories(Config.srcdir, "*", SearchOption.AllDirectories);
            var filesToCopy = new List<string>();

            foreach (var srcfile in Directory.GetFiles(Config.srcdir, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(srcfile).Equals(".txt", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!Config.TEST_ONLY || Path.GetFileNameWithoutExtension(srcfile).StartsWith("testdata"))
                    {
                        srcarticles.Add(ParseArticleData(srcfile));
                    }
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
                if (!article.Stub)
                {
                    //add article to index page (if not stub)
                    indexOfArticlesAsHtml.AppendLine(indexPrefix + article.HtmlContent);
                    indexPrefix = "<hr class='separator'/>";
                }
                var dirOnly = Path.GetDirectoryName(article.Path);
                var targetDir = dirOnly.Replace(Config.srcdir, Config.destdir);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
                var destArticlePath = Path.Combine(targetDir, article.Slug + ".html");
                var articleAsHtml = encloseInHtml(article, "template-article");
                File.WriteAllText(destArticlePath, articleAsHtml);
            }
            if (!Config.TEST_ONLY)
            {
                string destlistpath = Path.Combine(Config.destdir, "index.html");
                var indexContent = encloseInHtml(indexOfArticlesAsHtml.ToString(), "template-index");
                File.WriteAllText(destlistpath, indexContent);
            }
        }

        private ArticleData ParseArticleData(string srcfile)
        {
            //var testOutput = convertArticleToHtml(srcfile);

            var a = new ArticleData();
            a.Path = srcfile;
            a.Slug = Path.GetFileNameWithoutExtension(srcfile);
            a.Created = new DateTime[] { File.GetCreationTime(srcfile), File.GetLastWriteTime(srcfile) }.Min();
            ParseLineData(a);
            //convertToHtmlArticle(a);
            convertArticleToHtml(a);
            customLogic(a);
            return a;
        }

        private void customLogic(ArticleData a)
        {
            //extract H1 element content as a title
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(a.HtmlContent);
            var h1 = doc.DocumentNode.SelectSingleNode(".//h1");
            a.Title = h1.InnerText;

            //replace heading text with heading link
            h1.RemoveAllChildren();
            h1.AppendChild(HtmlNode.CreateNode(toLink(a.Title, a.Slug)));

            //add date under the title
            //sb.AppendLine("<div class='undertitle'>" + ToDateString(a.Created) + "</div>");            
            h1.ParentNode.InsertAfter(HtmlNode.CreateNode($"<div class='undertitle'>{ToDateString(a.Created)}</div>"),h1);
            h1.ParentNode.InsertAfter(HtmlNode.CreateNode($"\r\n"),h1);

            //if ul follows p that ends with :, mark it so there is not margin between
            var uls = doc.DocumentNode.SelectNodes(".//ul");
            if (uls != null)
            {
                foreach (var ul in uls)
                {
                    HtmlNode prevSibling = ul.PreviousSibling;
                    while (prevSibling != null)
                    {
                        if (prevSibling.Name == "p")
                        {
                            if (prevSibling.InnerText.EndsWith(":"))
                            {
                                prevSibling.Attributes.Add("class", "overlist");
                                break;
                            }
                        }
                        else if (prevSibling.NodeType == HtmlNodeType.Text)
                        {
                            prevSibling = prevSibling.PreviousSibling;
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            //add target=blank to all absolute links
            var links = doc.DocumentNode.SelectNodes(".//a");
            foreach(var link in links)
            {
                var href = link.GetAttributeValue("href", string.Empty);
                if (href.StartsWith("http"))
                {
                    link.SetAttributeValue("target", "_blank");
                }
            }


            a.HtmlContent = $"<article>\n{doc.DocumentNode.OuterHtml}\n</article>";
        }



        private void convertArticleToHtml(ArticleData a)
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Use<BadHeadersExtension>()
                .Build();
            a.HtmlContent = Markdown.ToHtml(a.SourceContent, pipeline);
        }

        private void ParseLineData(ArticleData adata)
        {
            var lines = File.ReadAllLines(adata.Path);

            var ret = new List<LineData>();
            //var hregex = @"^#+.+$";
            //var numberingRegex = @"^\d+\..*";
            //var bulletingRegex = @"^[\*\-].*";
            //var refRegex = @"^\[\d+?\].*";
            var paramRegex = @"^\-\-(\w*)[\s\:]*(.*)$";
            var stubKeyword = "stub";
            var timestampKeyword = "ts";

            bool olstarted = false;
            bool ulstarted = false;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];//.Trim();
                //if (line.Trim() == string.Empty)
                //{
                //    continue;
                //}

                var paramMatch = Regex.Match(line.Trim(), paramRegex);
                if (paramMatch.Success)
                {
                    if (paramMatch.Groups.Count > 1)
                    {
                        var val = paramMatch.Groups[1].Value.Trim();
                        if (val.Equals(stubKeyword, StringComparison.CurrentCultureIgnoreCase))
                        {
                            adata.Stub = true;
                        }
                        else if (val.Equals(timestampKeyword, StringComparison.CurrentCultureIgnoreCase))
                        {
                            if (paramMatch.Groups.Count > 2)
                            {
                                var tsString = paramMatch.Groups[2].Value.Trim();
                                DateTime timestamp;
                                var success = DateTime.TryParseExact(tsString, "d.M.yyyy", System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out timestamp);
                                if (!success)
                                {
                                    success = DateTime.TryParseExact(tsString, "d.M.yy", System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out timestamp);
                                }
                                if (!success)
                                {
                                    success = DateTime.TryParseExact(tsString, "yyyy-M-d", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out timestamp);
                                }
                                if (!success)
                                {
                                    success = DateTime.TryParseExact(tsString, "yy-M-d", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out timestamp);
                                }
                                if (success)
                                {
                                    adata.Created = timestamp;
                                }
                            }
                        }
                    }
                    continue;
                }
                //if (olstarted && !Regex.IsMatch(line, numberingRegex))
                //{
                //    olstarted = false;
                //    ret.Add(new LineData(LineType.OlClose));
                //}
                //if (ulstarted && !Regex.IsMatch(line, bulletingRegex))
                //{
                //    ulstarted = false;
                //    ret.Add(new LineData(LineType.UlClose));
                //}
                //if (Regex.IsMatch(line, hregex))
                //{
                //    var fullLen = line.Length;
                //    var trimmed = line.TrimStart(new char[] { '#' });
                //    var trimmedLen = trimmed.Length;
                //    LineType ltype;
                //    switch (fullLen - trimmedLen)
                //    {
                //        case 1: ltype = LineType.H1; break;
                //        case 2: ltype = LineType.H2; break;
                //        case 3: ltype = LineType.H3; break;
                //        default: ltype = LineType.H4; break;
                //    }
                //    ret.Add(new LineData(ltype, trimmed));
                //}
                //else if (Regex.IsMatch(line, numberingRegex))
                //{
                //    if (!olstarted)
                //    {
                //        var last = ret.Last();
                //        if (last.Type == LineType.Text && last.Text.EndsWith(":"))
                //        {
                //            last.Type = LineType.ListOver;
                //        }
                //        ret.Add(new LineData(LineType.OlOpen));
                //        olstarted = true;
                //    }
                //    var lionly = Regex.Replace(line, @"^\d+\.", string.Empty).Trim();
                //    ret.Add(new LineData(LineType.Li, lionly));
                //}
                //else if (Regex.IsMatch(line, bulletingRegex))
                //{
                //    if (!ulstarted)
                //    {
                //        var last = ret.Last();
                //        if (last.Type == LineType.Text && last.Text.EndsWith(":"))
                //        {
                //            last.Type = LineType.ListOver;
                //        }
                //        ret.Add(new LineData(LineType.UlOpen));
                //        ulstarted = true;
                //    }
                //    var lionly = Regex.Replace(line, @"^[\*\-]", string.Empty).Trim();
                //    ret.Add(new LineData(LineType.Li, lionly));
                //}
                //else if (Regex.IsMatch(line, refRegex))
                //{
                //    ret.Add(new LineData(LineType.Reference, line));
                //}
                else
                {
                    ret.Add(new LineData(LineType.Text, line));
                }
            }
            if (olstarted) { ret.Add(new LineData(LineType.OlClose)); }
            if (ulstarted) { ret.Add(new LineData(LineType.UlClose)); }

            adata.SourceContent = string.Join(Environment.NewLine, ret.Select(l => l.Text));

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

        //private void convertToHtmlArticle(ArticleData a)
        //{
        //    var reflines = a.Lines.Where(l => l.Type == LineType.Reference).ToArray();
        //    var sb = new StringBuilder();
        //    sb.AppendLine("<article>");

        //    for (int i = 0; i < a.Lines.Length; i++)
        //    {
        //        var line = a.Lines[i];
        //        string text = null;
        //        if (line.Text != null) { text = markify(line.Text, reflines); }
        //        switch (line.Type)
        //        {
        //            case LineType.H1:
        //                a.Title = text;
        //                var htags = string.Format("<h1>{0}</h1>", toLink(a.Title, a.Slug));
        //                sb.AppendLine(htags);
        //                sb.AppendLine("<div class='undertitle'>" + ToDateString(a.Created) + "</div>");
        //                break;

        //            case LineType.H2:
        //                sb.AppendLine(string.Format("<h2>{0}</h2>", text));
        //                break;
        //            case LineType.H3:
        //                sb.AppendLine(string.Format("<h3>{0}</h3>", text));
        //                break;
        //            case LineType.H4:
        //                sb.AppendLine(string.Format("<h4>{0}</h4>", text));
        //                break;
        //            case LineType.Li:
        //                sb.AppendLine(string.Format("<li>{0}</li>", text));
        //                break;

        //            case LineType.OlOpen:
        //                sb.AppendLine("<ol>");
        //                break;

        //            case LineType.OlClose:
        //                sb.AppendLine("</ol>");
        //                break;

        //            case LineType.UlOpen:
        //                sb.AppendLine("<ul>");
        //                break;

        //            case LineType.UlClose:
        //                sb.AppendLine("</ul>");
        //                break;

        //            case LineType.Text:
        //                sb.AppendLine(string.Format("<p>{0}</p>", text));
        //                break;

        //            case LineType.ListOver:
        //                sb.AppendLine(string.Format("<p class='overlist'>{0}</p>", text));
        //                break;

        //            case LineType.Reference:
        //                //ništa za sad
        //                break;

        //            default: throw new Exception("Unknown line type: " + line.Type);
        //        }
        //    }

        //    sb.AppendLine("</article>");
        //    a.Content = sb.ToString();
        //}

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
                var title = cap.Value.Substring(1, cap.Value.Length - refUp.Length - 2);
                var href = refDown.Text.Substring(refUp.Length + 2, refDown.Text.Length - refUp.Length - 3).Trim();
                var link = toLink(title, href, true);
                text = text.Replace(cap.Value, link);
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
            return template.Replace("{content}", a.HtmlContent);
        }

        private string encloseInHtml(string content, string templatename)
        {
            var template = File.ReadAllText(Path.Combine(Config.srcdir, templatename + ".html"));
            return template.Replace("{content}", content);
        }
    }
}