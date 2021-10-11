using System;

namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <summary>
    ///     Base-Class for all Identifiers
    /// </summary>
    public abstract class Identifier
    {
        /// <summary>
        ///     Create an empty representation of the target-Identifier
        /// </summary>
        /// <typeparam name="TIdentifier">type of Identifier to create empty instance of</typeparam>
        /// <returns>new instance of <typeparamref name="TIdentifier" />, or throws Exception</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <typeparamref name="TIdentifier" /> is unknown</exception>
        public static TIdentifier Empty<TIdentifier>()
            where TIdentifier : Identifier
            => typeof(TIdentifier).Name switch
               {
                   nameof(EnvironmentIdentifier) => new EnvironmentIdentifier(string.Empty, string.Empty) as TIdentifier,
                   nameof(StructureIdentifier) => new StructureIdentifier(string.Empty, 0) as TIdentifier,
                   nameof(LayerIdentifier) => new LayerIdentifier(string.Empty) as TIdentifier,
                   nameof(ConfigurationIdentifier) => new ConfigurationIdentifier(
                                                          Empty<EnvironmentIdentifier>(),
                                                          Empty<StructureIdentifier>(),
                                                          0) as TIdentifier,
                   _ => throw new ArgumentOutOfRangeException()
               }
               ?? throw new ArgumentOutOfRangeException();
    }
}
