using System;
using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Shared.Validation;

/// <summary>
/// Rejects enum values that are not declared members. Without this, a request such as
/// <c>{"priority": 99}</c> binds successfully to <c>(TaskPriority)99</c>.
/// A null value is treated as valid; combine with <see cref="RequiredAttribute"/> when the field is mandatory.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class DefinedEnumAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        var type = value.GetType();
        if (type.IsEnum && Enum.IsDefined(type, value))
        {
            return ValidationResult.Success;
        }

        var allowed = type.IsEnum ? string.Join(", ", Enum.GetNames(type)) : string.Empty;
        var members = validationContext.MemberName is null ? null : new[] { validationContext.MemberName };
        return new ValidationResult($"{validationContext.DisplayName} must be one of: {allowed}.", members);
    }
}
