namespace Bechtle.A365.ConfigService.Dto.DomainEvents
{
    /// <inheritdoc />
    /// <summary>
    ///     a Structure has been deleted
    /// </summary>
    public class StructureDeleted : DomainEvent
    {
        /// <inheritdoc cref="StructureIdentifier" />
        public StructureIdentifier Identifier { get; set; }
    }
}