using System;

namespace Bechtle.A365.ConfigService.Common.Exceptions
{
    /// <summary>
    ///     Thrown when <see cref="IResult{T}.Data" /> of a failed operation is accessed through <see cref="IResult{T}.CheckedData" />.
    ///     This would otherwise lead to a <see cref="NullReferenceException" />, because <see cref="IResult{T}.Data" /> would ne <c>null</c>.
    /// </summary>
    public class InvalidResultAccessException : Exception
    {
        /// <summary>
        ///     Result that caused this Operation
        /// </summary>
        public IResult<object> Result { get; }

        /// <inheritdoc />
        public InvalidResultAccessException(IResult<object> result)
            : base("Result of a failed operation was accessed.")
        {
            Result = result;
        }
    }
}
