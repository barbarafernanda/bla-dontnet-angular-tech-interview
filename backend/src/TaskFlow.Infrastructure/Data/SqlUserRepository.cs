using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Exceptions;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Data;

public sealed class SqlUserRepository(IDbConnectionFactory connections) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        FindAsync("SELECT Id, FullName, Email, PasswordHash, CreatedAt FROM dbo.Users WHERE Id = @id", "@id", SqlDbType.UniqueIdentifier, id, 0, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        FindAsync("SELECT Id, FullName, Email, PasswordHash, CreatedAt FROM dbo.Users WHERE Email = @email", "@email", SqlDbType.VarChar, email, 255, cancellationToken);

    private async Task<User?> FindAsync(string sql, string parameter, SqlDbType type, object value, int size, CancellationToken cancellationToken)
    {
        await using var connection = await connections.CreateConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.AddParameter(parameter, type, value, size);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new User(reader.GetGuid(reader.GetOrdinal("Id")), reader.GetString(reader.GetOrdinal("FullName")),
            reader.GetString(reader.GetOrdinal("Email")), reader.GetString(reader.GetOrdinal("PasswordHash")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")));
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        await using var connection = await connections.CreateConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO dbo.Users (Id, FullName, Email, PasswordHash, CreatedAt) VALUES (@id, @name, @email, @hash, @created)";
        command.AddParameter("@id", SqlDbType.UniqueIdentifier, user.Id);
        command.AddParameter("@name", SqlDbType.VarChar, user.FullName, 150);
        command.AddParameter("@email", SqlDbType.VarChar, user.Email, 255);
        command.AddParameter("@hash", SqlDbType.NVarChar, user.PasswordHash, 512);
        command.AddParameter("@created", SqlDbType.DateTimeOffset, user.CreatedAt);
        try { await command.ExecuteNonQueryAsync(cancellationToken); }
        catch (SqlException exception) when ((exception.Number is 2601 or 2627) && exception.Message.Contains("UX_Users_Email", StringComparison.OrdinalIgnoreCase))
        {
            throw new DuplicateEmailException("An account with this email already exists.", exception);
        }
    }
}
