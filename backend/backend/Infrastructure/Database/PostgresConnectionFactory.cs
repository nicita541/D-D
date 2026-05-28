using Npgsql;

namespace backend.Infrastructure.Database;

public sealed class PostgresConnectionFactory : IPostgresConnectionFactory
{
    private readonly string _connectionString;

    public PostgresConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DndDatabase")
            ?? throw new InvalidOperationException("Connection string 'DndDatabase' not found.");
    }

    public NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task<NpgsqlConnection> CreateOpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = CreateConnection();

        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}