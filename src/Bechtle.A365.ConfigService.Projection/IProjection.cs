using System.Threading;
using System.Threading.Tasks;

namespace Bechtle.A365.ConfigService.Projection
{
    public interface IProjection
    {
        Task Start(CancellationToken cancellationToken);
    }
}