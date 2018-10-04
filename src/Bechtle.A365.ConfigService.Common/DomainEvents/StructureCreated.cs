namespace Bechtle.A365.ConfigService.Common.DomainEvents
{
    /// <inheritdoc />
    /// <summary>
    ///     a Structure has been created with the given <see cref="StructureIdentifier" />
    /// </summary>
    public class StructureCreated : DomainEvent
    {
        /// <inheritdoc />
        public StructureCreated(StructureIdentifier identifier, ConfigKeyAction[] modifiedKeys)
        {
            Identifier = identifier;
            ModifiedKeys = modifiedKeys;
        }

        /// <inheritdoc />
        public StructureCreated()
        {
        }

        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Identifier { get; set; }
        
        /// <summary>
        ///     list of Actions that have been applied to the keys
        /// </summary>
        public ConfigKeyAction[] ModifiedKeys { get; set; }
    }
}