namespace NCcsds.Core.Interfaces;

/// <summary>
/// Interface for validating data or objects.
/// </summary>
/// <typeparam name="T">The type to validate.</typeparam>
public interface IValidator<T>
{
    /// <summary>
    /// Validates the value.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns>A validation result.</returns>
    ValidationResult Validate(T value);
}

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public readonly struct ValidationResult
{
    /// <summary>
    /// Gets whether the validation passed.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the validation errors if any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    private ValidationResult(bool isValid, IReadOnlyList<string>? errors = null)
    {
        IsValid = isValid;
        Errors = errors ?? Array.Empty<string>();
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Valid() => new(true);

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    public static ValidationResult Invalid(string error) => new(false, new[] { error });

    /// <summary>
    /// Creates a failed validation result with multiple errors.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    public static ValidationResult Invalid(IReadOnlyList<string> errors) => new(false, errors);

    /// <summary>
    /// Creates a failed validation result with multiple errors.
    /// </summary>
    /// <param name="errors">The error messages.</param>
    public static ValidationResult Invalid(params string[] errors) => new(false, errors);

    /// <summary>
    /// Combines multiple validation results.
    /// </summary>
    public static ValidationResult Combine(params ValidationResult[] results)
    {
        var errors = new List<string>();
        foreach (var result in results)
        {
            if (!result.IsValid)
                errors.AddRange(result.Errors);
        }
        return errors.Count == 0 ? Valid() : Invalid(errors);
    }

    public override string ToString() =>
        IsValid ? "Valid" : $"Invalid: {string.Join(", ", Errors)}";
}
