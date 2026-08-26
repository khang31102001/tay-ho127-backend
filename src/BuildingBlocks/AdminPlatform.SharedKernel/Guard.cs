namespace AdminPlatform.SharedKernel;

/// <summary>Small set of guard clauses for domain invariants. Throws domain exceptions, not framework ones.</summary>
public static class Guard
{
    public static string NotNullOrWhiteSpace(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleValidationException($"{fieldName} must not be empty.");
        }

        return value;
    }

    public static T NotNull<T>(T? value, string fieldName) where T : class
    {
        if (value is null)
        {
            throw new BusinessRuleValidationException($"{fieldName} must not be null.");
        }

        return value;
    }

    public static Guid NotEmpty(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new BusinessRuleValidationException($"{fieldName} must not be empty.");
        }

        return value;
    }
}
