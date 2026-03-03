namespace payzen_backend.Services.Convergence
{
    /// <summary>
    /// Result of convergence analysis between CNSS and DGI rules
    /// </summary>
    public class ConvergenceResult
    {
        public bool IsConvergent { get; set; }
        public string? Summary { get; set; }
        public List<FieldComparison> Differences { get; set; } = new();
        public int? CnssRuleId { get; set; }
        public int? DgiRuleId { get; set; }
    }

    /// <summary>
    /// Comparison of a specific field between two rules
    /// </summary>
    public class FieldComparison
    {
        public required string FieldName { get; set; }
        public string? CnssValue { get; set; }
        public string? DgiValue { get; set; }
        public bool Matches { get; set; }
    }
}
