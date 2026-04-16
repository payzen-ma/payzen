using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;

namespace Payzen.Application.Interfaces;

/// <summary>
/// Analyse et simulation de paie via LLM (Claude API).
/// Miroir exact de IClaudeService + IClaudeSimulationService du source.
/// Implémenté en Phase 3 par ClaudeService (HttpClient → api.anthropic.com).
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Analyse une fiche de paie et retourne une explication narrative.
    /// Miroir de IClaudeService.AnalyseSalarieAsync.
    /// </summary>
    Task<string> AnalyseSalarieAsync(
        string regleContent,
        EmployeePayrollDto payrollData,
        string instruction,
        CancellationToken ct = default
    );

    /// <summary>
    /// Simule des compositions de salaire (ex: "Je veux un net de 10 000 DH").
    /// Miroir de IClaudeSimulationService.SimulationSalaryAsync.
    /// </summary>
    Task<string> SimulationSalaryAsync(string regleContent, string instruction, CancellationToken ct = default);

    /// <summary>
    /// Même simulation mais via HTTP non-streaming.
    /// Miroir de IClaudeSimulationService.SimulationSalaryStreamAsync.
    /// </summary>
    Task<string> SimulationSalaryStreamAsync(string regleContent, string instruction, CancellationToken ct = default);

    /// <summary>Simulation rapide (réponse courte).</summary>
    Task<string> SimulateQuickAsync(string regleContent, string instruction, CancellationToken ct = default);

    /// <summary>Retourne les règles de paie utilisables par le LLM.</summary>
    Task<string> GetRulesAsync(CancellationToken ct = default);
}
