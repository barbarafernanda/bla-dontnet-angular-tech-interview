using System.Data;
using TaskFlow.Application.Abstractions;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Startup;

public sealed class DemoDataSeeder(IDbConnectionFactory connections, IPasswordHasher hasher, TimeProvider clock)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var hash = hasher.Hash("Demo@1234!");
        var now = clock.GetUtcNow();
        await using var connection = await connections.CreateConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            DECLARE @lockResult int;
            EXEC @lockResult = sys.sp_getapplock @Resource = N'TaskFlow:demo-seed', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 60000;
            IF @lockResult < 0 THROW 50001, 'Could not acquire demo seeding lock.', 1;
            IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = @email)
                INSERT INTO dbo.Users (Id, FullName, Email, PasswordHash, CreatedAt) VALUES (NEWID(), @name, @email, @hash, @now);
            DECLARE @userId uniqueidentifier = (SELECT Id FROM dbo.Users WHERE Email = @email);
            DECLARE @demo TABLE (Title varchar(120), Description varchar(8000), Status varchar(32), DaysOffset int);
            INSERT INTO @demo VALUES
                ('Prepare TaskFlow architecture review', 'Finalize Clean Architecture diagrams and SQL schema', 'InProgress', 2),
                ('Write unit tests for Auth Use Cases', 'Cover register/login happy paths and edge cases', 'Pending', 4),
                ('Record GenAI assessment walkthrough', 'Document prompt, output, and manual review', 'Pending', 7),
                ('Configure local SQL Server Express', 'Verify local SQL Server Express connectivity', 'Completed', -1),
                ('Retire legacy spreadsheet tracker', 'Superseded by TaskFlow', 'Cancelled', -3);
            INSERT INTO dbo.Tasks (Id, UserId, Title, Description, Status, DueDate, CreatedAt, UpdatedAt)
                SELECT NEWID(), @userId, d.Title, d.Description, d.Status, DATEADD(day, d.DaysOffset, @now), @now, @now
                FROM @demo d WHERE NOT EXISTS (SELECT 1 FROM dbo.Tasks t WHERE t.UserId = @userId AND t.Title = d.Title);
            COMMIT TRANSACTION;
            """;
        command.AddParameter("@email", SqlDbType.VarChar, "demo@taskflow.dev", 255);
        command.AddParameter("@name", SqlDbType.VarChar, "Demo User", 150);
        command.AddParameter("@hash", SqlDbType.NVarChar, hash, 512);
        command.AddParameter("@now", SqlDbType.DateTimeOffset, now);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
