namespace Bechtle.A365.ConfigService.Cli.Commands.ConnectionChecks
{
    public class TestParameters
    {
        public string ConfigServiceEndpoint { get; set; }

        public string[] Sources { get; set; }
        
        public string[] PassThruArguments { get; set; }
        
        public string UseDefaultConfigSources { get; set; }
    }
}