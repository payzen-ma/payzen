using payzen_backend.Models.Employee.Dtos;

namespace payzen_backend.Services.Llm
{
    /// <summary>
    /// Interface pour le service Claude, permettant le mocking pour les tests
    /// </summary>
    public interface IClaudeService
    {
        Task<string> AnalyseSalarieAsync(
            string regleContent,
            EmployeePayrollDto payrollData,
            string instruction,
            CancellationToken cancellationToken = default);
    }
}
