using Microsoft.Extensions.Configuration;

namespace Bechtle.A365.ConfigService.Cli
{
    public class ApplicationSettings
    {
        public string ConnectionString { get; set; }

        public DbBackend DbType { get; set; }

        public IConfiguration EffectiveConfiguration { get; set; }
    }

    /// <summary>
    ///     The Database-Backend to use
    /// </summary>
    public enum DbBackend
    {
        /// <summary>
        ///     Use Microsofts SQL-Server
        /// </summary>
        MsSql,

        /// <summary>
        ///     Use the Open-Source Database PostgreSql
        /// </summary>
        Postgres
    }
}