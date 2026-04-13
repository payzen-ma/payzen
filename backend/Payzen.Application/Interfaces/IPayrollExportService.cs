using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;

namespace Payzen.Application.Interfaces;

/// <summary>
/// Génération des états réglementaires mensuels.
/// Miroir exact de IPayrollExportService du source.
/// Implémenté en Phase 3 par PayrollExportService (AppDbContext + IronPDF + CsvHelper).
/// </summary>
public interface IPayrollExportService
{
    Task<ServiceResult<List<JournalPaieRow>>> GetJournalPaieAsync(int companyId, int year, int month, CancellationToken ct = default);
    Task<ServiceResult<List<EtatCnssRow>>> GetEtatCnssAsync(int companyId, int year, int month, CancellationToken ct = default);
    Task<ServiceResult<EtatCnssPdfData>> GetEtatCnssPdfDataAsync(int companyId, int year, int month, CancellationToken ct = default);
    Task<ServiceResult<List<EtatIrRow>>> GetEtatIrAsync(int companyId, int year, int month, CancellationToken ct = default);
    Task<ServiceResult<EtatIrPdfData>> GetEtatIrPdfDataAsync(int companyId, int year, int month, CancellationToken ct = default);
}
