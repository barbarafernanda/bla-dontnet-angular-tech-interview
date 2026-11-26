using System.Data;
using FluentAssertions;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;
using TaskFlow.Infrastructure.Tests.TestDoubles;

namespace TaskFlow.Infrastructure.Tests.Data;

public class SqlUserRepositoryTests
{
    private static readonly Guid Id = Guid.Parse("c5bd846e-dccb-4f07-8b5a-d601e3491594");
    private static readonly DateTimeOffset Created = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddAsync_BindsValuesWithExplicitSqlTypesAndDisposesResources()
    {
        var database = new RecordingDatabase();
        var user = new User(Id, "Demo User", "demo@taskflow.dev", "encoded-password-value", Created);

        await new SqlUserRepository(database).AddAsync(user);

        var command = database.Commands.Should().ContainSingle().Subject;
        command.Text.Should().ContainEquivalentOf("INSERT");
        command.Text.Should().NotContain(user.Email).And.NotContain(user.PasswordHash);
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.UniqueIdentifier && Equals(p.Value, Id));
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.VarChar && p.Size == 150 && Equals(p.Value, user.FullName));
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.VarChar && p.Size == 255 && Equals(p.Value, user.Email));
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.NVarChar && p.Size == 512 && Equals(p.Value, user.PasswordHash));
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.DateTimeOffset && Equals(p.Value, Created));
        database.DisposedCommands.Should().Be(1);
        database.DisposedConnections.Should().Be(1);
    }

    [Fact]
    public async Task GetByEmailAsync_MapsReaderAndBindsEmail()
    {
        var database = new RecordingDatabase { Reader = () => UsersTable(true).CreateDataReader() };

        var result = await new SqlUserRepository(database).GetByEmailAsync("demo@taskflow.dev");

        result.Should().NotBeNull();
        result!.Id.Should().Be(Id);
        result.FullName.Should().Be("Demo User");
        result.Email.Should().Be("demo@taskflow.dev");
        result.PasswordHash.Should().Be("hash");
        result.CreatedAt.Should().Be(Created);
        database.Commands.Should().ContainSingle().Which.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.VarChar && p.Size == 255 && Equals(p.Value, "demo@taskflow.dev"));
    }

    [Fact]
    public async Task GetByEmailAsync_WhenMissing_ReturnsNull()
    {
        var database = new RecordingDatabase { Reader = () => UsersTable(false).CreateDataReader() };
        (await new SqlUserRepository(database).GetByEmailAsync("missing@taskflow.dev")).Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_WhenExecutionFails_PropagatesErrorAndDisposesResources()
    {
        var error = new InvalidOperationException("Database failure");
        var database = new RecordingDatabase { Error = error };
        var act = () => new SqlUserRepository(database).AddAsync(new User(Id, "Demo User", "demo@taskflow.dev", "hash", Created));

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(error);
        database.DisposedConnections.Should().Be(1);
        database.DisposedCommands.Should().Be(1);
    }

    [Theory]
    [InlineData(2601)]
    [InlineData(2627)]
    public async Task AddAsync_WhenEmailIsDuplicate_MapsSqlErrorToDomainConflict(int number)
    {
        var error = SqlErrors.Create(number, "Duplicate key in UX_Users_Email");
        var database = new RecordingDatabase { Error = error };
        var act = () => new SqlUserRepository(database).AddAsync(new User(Id, "Demo User", "demo@taskflow.dev", "hash", Created));

        var exception = (await act.Should().ThrowAsync<TaskFlow.Domain.Exceptions.DuplicateEmailException>()).Which;
        exception.InnerException.Should().BeSameAs(error);
    }

    private static DataTable UsersTable(bool includeRow)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("FullName", typeof(string));
        table.Columns.Add("Email", typeof(string));
        table.Columns.Add("PasswordHash", typeof(string));
        table.Columns.Add("CreatedAt", typeof(DateTimeOffset));
        if (includeRow) table.Rows.Add(Id, "Demo User", "demo@taskflow.dev", "hash", Created);
        return table;
    }
}
