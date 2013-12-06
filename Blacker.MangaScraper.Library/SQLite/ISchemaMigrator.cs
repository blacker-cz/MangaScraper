using System.Data.SQLite;

namespace Blacker.MangaScraper.Library.SQLite
{
    internal interface ISchemaMigrator
    {
        long FromVersion { get; }

        void Migrate(SQLiteConnection connection);
    }
}