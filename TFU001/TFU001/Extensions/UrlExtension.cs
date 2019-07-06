using System;
using System.Linq;

namespace TFU001.Extensions
{
    public static class UrlExtension
    {
        /// <summary>
        /// Copied from https://stackoverflow.com/questions/7578857/how-to-check-whether-a-string-is-a-valid-http-url
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static bool IsValidUrl(this string address)
        {
            Uri uriResult;
            bool result = Uri.TryCreate(address, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            return result;
        }
    }
}