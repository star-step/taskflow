using System;
using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Shared.Validation;

/// <summary>
/// Fails when a string is present but contains only whitespace. Null is valid (use [Required] for that).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NotWhiteSpaceAttribute : ValidationAttribute
{
    public NotWhiteSpaceAttribute() : base("The {0} field cannot be blank.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text || !string.IsNullOrWhiteSpace(text);
}
