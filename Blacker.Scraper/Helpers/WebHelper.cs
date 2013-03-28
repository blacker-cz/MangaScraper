using System;
using HtmlAgilityPack;
using System.Net;
using System.IO;
using Blacker.Scraper.Exceptions;
using System.Drawing;
using log4net;
using System.Diagnostics;

namespace Blacker.Scraper.Helpers
{
    internal static class WebHelper
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(WebHelper));

        /// <summary>
        /// Get HTML node of the remote website
        /// </summary>
        /// <param name="url">Website url</param>
        /// <returns>Html node of remote website</returns>
        /// <exception cref="HttpException" />
        /// <exception cref="ParserException" />
        public static HtmlNode GetHtmlDocument(string url)
        {
            if (url == null)
                throw new ArgumentNullException("url");

            HtmlDocument doc = new HtmlDocument();
            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                var request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    doc.Load(stream, true);
                }

                stopWatch.Stop();

                _log.DebugFormat("Download of URL '{0}' took '{1}'", url, stopWatch.Elapsed);

                return doc.DocumentNode;
            }
            catch (WebException ex)
            {
                throw new HttpException("Could not load remote website.", ex);
            }
            catch (IOException ex)
            {
                throw new HttpException("Could not load remote website.", ex);
            }
            catch (Exception ex)
            {
                throw new ParserException("Could not process remote website request.", ex);
            }
        }

        public static Image GetImageFromUrl(string imageUrl)
        {
            if (imageUrl == null)
                throw new ArgumentNullException("imageUrl");

            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                var request = HttpWebRequest.Create(imageUrl);
                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    MemoryStream memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);

                    stopWatch.Stop();
                    _log.DebugFormat("Download of image from URL '{0}' took '{1}'", imageUrl, stopWatch.Elapsed);
                    
                    return new Bitmap(memoryStream);
                }
            }
            catch (Exception ex)
            {
                throw new HttpException("Could not load remote website.", ex);
            }
        }

        public static void DownloadImage(string imageUrl, string fileName)
        {
            if (imageUrl == null)
                throw new ArgumentNullException("imageUrl");
            if (fileName == null)
                throw new ArgumentNullException("fileName");

            using (var image = GetImageFromUrl(imageUrl))
            {
                image.Save(fileName);
            }
        }
    }
}
