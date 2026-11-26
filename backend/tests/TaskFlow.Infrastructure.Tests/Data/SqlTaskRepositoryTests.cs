using System.Data;
using FluentAssertions;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;
using TaskFlow.Infrastructure.Tests.TestDoubles;
using TaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Infrastructure.Tests.Data;

public class SqlTaskRepositoryTests
{
    private static readonly Guid Id = Guid.Parse("b5727ac9-3159-42f3-a127-7a8a73454c31");
    private static readonly Guid Owner = Guid.Parse("c5bd846e-dccb-4f07-8b5a-d601e3491594");
    private static readonly DateTimeOffset Created = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);
    private static TaskItem Item() => new(Id, Owner, "Title", null, TaskStatus.Pending, Created.AddDays(1), Created, Created);

    [Fact]
    public async Task AddAsync_BindsTypedValuesAndNullDescription()
    {
        var database = new RecordingDatabase();
        await new SqlTaskRepository(database).AddAsync(Item());

        var command = database.Commands.Should().ContainSingle().Subject;
        command.Text.Should().ContainEquivalentOf("INSERT").And.NotContain("'Title'");
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.UniqueIdentifier && Equals(p.Value, Id));
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.UniqueIdentifier && Equals(p.Value, Owner));
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.VarChar && p.Size == 120 && Equals(p.Value, "Title"));
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.VarChar && p.Size == 8000 && p.Value == DBNull.Value);
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.VarChar && p.Size == 32 && Equals(p.Value, "Pending"));
    }

    [Fact]
    public async Task ListByUserIdAsync_BindsOwnerAndMapsNullableDescription()
    {
        var database = new RecordingDatabase { Reader = () => TasksTable(true).CreateDataReader() };
        var result = await new SqlTaskRepository(database).ListByUserIdAsync(Owner);

        var item = result.Should().ContainSingle().Subject;
        item.Id.Should().Be(Id);
        item.UserId.Should().Be(Owner);
        item.Title.Should().Be("Title");
        item.Description.Should().BeNull();
        item.Status.Should().Be(TaskStatus.Pending);
        item.DueDate.Should().Be(Created.AddDays(1));
        item.CreatedAt.Should().Be(Created);
        database.Commands.Should().ContainSingle().Which.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.UniqueIdentifier && Equals(p.Value, Owner));
        database.Commands[0].Text.Should().MatchRegex("(?i)where[\\s\\S]*userid[\\s\\S]*@");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNoRow_ReturnsNullAndBindsBothIds()
    {
        var database = new RecordingDatabase { Reader = () => TasksTable(false).CreateDataReader() };
        var result = await new SqlTaskRepository(database).GetByIdAsync(Id, Owner);

        result.Should().BeNull();
        var command = database.Commands.Should().ContainSingle().Subject;
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.UniqueIdentifier && Equals(p.Value, Id));
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.UniqueIdentifier && Equals(p.Value, Owner));
        command.Text.Should().MatchRegex("(?i)where[\\s\\S]*userid");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateAsync_UsesOwnerAndReturnsWhetherRowWasAffected(bool exists)
    {
        var database = new RecordingDatabase { RowsAffected = exists ? 1 : 0 };
        var result = await new SqlTaskRepository(database).UpdateAsync(Item(), Owner);

        result.Should().Be(exists);
        var command = database.Commands.Should().ContainSingle().Subject;
        command.Text.Should().ContainEquivalentOf("UPDATE").And.MatchRegex("(?i)where[\\s\\S]*userid");
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.UniqueIdentifier && Equals(p.Value, Owner));
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.UniqueIdentifier && Equals(p.Value, Id));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteAsync_UsesOwnerAndReturnsWhetherRowWasAffected(bool exists)
    {
        var database = new RecordingDatabase { RowsAffected = exists ? 1 : 0 };
        var result = await new SqlTaskRepository(database).DeleteAsync(Id, Owner);

        result.Should().Be(exists);
        var command = database.Commands.Should().ContainSingle().Subject;
        command.Text.Should().ContainEquivalentOf("DELETE").And.MatchRegex("(?i)where[\\s\\S]*userid");
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.UniqueIdentifier && Equals(p.Value, Owner));
        command.Parameters.Should().Contain(p => p.SqlDbType == SqlDbType.UniqueIdentifier && Equals(p.Value, Id));
    }

    private static DataTable TasksTable(bool includeRow)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("UserId", typeof(Guid));
        table.Columns.Add("Title", typeof(string));
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("Status", typeof(string));
        table.Columns.Add("DueDate", typeof(DateTimeOffset));
        table.Columns.Add("CreatedAt", typeof(DateTimeOffset));
        table.Columns.Add("UpdatedAt", typeof(DateTimeOffset));
        if (includeRow) table.Rows.Add(Id, Owner, "Title", DBNull.Value, "Pending", Created.AddDays(1), Created, Created);
        return table;
    }
}
