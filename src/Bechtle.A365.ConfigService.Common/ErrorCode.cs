namespace Bechtle.A365.ConfigService.Common
{
    /// <summary>
    ///     generic ErrorCode
    /// </summary>
    public enum ErrorCode
    {
        /// <summary>
        ///     no Error occured
        /// </summary>
        None = 0,

        /// <summary>
        ///     undefined Error occured
        /// </summary>
        Undefined = 1,

        /// <summary>
        ///     environment already exists
        /// </summary>
        EnvironmentAlreadyExists = 2,

        /// <summary>
        ///     structure already exists
        /// </summary>
        StructureAlreadyExists = 3,

        /// <summary>
        ///     error while updating database
        /// </summary>
        DbUpdateError = 4,

        /// <summary>
        ///     a resource could not be found, see message for more details
        /// </summary>
        NotFound = 5,

        /// <summary>
        ///     given data is invalid or unsupported
        /// </summary>
        InvalidData = 6,

        /// <summary>
        ///     another default-environment already exists for that category
        /// </summary>
        DefaultEnvironmentAlreadyExists = 7,

        /// <summary>
        ///     error while querying database
        /// </summary>
        DbQueryError = 8,

        /// <summary>
        ///     DomainEvent contains invalid data and cannot be executed / written
        /// </summary>
        ValidationFailed = 9
    }
}