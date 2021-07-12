namespace Bechtle.A365.ConfigService.Interfaces
{
    /// <summary>
    ///     Component that provides the File-Name that should be used to store DomainObject data
    /// </summary>
    public interface IDomainObjectStoreLocationProvider
    {
        /// <summary>
        ///     Filename of where the local DomainObject-Database should be stored 
        /// </summary>
        public string FileName { get; }
    }
}
