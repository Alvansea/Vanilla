using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;

namespace Vanilla.Utility
{
    public class HtmlCrawler
    {
        #region Crawler

        public const string UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";

        public static WebClient GetDefaultClient()
        {
            WebClient client = new WebClient();
            client.Proxy = WebRequest.DefaultWebProxy;
            //client.Proxy = WebProxy.GetDefaultProxy();
            client.Encoding = System.Text.Encoding.UTF8;
            client.Headers.Set(HttpRequestHeader.UserAgent, UserAgent);
            return client;
        }

        public static byte[] DownloadData(string url)
        {
            WebClient client = GetDefaultClient();
            try
            {
                return client.DownloadData(url);
            }
            catch
            {
                return new byte[0];
            }
        }

        public static string DownloadPage(string url)
        {
            string pattern = "content=\"text/html;\\s?charset=(?<encoding>[^\"]+)\"";

            byte[] data = DownloadData(url);
            if (data.Length > 0)
            {
                string unicode = Encoding.UTF8.GetString(data);
                Match match = Regex.Match(unicode, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (match.Success)
                {
                    return Encoding.GetEncoding(match.Groups["encoding"].Value).GetString(data);
                }
                else
                {
                    return unicode;
                }
            }
            return string.Empty;
        }

        #endregion

        #region Html Utilities

        public static HyperLink[] ExtractLinks(string html, string baseUrl)
        {
            string pattern = "<a[^>]*>(?<text>[^<]*)</a>";
            Uri baseUri = new Uri(baseUrl);

            MatchCollection matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            List<HyperLink> links = new List<HyperLink>();

            foreach (Match match in matches)
            {
                HyperLink link = new HyperLink();
                link.Text = match.Groups["text"].Value;

                Match m = Regex.Match(match.Value, "href=\"(?<url>[^\"]+)\"");
                if (m.Success)
                {
                    string url = m.Groups["url"].Value;
                    url = GetRelativeUrl(url, baseUrl);

                    link.NavigateUrl = url;
                }

                if (!string.IsNullOrEmpty(link.NavigateUrl))
                {
                    links.Add(link);
                }
            }

            return links.ToArray();
        }

        public static string GetRelativeUrl(string url, string baseUrl)
        {
            if (url.StartsWith("?"))
            {
                if (baseUrl.IndexOf("?") < 0)
                {
                    return baseUrl + url;
                }
                else
                {
                    return Regex.Replace(baseUrl, "\\?.*", url);
                }
            }
            else if (url.StartsWith("#"))
            {
                if (baseUrl.IndexOf("#") < 0)
                {
                    return baseUrl + url;
                }
                else
                {
                    return Regex.Replace(baseUrl, "\\#.*", url);
                }
            }
            else
            {
                try
                {
                    Uri uri = new Uri(new Uri(baseUrl), url);
                    return uri.ToString();
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public static string GetDefaultPattern()
        {
            string tag = @"(?:[\w-:]+)";
            string attribute = @"(?:[\w-:]+)(?:=(?:[^\s\>\<]*|\""[\s\S]*?\""|\'[\s\S]*?\'))?";
            string name = @"(?:[\w-:]+)";
            string argument = @"(?:[\w-:]+|\""[\s\S]*?\""|\'[\s\S]*?\')";

            string beginningTag = @"(?:\<" + tag + @"(?:\s+" + attribute + @")*\s*(?:/)?\>)";
            string endingTag = @"(?:\</" + tag + @"\>)";
            string xmlComment = @"(?:\<!--[\s\S]*?--\>)";
            string xmlDirective = @"(?:\<!" + name + @"(?:\s+" + argument + @")*\s*\>)";
            string xmlCData = @"(?:\<!\[CDATA\[(?:[\s\S]*?)\]\]\>)";
            string styleBlock = @"(?:(?:\<(?:Style)(?:\s+" + attribute + @")*\s*(?:/)?\>)(?:[\s\S]*?)(?:\</(?:Style)\>))";
            string scriptBlock = @"(?:(?:\<(?:script)(?:\s+" + attribute + @")*\s*(?:/)?\>)(?:[\s\S]*?)(?:\</(?:script)\>))";
            string xmlLiteral = @"(?:(?:(?<blank>[ ]+)|[^ \<\>])+)";

            string pattern = styleBlock + "|" + scriptBlock + "|" + xmlDirective + "|" + xmlComment + "|" + beginningTag + "|" + endingTag + "|" + xmlCData + "|" + xmlLiteral;

            return pattern;
        }

        public static Regex GetDefaultRegex()
        {
            Regex reg = new Regex(GetDefaultPattern(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return reg;
        }

        public static Regex GetSelectionRegex(string[] holdTags)
        {
            // <(?!((/?\s?li)|(/?\s?ul)|(/?\s?a)|(/?\s?img)|(/?\s?br)|(/?\s?span)|(/?\s?b)))[^>]+>
            string regStr = string.Format(@"<(?!((/?\s?{0})))[^>]+>", string.Join(@")|(/?\s?", holdTags));
            Regex reg = new Regex(regStr, RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
            return reg;
        }

        #endregion
    }
}
