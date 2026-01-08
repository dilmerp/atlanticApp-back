using Common.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Npgsql; // Necesario para NpgsqlConnection
using System.Data;

namespace Common.Persistence.Data
{
    public class NpgsqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public NpgsqlConnectionFactory(IConfiguration configuration)
        {
            // Obtiene la cadena de conexión de appsettings.json
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IDbConnection CreateConnection()
        {
            // Crea una conexión Npgsql y la abre inmediatamente para Dapper
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}