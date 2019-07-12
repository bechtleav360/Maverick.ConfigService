namespace Bechtle.A365.ConfigService.Cli
{
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