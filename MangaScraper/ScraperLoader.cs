using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Helpers;
using log4net;

namespace Blacker.MangaScraper
{
    class ScraperLoader
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ScraperLoader));
        private static ScraperLoader _instance;
        private static readonly object _lock = new object();

        private IEnumerable<IScraper> _scrapers;

        private ScraperLoader()
        {
            Reload();
        }

        public static ScraperLoader Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new ScraperLoader();

                    return _instance;
                }
            }
        }

        // fixme: this should be more intelligent so one failing scraper will not kill whole application
        public void Reload()
        {
            try
            {
                // todo: this doesn't work if assemblies are not all loaded in AppDomain
                AppDomain.CurrentDomain.Load("Blacker.Scraper");

                var factories = ReflectionHelper.GetInstances<IScraperFactory>();

                _scrapers = ReflectionHelper
                    .GetInstances<IScraper>(new[] { typeof(IScraperIgnore) })
                    .Concat(factories.SelectMany(f => f.GetScrapers()));
            }
            catch (Exception ex)
            {
                _log.Error("Unable to load scrapers.", ex);

                throw;
            }
        }

        public IEnumerable<IScraper> AllScrapers
        {
            get { return _scrapers; }
        }

        public IEnumerable<IScraper> EnabledScrapers
        {
            get { return _scrapers.Where(s => !Properties.Settings.Default.DisabledScrapers.Contains(s.ScraperGuid)); }
        }
    }
}
