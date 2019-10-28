namespace Bechtle.A365.ConfigService.Common
{
    /// <inheritdoc />
    public class Result : IResult
    {
        /// <inheritdoc />
        public ErrorCode Code { get; set; }

        /// <inheritdoc />
        public bool IsError { get; set; }

        /// <inheritdoc />
        public string Message { get; set; }

        /// <summary>
        ///     create a 'Error' result, with the provided <paramref name="message" /> and <paramref name="code" /> property,
        /// </summary>
        /// <param name="message"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static IResult Error(string message, ErrorCode code) => new Result
        {
            Code = code,
            IsError = true,
            Message = message
        };

        /// <summary>
        ///     create a 'Error' result, with the provided <paramref name="message" /> and <paramref name="code" /> property,
        ///     and an empty <see cref="Result{T}.Data" /> prop
        /// </summary>
        /// <param name="message"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static IResult<T> Error<T>(string message, ErrorCode code) => new Result<T>
        {
            Code = code,
            IsError = true,
            Message = message
        };

        /// <summary>
        ///     create a 'Error' result, with the provided <paramref name="message" /> and <paramref name="code" /> property,
        ///     and an filled <see cref="Result{T}.Data" /> prop
        /// </summary>
        /// <param name="message"></param>
        /// <param name="code"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IResult<T> Error<T>(string message, ErrorCode code, T data) => new Result<T>
        {
            Code = code,
            Data = data,
            IsError = true,
            Message = message
        };

        /// <summary>
        ///     create a 'Success' result, with an empty <see cref="Result.Message" /> and <see cref="Result.Code" /> properties
        /// </summary>
        /// <returns></returns>
        public static IResult Success() => new Result
        {
            Code = 0,
            IsError = false,
            Message = string.Empty
        };

        /// <summary>
        ///     create a 'Success' result, with an empty <see cref="Result.Message" /> and <see cref="Result.Code" /> properties
        ///     and a filled <see cref="Result{T}.Data" /> property
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IResult<T> Success<T>(T data) => new Result<T>
        {
            Data = data,
            Code = 0,
            IsError = false,
            Message = string.Empty
        };
    }

    /// <inheritdoc cref="IResult{T}" />
    /// <inheritdoc cref="Result" />
    public class Result<T> : Result, IResult<T>
    {
        /// <inheritdoc />
        public T Data { get; set; }
    }
}