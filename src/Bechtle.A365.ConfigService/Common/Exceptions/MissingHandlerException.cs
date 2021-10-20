using System;

namespace Bechtle.A365.ConfigService.Common.Exceptions
{
    /// <summary>
    ///     A
    /// </summary>
    public class MissingHandlerException : Exception
    {
        /// <inheritdoc />
        public MissingHandlerException(string handlerType)
            : base($"handler for '{handlerType}' is missing")
        {
        }
    }
}
