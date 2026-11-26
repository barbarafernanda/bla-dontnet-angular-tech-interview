using System.Data;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Startup;

public sealed class SqlSchemaInitializer(IDbConnectionFactory connections, string databaseName = "TaskFlowDb")
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(databaseName) || databaseName.Length > 128)
            throw new ArgumentException("A database name of at most 128 characters is required.", nameof(databaseName));

        await using var connection = await connections.CreateConnectionAsync(cancellationToken);
        await connection.ChangeDatabaseAsync("master", cancellationToken);
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource = @lockName, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 60000;
                IF @result < 0 THROW 50000, 'Could not acquire database initialization lock.', 1;
                IF DB_ID(@databaseName) IS NULL
                BEGIN
                    DECLARE @sql nvarchar(300) = N'CREATE DATABASE ' + QUOTENAME(@databaseName);
                    EXEC (@sql);
                END
                EXEC sys.sp_releaseapplock @Resource = @lockName, @LockOwner = 'Session';
                """;
            command.AddParameter("@databaseName", SqlDbType.NVarChar, databaseName, 128);
            command.AddParameter("@lockName", SqlDbType.NVarChar, "TaskFlow:initialize:" + databaseName, 255);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await connection.ChangeDatabaseAsync(databaseName, cancellationToken);
        await using var schema = connection.CreateCommand();
        schema.CommandText = """
            IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Users (Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
                    FullName VARCHAR(150) NOT NULL, Email VARCHAR(255) COLLATE Latin1_General_100_CI_AS NOT NULL,
                    PasswordHash NVARCHAR(512) NOT NULL, CreatedAt DATETIMEOFFSET NOT NULL);
                CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users (Email);
            END;
            IF OBJECT_ID(N'dbo.Tasks', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Tasks (Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Tasks PRIMARY KEY,
                    UserId UNIQUEIDENTIFIER NOT NULL, Title VARCHAR(120) NOT NULL, Description VARCHAR(8000) NULL,
                    Status VARCHAR(32) NOT NULL, DueDate DATETIMEOFFSET NOT NULL, CreatedAt DATETIMEOFFSET NOT NULL,
                    UpdatedAt DATETIMEOFFSET NOT NULL, CONSTRAINT FK_Tasks_Users FOREIGN KEY (UserId)
                    REFERENCES dbo.Users (Id) ON DELETE CASCADE);
                CREATE INDEX IX_Tasks_UserId ON dbo.Tasks (UserId);
            END;
            """;
        await schema.ExecuteNonQueryAsync(cancellationToken);
    }
}
