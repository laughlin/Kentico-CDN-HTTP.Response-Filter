using System.Web;

namespace CMS.Controls
{
    /// <summary>
    /// Extends the CMSTransformation partial class.
    /// </summary>
    public partial class CMSTransformation
    {
        /// <summary>
        /// Determies if a CDN URL should be prefixed to the path
        /// </summary>
        /// <param name="path">The path to be prefixed</param>
        public string CdnPrefix(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            if (!CdnHelper.IsCdnEnabled()) return path;
            var response = HttpContext.Current.Response;
            var rawUrl = HttpContext.Current.Request.RawUrl.ToLower();
            if (!CdnHelper.IsCdnAllowed(response, rawUrl))
                return path;
            var cdnFilter = new CdnFilter(HttpContext.Current.Response.Filter, HttpContext.Current.Request);
            var cdnPath = cdnFilter.CdnParser.GetUrl(path);
            cdnFilter.ForceClose = true;
            cdnFilter.Close();
            return cdnPath;
        }
    }
}