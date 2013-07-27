using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blacker.MangaScraper.Common;
using Blacker.MangaScraper.Helpers;
using Blacker.MangaScraper.Recent;
using log4net;

namespace Blacker.MangaScraper
{
    class ScraperLoader
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ScraperLoader));
        private static ScraperLoader _instance;
        private static readonly object _lock = new object();

        private AppDomain _pluginsAppDomain;

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

        public void Reload()
        {
            try
            {
                if (_pluginsAppDomain != null)
                {
                    AppDomain.Unload(_pluginsAppDomain);
                }

                _pluginsAppDomain = AppDomain.CreateDomain("Blacker.Scrapers.AppDomain");

                ReflectionHelper.LoadAssembliesFromDir(_pluginsAppDomain, "*.Scraper.dll");

                var factories = ReflectionHelper.GetInstances<IScraperFactory>(_pluginsAppDomain);

                _scrapers = ReflectionHelper
                    .GetInstances<IScraper>(_pluginsAppDomain, new[] {typeof (IScraperIgnore)})
                    .Concat(factories.SelectMany(f => f.GetScrapers()))
                    .Concat(new[] {new RecentMangaScraper()});
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
