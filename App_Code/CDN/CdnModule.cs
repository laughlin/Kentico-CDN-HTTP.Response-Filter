using System;
using System.Configuration;
using System.Linq;
using System.Web;
using Kentico.Google.Apis.Util;

/// <summary>
/// Taken from https://devnet.kentico.com/marketplace/utilities/kentico-cdn-injector
/// and http://stackoverflow.com/questions/6061286/looking-for-a-httphandler-to-modify-pages-on-the-fly-to-point-to-a-cdn
/// and http://stackoverflow.com/questions/4464983/how-do-i-implement-an-http-response-filter-to-operate-on-the-entire-content-at-o
/// </summary>
public class CdnModule : IHttpModule
{
    void IHttpModule.Dispose()
    {
    }

    void IHttpModule.Init(HttpApplication context)
    {
        var enableCdn = ConfigurationManager.AppSettings["CdnEnabled"];
        if (!enableCdn.IsNullOrEmpty())
        {
            bool enable;
            if (bool.TryParse(enableCdn, out enable))
            {
                if(enable)
                    context.ReleaseRequestState += ReleaseRequestState;
            }
        }
    }

    static void ReleaseRequestState(object sender, EventArgs e)
    {
        var response = HttpContext.Current.Response;
        var rawUrl = HttpContext.Current.Request.RawUrl.ToLower();
        if (response.ContentType != "text/html" || rawUrl.Contains("/cms"))
            return;
        var cdnDisallowedUrls = ConfigurationManager.AppSettings["CdnDisallowedUrls"];
        if (!cdnDisallowedUrls.IsNullOrEmpty())
        {
            var disallowedUrls = cdnDisallowedUrls.Split(',');
            if (disallowedUrls.Any(disallowedUrl => rawUrl.Contains(disallowedUrl)))
                return;
        }
        response.Filter = new CdnFilter(HttpContext.Current.Response.Filter, HttpContext.Current.Request.Url);
    }
}