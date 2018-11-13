namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc cref="DomainEvent"/>
    /// <summary>
    ///     a number of variables within the Structure have been changed
    /// </summary>
    public class StructureVariablesModified : DomainEvent
    {
        /// <inheritdoc />
        public StructureVariablesModified(StructureIdentifier identifier, ConfigKeyAction[] modifiedKeys)
        {
            Identifier = identifier;
            ModifiedKeys = modifiedKeys;
        }

        /// <inheritdoc />
        public StructureVariablesModified()
        {
        }

        /// <inheritdoc cref="StructureIdentifier"/>
        public StructureIdentifier Identifier { get; set; }

        /// <summary>
        ///     list of actions that have been applied to the variables
        /// </summary>
        public ConfigKeyAction[] ModifiedKeys { get; set; }
    }
}