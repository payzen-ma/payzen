using Payzen.Application.Common;

namespace Payzen.Application.Interfaces;

/// <summary>
/// Génération de documents PDF (bulletins de paie, états fiscaux).
/// Implémenté en Phase 3 par IronPdfDocumentService.
/// </summary>
public interface IDocumentService
{
    Task<ServiceResult<byte[]>> GeneratePayslipAsync(int payrollResultId, CancellationToken ct = default);
    Task<ServiceResult<byte[]>> GeneratePayslipByEmployeePeriodAsync(int employeeId, int year, int month, int? half, CancellationToken ct = default);
    Task<ServiceResult<byte[]>> GenerateEtatCnssPdfAsync(int companyId, int year, int month, CancellationToken ct = default);
    Task<ServiceResult<byte[]>> GenerateEtatIrPdfAsync(int companyId, int year, int month, CancellationToken ct = default);
    Task<ServiceResult<byte[]>> GenerateJournalPaieCsvAsync(int companyId, int year, int month, CancellationToken ct = default);
}
