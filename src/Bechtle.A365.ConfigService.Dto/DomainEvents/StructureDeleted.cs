namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <inheritdoc />
    /// <summary>
    ///     a Structure has been deleted
    /// </summary>
    public class StructureDeleted : DomainEvent
    {
        /// <inheritdoc />
        public StructureDeleted(StructureIdentifier identifier)
        {
            Identifier = identifier;
        }

        /// <inheritdoc />
        public StructureDeleted()
        {
        }

        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Identifier { get; set; }
    }
}