using FluentAssertions;
using TaskFlow.Infrastructure.Startup;
using TaskFlow.Infrastructure.Tests.TestDoubles;

namespace TaskFlow.Infrastructure.Tests.Startup;

public class SqlSchemaInitializerTests
{
    [Fact]
    public async Task InitializeTwice_UsesIdempotentDatabaseAndTableGuards()
    {
        var database = new RecordingDatabase();
        var initializer = new SqlSchemaInitializer(database);

        await initializer.InitializeAsync();
        var firstRun = database.Commands.Select(c => c.Text).ToArray();
        await initializer.InitializeAsync();
        var secondRun = database.Commands.Skip(firstRun.Length).Select(c => c.Text).ToArray();

        firstRun.Should().NotBeEmpty();
        secondRun.Should().Equal(firstRun);
        var sql = string.Join("\n", firstRun);
        sql.Should().ContainEquivalentOf("DB_ID").And.ContainEquivalentOf("OBJECT_ID");
        sql.Should().ContainEquivalentOf("dbo.Users").And.ContainEquivalentOf("dbo.Tasks");
        sql.Should().ContainEquivalentOf("CREATE TABLE").And.ContainEquivalentOf("CREATE DATABASE");
    }
}
