using System;
using System.Configuration;
using log4net;

namespace Blacker.MangaScraper.Library.Configuration
{
    public class LibraryConfiguration : ConfigurationSection
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof (LibraryConfiguration));

        private static LibraryConfiguration _instance = null;

        private static readonly object _syncRoot = new object();

        public static LibraryConfiguration Instance
        {
            get
            {
                lock (_syncRoot)
                {
                    if (_instance == null)
                    {
                        LibraryConfiguration instance = null;

                        try
                        {
                            instance = ConfigurationManager.GetSection("libraryConfiguration") as LibraryConfiguration;
                        }
                        catch (Exception ex)
                        {
                            _log.Warn("Unable to load configuration. Using default.", ex);
                        }

                        _instance = instance ?? new LibraryConfiguration();
                    }

                    return _instance;
                }
            }
        }

        /// <summary>
        /// Storage location as can be found in the config file
        /// </summary>
        [ConfigurationProperty("storageLocation", IsRequired = false, DefaultValue = @"%ALLUSERSPROFILE%\Blacker\MangaScraper\Data\library.sqlite")]
        public string StorageLocation
        {
            get { return (string) this["storageLocation"]; }
        }

        /// <summary>
        /// Storage location with expanded environmental variables
        /// </summary>
        public string StoragePath
        {
            get { return Environment.ExpandEnvironmentVariables(StorageLocation); }
        }
    }
}
