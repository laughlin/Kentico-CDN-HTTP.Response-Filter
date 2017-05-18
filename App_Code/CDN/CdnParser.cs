using System;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

/// <summary>
/// Taken from https://devnet.kentico.com/marketplace/utilities/kentico-cdn-injector
/// and http://stackoverflow.com/questions/6061286/looking-for-a-httphandler-to-modify-pages-on-the-fly-to-point-to-a-cdn
/// and http://stackoverflow.com/questions/4464983/how-do-i-implement-an-http-response-filter-to-operate-on-the-entire-content-at-o
/// </summary>
public class CdnParser
{
    public string CdnXPathHtmlToReplace = ConfigurationManager.AppSettings["CdnXPathHtmlToReplace"];
    private readonly bool _useSsl;
    private readonly string _smallDomainName;
    private readonly string _largeDomainName;
    private readonly string[] _smallFileExtensions;
    private readonly string[] _largeFileExtensions;
    private readonly string[] _matchFilePaths;
    private readonly string[] _excludeFilePaths;
    private readonly bool _doFilePathMatching;
    private readonly bool _enableSmallCdn;
    private readonly bool _enableLargeCdn;
    private readonly Uri _currentUrl;

    public CdnParser(bool useSsl, string smallDomainName, string largeDomainName, string[] smallFileExtensions, string[] largeFileExtensions, bool doFilePathMatching, string[] matchFilePaths, string[] excludeFilePaths, Uri currentUrl)
    {
        _useSsl = useSsl;
        _smallDomainName = smallDomainName;
        _largeDomainName = largeDomainName;
        _smallFileExtensions = smallFileExtensions;
        _largeFileExtensions = largeFileExtensions;
        _doFilePathMatching = doFilePathMatching;
        _matchFilePaths = matchFilePaths;
        _excludeFilePaths = excludeFilePaths;
        _currentUrl = currentUrl;
        _enableSmallCdn = !string.IsNullOrEmpty(smallDomainName);
        _enableLargeCdn = !string.IsNullOrEmpty(largeDomainName);

    }
    public string ReplaceHtml(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);
        if(!string.IsNullOrEmpty(CdnXPathHtmlToReplace))
        { 
            var collection = document.DocumentNode.SelectNodes(CdnXPathHtmlToReplace);
            foreach (var element in collection)
            {
                switch (element.Name)
                {
                    case "script":
                        HandleAttribute(element, "src");
                        break;
                    case "img":
                        HandleAttribute(element, "src");
                        HandleDataAttributes(element);
                        break;
                    case "link":
                        HandleAttribute(element, "href");
                        break;
                    case "meta":
                        HandleAttribute(element, "content");
                        break;
                    default:
                        HandleDataAttributes(element);
                        break;
                }
                HandleStyleAttribute(element);
            }
        }
        return document.DocumentNode.OuterHtml;
    }

    private void HandleAttribute(HtmlNode node, string attribute)
    {
        var src = node.GetAttributeValue(attribute, string.Empty);
        if (!string.IsNullOrEmpty(src))
            node.Attributes[attribute].Value = GetUrl(src);
    }

    private void HandleDataAttributes(HtmlNode node)
    {
        if (node.HasAttributes && node.Attributes.Count(x => x.Name.ToLower().Contains("data-")) > 0)
        {
            var dataAttributes = node.Attributes.Where(x => x.Name.ToLower().Contains("data-"));
            foreach (var dataAttribute in dataAttributes)
            {
                if (!string.IsNullOrEmpty(dataAttribute.Value))
                    node.Attributes[dataAttribute.Name].Value = GetUrl(dataAttribute.Value);
            }
        }
    }

    private void HandleStyleAttribute(HtmlNode node)
    {
        if (node.HasAttributes && node.Attributes.Count(x => x.Name.ToLower().Equals("style")) > 0)
        {
            var styleAttribute = node.Attributes.Single(x => x.Name.ToLower().Equals("style"));
            if (!string.IsNullOrEmpty(styleAttribute.Value))
            {
                var regex = new Regex(@"background(-image)?:.*?url\('\s*(?<url>.*?)\s*'\)");
                if (regex.IsMatch(styleAttribute.Value))
                {
                    var match = regex.Match(styleAttribute.Value);
                    if (match.Groups.Count == 3)
                    {
                        var url = match.Groups[2].Value;
                        node.Attributes[styleAttribute.Name].Value = styleAttribute.Value.Replace(url, GetUrl(url));
                    }
                }
            }

        }
    }

    private string GetUrl(string path)
    {
        if (!_doFilePathMatching)
            return path;
        var continueFlag = false;
        if (_excludeFilePaths != null)
        {
            if (_excludeFilePaths.Any(filePath => path.ToLower().Contains(filePath.ToLower())))
                return path;
        }
        foreach (var filePath in _matchFilePaths)
        {
            if (path.ToLower().Contains(filePath.ToLower()))
                continueFlag = true;
        }
        if (!continueFlag)
            return path;
        continueFlag = false;
        var useSmallCdn = false;
        var useLargeCdn = false;
        foreach (var fileExtension in _smallFileExtensions)
        {
            if (path.ToLower().Contains(fileExtension.ToLower()))
            {
                continueFlag = true;
                useSmallCdn = _enableSmallCdn;
            }
        }
        if (!useSmallCdn)
        {
            foreach (var fileExtension in _largeFileExtensions)
            {
                if (path.ToLower().Contains(fileExtension.ToLower()))
                {
                    continueFlag = true;
                    useLargeCdn = _enableLargeCdn;
                }
            }
            if (!continueFlag || !useLargeCdn)
                return path;
        }
        if (path.Contains("https://") || path.Contains("http://"))
        {
            var uri = new Uri(path);
            if (uri.Host.Equals(_currentUrl.Host)) //request is fully qualified, still use CDN
            {
                path = uri.AbsolutePath;
            }
            else
            {
                return path;
            }
        }
        var scheme = !_useSsl ? "http://" : "https://";
        var trailingSlash = "/";
        if (path[0] == 47)
            trailingSlash = string.Empty;
        if (path[0] == 126)
            path = path.TrimStart('~');
        string urlRewrite;
        if (useSmallCdn)
            urlRewrite = scheme + _smallDomainName + trailingSlash + path;
        else urlRewrite = scheme + _largeDomainName + trailingSlash + path;
        return urlRewrite;
    }
}