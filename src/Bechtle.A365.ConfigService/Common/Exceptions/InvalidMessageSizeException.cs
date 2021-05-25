using System;

namespace Bechtle.A365.ConfigService.Common.Exceptions
{
    /// <summary>
    ///     The total Message-Size was larger than allowed by the subcomponent
    /// </summary>
    public class InvalidMessageSizeException : Exception
    {
        /// <inheritdoc />
        public InvalidMessageSizeException(
            long messageSize,
            long allowedMessageSize)
            : base($"Message was larger than allowed {messageSize:N} >= {allowedMessageSize:N}")
        {
        }

        /// <inheritdoc />
        public InvalidMessageSizeException(
            long messageSize,
            long allowedMessageSize,
            Exception innerException)
            : base(
                $"Message was larger than allowed {messageSize:N} >= {allowedMessageSize:N}",
                innerException)
        {
        }
    }
}
