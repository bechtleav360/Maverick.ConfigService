namespace Bechtle.A365.ConfigService.Configuration
{
    /// <summary>
    ///     options for different DbContext-Backends
    /// </summary>
    public enum DbBackend
    {
        /// <summary>
        ///     default value, indicates mis-configuration
        /// </summary>
        None,

        /// <summary>
        ///     use Microsofts SqlServer backend
        /// </summary>
        MsSql,

        /// <summary>
        ///     use the open-source PostgreSql
        /// </summary>
        Postgres
    }
}