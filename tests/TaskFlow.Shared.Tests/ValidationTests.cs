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

public sealed class RegisterRequestValidationTests
{
    private static List<string?> Errors(TaskFlow.Shared.Contracts.Auth.RegisterRequest model)
    {
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(model,
            new System.ComponentModel.DataAnnotations.ValidationContext(model), results, validateAllProperties: true);
        return results.Select(r => r.ErrorMessage).ToList();
    }

    private static TaskFlow.Shared.Contracts.Auth.RegisterRequest Valid(string password) =>
        new() { DisplayName = "Jordan Ellis", Email = "jordan@company.com", Password = password };

    [Fact]
    public void Password_TooShort_And_TooLong_GetTheirOwnMessages()
    {
        Assert.Equal(new[] { "Password must be at least 12 characters." }, Errors(Valid(new string('a', 11))));
        Assert.Equal(new[] { "Password must be 128 characters or fewer." }, Errors(Valid(new string('a', 129))));
        Assert.Empty(Errors(Valid(new string('a', 128))));
    }
}
