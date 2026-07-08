using System.ComponentModel.DataAnnotations;

namespace DukkanPilot.Web.Validation;

/// <summary>
/// Validates that a checkbox boolean is explicitly true.
/// Prefer this over [Range(typeof(bool), "true", "true")] which is unreliable for bool binding.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class RequiredTrueAttribute : ValidationAttribute
{
    public RequiredTrueAttribute()
        : base("Bu alanı onaylamanız gerekir.")
    {
    }

    public override bool IsValid(object? value)
    {
        return value is bool boolean && boolean;
    }
}
