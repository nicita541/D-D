using Npgsql;

namespace backend.Infrastructure.Database;

public interface IPostgresConnectionFactory
{
    Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}
