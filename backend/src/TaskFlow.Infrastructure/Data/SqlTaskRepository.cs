using System.Data;
using System.Data.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Repositories;

namespace TaskFlow.Infrastructure.Data;

public sealed class SqlTaskRepository(IDbConnectionFactory connections) : ITaskRepository
{
    private const string Columns = "Id, UserId, Title, Description, Status, DueDate, CreatedAt, UpdatedAt";

    public async Task<IReadOnlyList<TaskItem>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.CreateConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM dbo.Tasks WHERE UserId = @userId";
        command.AddParameter("@userId", SqlDbType.UniqueIdentifier, userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<TaskItem>();
        while (await reader.ReadAsync(cancellationToken)) result.Add(Map(reader));
        return result;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.CreateConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {Columns} FROM dbo.Tasks WHERE Id = @id AND UserId = @userId";
        command.AddParameter("@id", SqlDbType.UniqueIdentifier, id);
        command.AddParameter("@userId", SqlDbType.UniqueIdentifier, userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        await using var connection = await connections.CreateConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO dbo.Tasks (Id, UserId, Title, Description, Status, DueDate, CreatedAt, UpdatedAt) VALUES (@id, @userId, @title, @description, @status, @due, @created, @updated)";
        AddValues(command, task, task.UserId, includeCreated: true);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(TaskItem task, Guid userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (task.UserId != userId) return false;
        await using var connection = await connections.CreateConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.Tasks SET Title = @title, Description = @description, Status = @status, DueDate = @due, UpdatedAt = @updated WHERE Id = @id AND UserId = @userId";
        AddValues(command, task, userId, includeCreated: false);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connections.CreateConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dbo.Tasks WHERE Id = @id AND UserId = @userId";
        command.AddParameter("@id", SqlDbType.UniqueIdentifier, id);
        command.AddParameter("@userId", SqlDbType.UniqueIdentifier, userId);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void AddValues(DbCommand command, TaskItem item, Guid userId, bool includeCreated)
    {
        command.AddParameter("@id", SqlDbType.UniqueIdentifier, item.Id);
        command.AddParameter("@userId", SqlDbType.UniqueIdentifier, userId);
        command.AddParameter("@title", SqlDbType.VarChar, item.Title, 120);
        command.AddParameter("@description", SqlDbType.VarChar, item.Description, 8000);
        command.AddParameter("@status", SqlDbType.VarChar, item.Status.ToString(), 32);
        command.AddParameter("@due", SqlDbType.DateTimeOffset, item.DueDate);
        if (includeCreated) command.AddParameter("@created", SqlDbType.DateTimeOffset, item.CreatedAt);
        command.AddParameter("@updated", SqlDbType.DateTimeOffset, item.UpdatedAt);
    }

    private static TaskItem Map(DbDataReader reader)
    {
        var description = reader.GetOrdinal("Description");
        return new TaskItem(reader.GetGuid(reader.GetOrdinal("Id")), reader.GetGuid(reader.GetOrdinal("UserId")),
            reader.GetString(reader.GetOrdinal("Title")), reader.IsDBNull(description) ? null : reader.GetString(description),
            TaskStatusParser.Parse(reader.GetString(reader.GetOrdinal("Status"))), reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("DueDate")),
            reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")), reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("UpdatedAt")));
    }
}
