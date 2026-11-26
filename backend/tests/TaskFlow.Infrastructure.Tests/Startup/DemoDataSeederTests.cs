using FluentAssertions;
using NSubstitute;
using TaskFlow.Application.Abstractions;
using TaskFlow.Infrastructure.Startup;
using TaskFlow.Infrastructure.Tests.TestDoubles;

namespace TaskFlow.Infrastructure.Tests.Startup;

public class DemoDataSeederTests
{
    [Fact]
    public async Task SeedTwice_UsesGuardedParameterizedStatementsAndNeverStoresPlaintextPassword()
    {
        var database = new RecordingDatabase();
        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Hash("Demo@1234!").Returns("encoded-demo-hash");
        var seeder = new DemoDataSeeder(database, hasher, TimeProvider.System);

        await seeder.SeedAsync();
        var firstRun = database.Commands.ToArray();
        await seeder.SeedAsync();
        var secondRun = database.Commands.Skip(firstRun.Length).ToArray();

        firstRun.Should().NotBeEmpty();
        secondRun.Should().NotBeEmpty();
        foreach (var run in new[] { firstRun, secondRun })
        {
            var sql = string.Join("\n", run.Select(c => c.Text));
            sql.Should().ContainEquivalentOf("IF NOT EXISTS");
            sql.Should().ContainEquivalentOf("dbo.Users").And.ContainEquivalentOf("dbo.Tasks");
            sql.Should().NotContain("Demo@1234!");
            run.SelectMany(c => c.Parameters).Should().NotContain(p => Equals(p.Value, "Demo@1234!"));
        }
        hasher.Received().Hash("Demo@1234!");
    }
}
