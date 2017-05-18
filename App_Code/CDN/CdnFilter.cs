using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;

/// <summary>
/// Taken from https://devnet.kentico.com/marketplace/utilities/kentico-cdn-injector
/// and http://stackoverflow.com/questions/6061286/looking-for-a-httphandler-to-modify-pages-on-the-fly-to-point-to-a-cdn
/// and http://stackoverflow.com/questions/4464983/how-do-i-implement-an-http-response-filter-to-operate-on-the-entire-content-at-o
/// </summary>
public class CdnFilter : Stream
{
    public string CdnSmallDomain = ConfigurationManager.AppSettings["CdnSmallDomain"];
    public string CdnLargeDomain = ConfigurationManager.AppSettings["CdnLargeDomain"];
    public string ForceSsl = ConfigurationManager.AppSettings["CdnForceSsl"];
    public string SmallFileExtensionsToMatch = ConfigurationManager.AppSettings["CdnSmallFileExtensionsToMatch"];
    public string LargeFileExtensionsToMatch = ConfigurationManager.AppSettings["CdnLargeFileExtensionsToMatch"];
    public string FilePathsToMatch = ConfigurationManager.AppSettings["CdnFilePathsToMatch"];
    public string FilePathsToIgnore = ConfigurationManager.AppSettings["CdnFilePathsToIgnore"];
    private readonly MemoryStream _cachedStream = new MemoryStream(1024);
    private readonly Stream _responseStream;
    private readonly CdnParser _cdnParser;
    private readonly bool _useSsl;
    private readonly bool _doCdnRewrite;
    private readonly string[] _smallFileExtensions;
    private readonly string[] _largeFileExtensions;
    private readonly string[] _matchFilePaths;
    private readonly string[] _excludeFilePaths;
    private readonly bool _doFilePathMatching;
    private bool _isClosing;
    private bool _isClosed;

    public override bool CanRead
    {
        get
        {
            return true;
        }
    }

    public override bool CanSeek
    {
        get
        {
            return true;
        }
    }

    public override bool CanWrite
    {
        get
        {
            return true;
        }
    }

    public override long Length
    {
        get
        {
            return 0L;
        }
    }

    public override long Position { get; set; }

    public CdnFilter(Stream inputStream, Uri currentUrl)
    {
        _responseStream = inputStream;
        var smallDomainName = string.IsNullOrWhiteSpace(CdnSmallDomain) ? string.Empty : CdnSmallDomain;
        var largeDomainName = string.IsNullOrWhiteSpace(CdnLargeDomain) ? string.Empty : CdnLargeDomain;
        if (!string.IsNullOrEmpty(smallDomainName) || !string.IsNullOrEmpty(largeDomainName))
            _doCdnRewrite = true;
        if (!string.IsNullOrWhiteSpace(FilePathsToMatch))
        {
            _matchFilePaths = FilePathsToMatch.Split(',');
            _doFilePathMatching = _matchFilePaths.Length > 0;
        }
        else
            _doCdnRewrite = false;
        if (!string.IsNullOrWhiteSpace(SmallFileExtensionsToMatch))
        {
            _smallFileExtensions = SmallFileExtensionsToMatch.Split(',');
        }
        if (!string.IsNullOrWhiteSpace(LargeFileExtensionsToMatch))
        {
            _largeFileExtensions = LargeFileExtensionsToMatch.Split(',');
        }
        if (!_smallFileExtensions.Any() && !_largeFileExtensions.Any())
            _doCdnRewrite = false;
        bool forceSsl;
        if (!string.IsNullOrWhiteSpace(ForceSsl) && bool.TryParse(ForceSsl, out forceSsl))
            _useSsl = forceSsl;
        if (!string.IsNullOrWhiteSpace(FilePathsToIgnore))
         { 
            var chArray = new[] { ',' };
            _excludeFilePaths = FilePathsToIgnore.Split(chArray);
            foreach (var str in _excludeFilePaths)
            {
                if (HttpContext.Current.Request.RawUrl.Contains(str))
                    _doCdnRewrite = false;
            }
        }
        _cdnParser = new CdnParser(_useSsl, smallDomainName, largeDomainName, _smallFileExtensions, _largeFileExtensions, _doFilePathMatching, _matchFilePaths, _excludeFilePaths, currentUrl);
    }

    public override void Flush()
    {
        if (_isClosing && !_isClosed)
        {
            var encoding = HttpContext.Current.Response.ContentEncoding;
            var cachedContent = encoding.GetString(_cachedStream.ToArray());
            // Filter the cached content
            cachedContent = _cdnParser.ReplaceHtml(cachedContent);
            var buffer = encoding.GetBytes(cachedContent);
            // Write new content to stream
            _responseStream.Write(buffer, 0, buffer.Length);
            _cachedStream.SetLength(0);
            _responseStream.Flush();
        }
    }

    public override void Close()
    {
        _isClosing = true;
        Flush();
        _isClosed = true;
        _isClosing = false;
        _responseStream.Close();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _responseStream.Seek(offset, origin);
    }

    public override void SetLength(long length)
    {
        _responseStream.SetLength(length);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _responseStream.Read(buffer, offset, count);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (_doCdnRewrite)
            _cachedStream.Write(buffer, offset, count);
        else
            _responseStream.Write(buffer, offset, count);
    }
}