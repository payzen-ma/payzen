using payzen_backend.DTOs.Payroll;

namespace payzen_backend.Services.Payroll
{
    public interface IPayrollExportService
    {
        Task<List<JournalPaieRow>> GetJournalPaie(int companyId, int year, int month);
        Task<List<EtatCnssRow>> GetEtatCnss(int companyId, int year, int month);
        Task<List<EtatIrRow>> GetEtatIr(int companyId, int year, int month);
    }
}
