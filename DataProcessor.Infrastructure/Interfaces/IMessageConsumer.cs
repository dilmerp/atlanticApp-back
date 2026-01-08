using System.Threading;
using System.Threading.Tasks;

namespace DataProcessor.Infrastructure.Interfaces
{
    public interface IMessageConsumer
    {
        void StartConsuming();
        void StopConsuming();
    }
}