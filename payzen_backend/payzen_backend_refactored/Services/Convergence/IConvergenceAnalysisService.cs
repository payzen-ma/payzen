using payzen_backend.Models.Payroll.Referentiel;

namespace payzen_backend.Services.Convergence
{
    /// <summary>
    /// Service for analyzing convergence/divergence between CNSS and DGI rules
    /// </summary>
    public interface IConvergenceAnalysisService
    {
        /// <summary>
        /// Analyze convergence for a specific element at a given date
        /// </summary>
        /// <param name="elementId">Element to analyze</param>
        /// <param name="asOfDate">Date for historical analysis (null = today)</param>
        /// <returns>Detailed convergence analysis</returns>
        Task<ConvergenceResult> AnalyzeElementAsync(int elementId, DateOnly? asOfDate = null);

        /// <summary>
        /// Compare two rules to determine if they are convergent
        /// </summary>
        /// <param name="cnssRule">CNSS rule</param>
        /// <param name="dgiRule">DGI rule</param>
        /// <returns>True if rules match, false otherwise</returns>
        bool AreRulesConvergent(ElementRule? cnssRule, ElementRule? dgiRule);

        /// <summary>
        /// Get detailed differences between two rules
        /// </summary>
        /// <param name="cnssRule">CNSS rule</param>
        /// <param name="dgiRule">DGI rule</param>
        /// <returns>List of field comparisons showing differences</returns>
        List<FieldComparison> GetDivergenceDetails(ElementRule cnssRule, ElementRule dgiRule);

        /// <summary>
        /// Recalculate convergence for all active elements
        /// </summary>
        /// <returns>Number of elements updated</returns>
        Task<int> RecalculateAllConvergenceAsync();

        /// <summary>
        /// Recalculate convergence for a single element
        /// </summary>
        /// <param name="elementId">Element ID to recalculate</param>
        /// <returns>True if convergence status changed</returns>
        Task<bool> RecalculateElementConvergenceAsync(int elementId);
    }
}
