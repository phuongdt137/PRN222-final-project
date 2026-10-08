using System.ComponentModel.DataAnnotations;

namespace HighSchoolManagementSystem.Services.Common;

[AttributeUsage(AttributeTargets.Property)]
public sealed class MinimumAgeAttribute(int years, string? referenceDateProperty = null) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Date of birth remains optional, as in the existing profile forms.
        if (value == null) return ValidationResult.Success;
        var referenceDate = referenceDateProperty == null ? SchoolDate.Today
            : validationContext.ObjectType.GetProperty(referenceDateProperty)?.GetValue(validationContext.ObjectInstance) as DateTime? ?? SchoolDate.Today;
        if (value is DateTime birthDate && referenceDate.Year > years
            && birthDate.Date <= referenceDate.Date.AddYears(-years))
            return ValidationResult.Success;
        return new ValidationResult(FormatErrorMessage(validationContext.DisplayName),
            validationContext.MemberName == null ? null : new[] { validationContext.MemberName });
    }
}
