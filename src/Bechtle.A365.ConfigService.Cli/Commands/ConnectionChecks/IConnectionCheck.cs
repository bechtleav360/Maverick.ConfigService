using System.Threading.Tasks;

namespace Bechtle.A365.ConfigService.Cli.Commands.ConnectionChecks
{
    public interface IConnectionCheck
    {
        string Name { get; }

        Task<TestResult> Execute(FormattedOutput output, TestParameters parameters);
    }
}