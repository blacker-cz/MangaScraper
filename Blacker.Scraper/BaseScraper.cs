using System;
using System.Collections.Generic;
using Blacker.Scraper.Models;
using log4net;
using System.Text.RegularExpressions;

namespace Blacker.Scraper
{
    public abstract class BaseScraper
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(BaseScraper));

        #region Random numbers generator

        private readonly Random _randomGenerator = new Random(Environment.TickCount);

        protected int Random { get { return _randomGenerator.Next(0, Int32.MaxValue); } }

        #endregion // Random numbers generator

        protected abstract string BaseUrl { get; }

        protected string GetFullUrl(string url)
        {
            return GetFullUrl(url, BaseUrl);
        }

        protected string GetFullUrl(string url, string urlBase)
        {
            var baseUri = new Uri(urlBase);
            Uri uri;

            if (Uri.TryCreate(baseUri, url, out uri))
            {
                return uri.AbsoluteUri;
            }
            return url;
        }

        protected string CleanupText(string text)
        {
            Regex rgx = new Regex(@"\s+");
            return System.Net.WebUtility.HtmlDecode(rgx.Replace(text, " ").Trim());
        }
    }
}
