namespace Bechtle.A365.ConfigService.Common
{
    /// <inheritdoc />
    /// <summary>
    ///     result of an operation with attached data
    ///     <see cref="T:Bechtle.A365.ConfigService.Common.Result" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IResult<T> : IResult
    {
        /// <summary>
        ///     Computed Property, that returns <see cref="Data" /> or throws an Exception.
        ///     Ensure that this Result contains valid data, by checking <see cref="IResult{T}.IsError" />
        /// </summary>
        T CheckedData { get; }

        /// <summary>
        ///     attached data.
        ///     filled only when operation is successful.
        ///     defaults to <code>default(T)</code>
        /// </summary>
        T? Data { get; set; }
    }

    /// <summary>
    ///     result of an operation without attached data
    /// </summary>
    public interface IResult
    {
        /// <summary>
        ///     generic error-code indicating the result of the operation
        /// </summary>
        ErrorCode Code { get; set; }

        /// <summary>
        ///     quick-access property.
        ///     use like this:
        ///     <code>
        /// if(result.IsError){ ... }
        /// </code>
        /// </summary>
        bool IsError { get; set; }

        /// <summary>
        ///     error message explaining the error in more detail
        /// </summary>
        string Message { get; set; }
    }
}
