namespace Bechtle.A365.ConfigService.DomainObjects
{
    public class StreamedObjectSnapshot
    {
        public string DataType { get; set; }

        public long Version { get; set; }

        public byte[] Data { get; set; }
    }
}