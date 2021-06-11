using System.Collections.Generic;
using EventStore.Client;

namespace Bechtle.A365.ConfigService.Cli.Commands.MigrationModels
{
    /// <summary>
    ///     Data collected from the EventStore that represents a specific state that can be migrated to a new one
    /// </summary>
    public interface IState
    {
        /// <summary>
        ///     take the Domain-Event and apply its changes.
        /// </summary>
        /// <param name="recordedEvent">some recorded event from the EventStore</param>
        /// <param name="ignoreReplayErrors">flag indicating if replay-errors should be ignored or throw an <see cref="MigrationReplayException" /></param>
        void ApplyEvent(ResolvedEvent recordedEvent, bool ignoreReplayErrors);

        /// <summary>
        ///     Generates the Data written to the EventStore as a Tuple
        /// </summary>
        /// <returns>Tuple of Type/Data/Metadata</returns>
        List<(string Type, byte[] Data, byte[] Metadata)> GenerateEventData();
    }
}