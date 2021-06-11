using System;
using System.Runtime.Serialization;

namespace Bechtle.A365.ConfigService.Cli.Commands.MigrationModels
{
    /// <summary>
    ///     Exception indicating that some invalid operation occurred in the EventStream, which may alter the result of the Migration.
    /// </summary>
    [Serializable]
    public class MigrationReplayException : Exception
    {
        /// <inheritdoc />
        public MigrationReplayException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        protected MigrationReplayException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}