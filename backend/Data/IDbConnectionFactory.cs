using System.Data;
using Npgsql;

namespace LimpiezaUrbana.API.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public NpgsqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SupabaseConnection")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión 'ConnectionStrings:SupabaseConnection'.");
    }

    public IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}
