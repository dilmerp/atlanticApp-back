using System.Data;

namespace Common.Domain.Interfaces
{
    public interface IDbConnectionFactory
    {
        
        IDbConnection CreateConnection();
    }
}