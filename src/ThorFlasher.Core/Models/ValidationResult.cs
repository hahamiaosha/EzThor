namespace ThorFlasher.Core.Models;

public sealed class ValidationResult
{
    public static ValidationResult Success() => new([]);

    public static ValidationResult Failure(params string[] errors) => new(errors);

    public ValidationResult(IEnumerable<string> errors)
    {
        Errors = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<string> Errors { get; }
}
