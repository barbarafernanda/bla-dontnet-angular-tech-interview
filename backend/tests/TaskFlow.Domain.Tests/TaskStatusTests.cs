using FluentAssertions;
using TaskFlow.Domain.Enums;
using TaskFlow.Domain.Exceptions;
using TaskStatus = TaskFlow.Domain.Enums.TaskStatus;

namespace TaskFlow.Domain.Tests;

public class TaskStatusTests
{
    [Fact]
    public void TaskStatus_ContainsExactlyTheSpecifiedValues()
    {
        Enum.GetNames<TaskStatus>().Should().BeEquivalentTo("Pending", "InProgress", "Completed", "Cancelled");
    }

    [Theory]
    [InlineData("Pending", TaskStatus.Pending)]
    [InlineData("InProgress", TaskStatus.InProgress)]
    [InlineData("Completed", TaskStatus.Completed)]
    [InlineData("Cancelled", TaskStatus.Cancelled)]
    public void Parse_WithKnownStatus_ReturnsCorrespondingValue(string value, TaskStatus expected)
    {
        var status = TaskStatusParser.Parse(value);

        status.Should().Be(expected);
    }

    [Theory]
    [InlineData("Blocked")]
    [InlineData("")]
    public void Parse_WithUnknownStatus_ThrowsInvalidTaskStatusException(string value)
    {
        var act = () => TaskStatusParser.Parse(value);

        act.Should().Throw<InvalidTaskStatusException>();
    }
}
