namespace payzen_backend.Services.Validation
{
    /// <summary>
    /// Result of validation operation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public static ValidationResult Success() => new() { IsValid = true };

        public static ValidationResult Failure(params string[] errors) => new()
        {
            IsValid = false,
            Errors = errors.ToList()
        };

        public void AddError(string error)
        {
            Errors.Add(error);
            IsValid = false;
        }

        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
    }
}
