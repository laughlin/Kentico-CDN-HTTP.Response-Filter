using System.Configuration;
using System.Linq;
using System.Web;
using Kentico.Google.Apis.Util;

/// <summary>
/// Summary description for CdnHelper
/// </summary>
public class CdnHelper
{
    public static bool IsCdnEnabled()
    {
        var enableCdn = ConfigurationManager.AppSettings["CdnEnabled"];
        if (!enableCdn.IsNullOrEmpty())
        {
            bool enable;
            if (bool.TryParse(enableCdn, out enable))
            {
                return enable;
            }
        }
        return false;
    }

    public static bool IsCdnAllowed(HttpResponse response, string rawUrl)
    {
        if (response.ContentType != "text/html" || rawUrl.Contains("/cms"))
            return false;
        var cdnDisallowedUrls = ConfigurationManager.AppSettings["CdnDisallowedUrls"];
        if (!cdnDisallowedUrls.IsNullOrEmpty())
        {
            var disallowedUrls = cdnDisallowedUrls.Split(',');
            if (disallowedUrls.Any(disallowedUrl => rawUrl.Contains(disallowedUrl)))
                return false;
        }
        return true;
    }
}