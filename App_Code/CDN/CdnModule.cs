using System;
using System.Web;

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
        if (CdnHelper.IsCdnEnabled())
            context.ReleaseRequestState += ReleaseRequestState;
    }

    static void ReleaseRequestState(object sender, EventArgs e)
    {
        var response = HttpContext.Current.Response;
        var rawUrl = HttpContext.Current.Request.RawUrl.ToLower();
        if (!CdnHelper.IsCdnAllowed(response, rawUrl))
            return;
        response.Filter = new CdnFilter(HttpContext.Current.Response.Filter, HttpContext.Current.Request);
    }
}