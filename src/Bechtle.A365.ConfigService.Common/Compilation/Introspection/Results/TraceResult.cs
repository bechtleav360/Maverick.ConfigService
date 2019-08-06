namespace Bechtle.A365.ConfigService.Common.Compilation.Introspection.Results
{
    public abstract class TraceResult
    {
        public TraceResult[] Children { get; set; }

        public string[] Errors { get; set; }

        public string[] Warnings { get; set; }
    }
}