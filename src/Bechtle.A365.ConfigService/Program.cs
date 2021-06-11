using System.Threading.Tasks;
using Bechtle.A365.ConfigService.Cli;
using Bechtle.A365.ServiceBase;

namespace Bechtle.A365.ConfigService
{
    /// <summary>
    ///     Main Entry-Point for the Application
    /// </summary>
    public static class Program
    {
        /// <summary>
        ///     Delegate App-Startup to the default ServiceBase-Behaviour
        /// </summary>
        /// <param name="args"></param>
        public static Task<int> Main(string[] args) => Setup.Main<Startup, CliBase>(args);
    }
}
