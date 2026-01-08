
using System.Threading;
using System.Threading.Tasks;

namespace Common.Domain.Interfaces
{
    public interface IJobStatusRepository 
    {
        Task<Common.Domain.Entities.CargaArchivo> GetActiveJobByPeriodAsync(string periodo, CancellationToken cancellationToken);
        Task CreateInitialJobAsync(Common.Domain.Entities.CargaArchivo job, CancellationToken cancellationToken);
        
    }
}