using Npgsql;

namespace backend.Infrastructure.Database;

public interface IPostgresConnectionFactory
{
    NpgsqlConnection CreateConnection();

    Task<NpgsqlConnection> CreateOpenConnectionAsync(
        CancellationToken cancellationToken = default);
}