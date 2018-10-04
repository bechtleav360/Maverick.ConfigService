namespace Bechtle.A365.ConfigService.Common
{
    /// <summary>
    ///     result of an operation without attached data
    /// </summary>
    public class Result
    {
        /// <summary>
        ///     generic error-code indicating the result of the operation
        /// </summary>
        public ErrorCode Code { get; set; }

        /// <summary>
        ///     quick-access property.
        ///     use like this:
        ///     <code>
        /// if(result.IsError){ ... }
        /// </code>
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        ///     error message explaining the error in more detail
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        ///     create a 'Error' result, with the provided <paramref name="message" /> and <paramref name="code" /> property,
        /// </summary>
        /// <param name="message"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static Result Error(string message, ErrorCode code) => new Result
        {
            Code = code,
            IsError = true,
            Message = message
        };

        /// <summary>
        ///     create a 'Success' result, with an empty <see cref="Result.Message" /> and <see cref="Result.Code" /> properties
        /// </summary>
        /// <returns></returns>
        public static Result Success() => new Result
        {
            Code = 0,
            IsError = false,
            Message = string.Empty
        };

        /// <summary>
        ///     convenience method to omit explicitly providing the type while calling to <see cref="Result{T}.Success(T)" />
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Result<T> Success<T>(T data) => Result<T>.Success(data);
    }

    /// <inheritdoc />
    /// <summary>
    ///     result of an operation with attached data
    ///     <see cref="T:Bechtle.A365.ConfigService.Common.Result" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Result<T> : Result
    {
        /// <summary>
        ///     attached data.
        ///     filled only when operation is successful.
        ///     defaults to <code>default(T)</code>
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        ///     create a 'Error' result, with the provided <paramref name="message" /> and <paramref name="code" /> property,
        ///     and an empty <see cref="Result{T}.Data" /> prop
        /// </summary>
        /// <param name="message"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public new static Result<T> Error(string message, ErrorCode code) => new Result<T>
        {
            Code = code,
            IsError = true,
            Message = message
        };

        /// <summary>
        ///     create a 'Success' result, with an empty <see cref="Result.Message" /> and <see cref="Result.Code" /> properties
        ///     and a filled <see cref="Result{T}.Data" /> property
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Result<T> Success(T data) => new Result<T>
        {
            Data = data,
            Code = 0,
            IsError = false,
            Message = string.Empty
        };
    }
}