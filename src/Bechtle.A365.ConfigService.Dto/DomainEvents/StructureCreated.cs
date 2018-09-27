namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <inheritdoc />
    /// <summary>
    ///     a Structure has been created with the given <see cref="StructureIdentifier" />
    /// </summary>
    public class StructureCreated : DomainEvent
    {
        /// <inheritdoc />
        public StructureCreated(StructureIdentifier identifier)
        {
            Identifier = identifier;
        }

        /// <inheritdoc />
        public StructureCreated()
        {
        }

        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Identifier { get; set; }
    }
}