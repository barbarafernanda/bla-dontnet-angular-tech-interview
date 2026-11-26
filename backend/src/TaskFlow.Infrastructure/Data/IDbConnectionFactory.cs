using System.Data.Common;

namespace TaskFlow.Infrastructure.Data;

public interface IDbConnectionFactory
{
    Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
