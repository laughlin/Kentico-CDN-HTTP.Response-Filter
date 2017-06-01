using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using CMS.Helpers;

/// <summary>
/// Taken from https://devnet.kentico.com/marketplace/utilities/kentico-cdn-injector
/// and http://stackoverflow.com/questions/6061286/looking-for-a-httphandler-to-modify-pages-on-the-fly-to-point-to-a-cdn
/// and http://stackoverflow.com/questions/4464983/how-do-i-implement-an-http-response-filter-to-operate-on-the-entire-content-at-o
/// </summary>
public class CdnFilter : Stream
{
    public readonly CdnParser CdnParser;
    public bool ForceClose;
    private readonly string _cdnSmallDomain = ConfigurationManager.AppSettings["CdnSmallDomain"];
    private readonly string _cdnLargeDomain = ConfigurationManager.AppSettings["CdnLargeDomain"];
    private readonly string _forceSsl = ConfigurationManager.AppSettings["CdnForceSsl"];
    private readonly string _smallFileExtensionsToMatch = ConfigurationManager.AppSettings["CdnSmallFileExtensionsToMatch"];
    private readonly string _largeFileExtensionsToMatch = ConfigurationManager.AppSettings["CdnLargeFileExtensionsToMatch"];
    private readonly string _filePathsToMatch = ConfigurationManager.AppSettings["CdnFilePathsToMatch"];
    private readonly string _filePathsToIgnore = ConfigurationManager.AppSettings["CdnFilePathsToIgnore"];
    private readonly MemoryStream _cachedStream = new MemoryStream(1024);
    private readonly Stream _responseStream;
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

    public CdnFilter(Stream inputStream, HttpRequest currentRequest)
    {
        _responseStream = inputStream;
        var smallDomainName = string.IsNullOrWhiteSpace(_cdnSmallDomain) ? string.Empty : _cdnSmallDomain;
        var largeDomainName = string.IsNullOrWhiteSpace(_cdnLargeDomain) ? string.Empty : _cdnLargeDomain;
        if (!string.IsNullOrEmpty(smallDomainName) || !string.IsNullOrEmpty(largeDomainName))
            _doCdnRewrite = true;
        if (!string.IsNullOrWhiteSpace(_filePathsToMatch))
        {
            _matchFilePaths = _filePathsToMatch.Split(',');
            _doFilePathMatching = _matchFilePaths.Length > 0;
        }
        else
            _doCdnRewrite = false;
        if (!string.IsNullOrWhiteSpace(_smallFileExtensionsToMatch))
        {
            _smallFileExtensions = _smallFileExtensionsToMatch.Split(',');
        }
        if (!string.IsNullOrWhiteSpace(_largeFileExtensionsToMatch))
        {
            _largeFileExtensions = _largeFileExtensionsToMatch.Split(',');
        }
        if (!_smallFileExtensions.Any() && !_largeFileExtensions.Any())
            _doCdnRewrite = false;
        bool forceSsl;
        if (!string.IsNullOrWhiteSpace(_forceSsl) && bool.TryParse(_forceSsl, out forceSsl))
            _useSsl = forceSsl;
        // Loads the request headers as a collection
        var headers = currentRequest.Headers;
        // Gets the value from the X-Forwarded-Ssl header
        var forwardedSsl = headers.Get("X-Forwarded-Ssl");
        var protoSsl = headers.Get("X-Forwarded-Proto");
        // Checks if the original request used HTTPS
        if ((!string.IsNullOrEmpty(forwardedSsl) && forwardedSsl == "On") || (!string.IsNullOrEmpty(protoSsl) && protoSsl == "https") || RequestContext.IsSSL)
        {
            _useSsl = true;
        }
        if (!string.IsNullOrWhiteSpace(_filePathsToIgnore))
        {
            var chArray = new[] { ',' };
            _excludeFilePaths = _filePathsToIgnore.Split(chArray);
            foreach (var str in _excludeFilePaths)
            {
                if (currentRequest.RawUrl.Contains(str))
                {
                    _doCdnRewrite = false;
                    break;
                }
                    
            }
        }
        CdnParser = new CdnParser(_useSsl, smallDomainName, largeDomainName, _smallFileExtensions, _largeFileExtensions, _doFilePathMatching, _matchFilePaths, _excludeFilePaths, currentRequest.Url);
    }

    public override void Flush()
    {
        if (_isClosing && !_isClosed)
        {
            var encoding = HttpContext.Current.Response.ContentEncoding;
            if (_cachedStream != null)
            {
                var cachedContent = encoding.GetString(_cachedStream.ToArray());
                // Filter the cached content
                cachedContent = CdnParser.ReplaceHtml(cachedContent);
                var buffer = encoding.GetBytes(cachedContent);
                // Write new content to stream
                _responseStream.Write(buffer, 0, buffer.Length);
                _cachedStream.Flush();
            }
            _responseStream.Flush();
        }
    }

    public override void Close()
    {
        if (!ForceClose)
        {
            _isClosing = true;
            Flush();
            _isClosed = true;
            _isClosing = false;
        }
        else
            _responseStream.Flush();
        _cachedStream.Close();
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