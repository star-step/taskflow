using System.ComponentModel.DataAnnotations;
using TaskFlow.Shared.Contracts.Tasks;
using TaskFlow.Shared.Enums;

namespace TaskFlow.Shared.Tests;

public sealed class ValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void DefinedEnum_RejectsUndeclaredValue()
    {
        var results = Validate(new CreateTaskRequest { Title = "Ok", Priority = (TaskPriority)99 });

        var error = Assert.Single(results);
        Assert.Equal(new[] { "Priority" }, error.MemberNames);
        Assert.Equal("Priority must be one of: Low, Medium, High, Critical.", error.ErrorMessage);
    }

    [Fact]
    public void CreateTaskRequest_RequiresTitleAndPriority()
    {
        var results = Validate(new CreateTaskRequest { Title = "   " });

        Assert.Contains(results, r => r.ErrorMessage == "Title is required.");
        Assert.Contains(results, r => r.ErrorMessage == "Priority is required.");
    }

    [Fact]
    public void CreateTaskRequest_RejectsTitleOver200Characters()
    {
        var results = Validate(new CreateTaskRequest { Title = new string('x', 201), Priority = TaskPriority.Low });

        Assert.Equal("Title must be 200 characters or fewer.", Assert.Single(results).ErrorMessage);
    }

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        Assert.Empty(Validate(new CreateTaskRequest { Title = "Ship it", Priority = TaskPriority.High }));
    }
}
