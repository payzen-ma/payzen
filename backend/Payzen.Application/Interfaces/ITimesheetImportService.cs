using Payzen.Application.Common;
using Payzen.Application.DTOs.Employee;

namespace Payzen.Application.Interfaces;

/// <summary>
/// Import de pointage (XLSX/CSV) et lecture des pointages par période.
/// Toute la logique métier et l'accès aux données sont dans l'implémentation (Infrastructure).
/// </summary>
public interface ITimesheetImportService
{
    /// <summary>
    /// Importe un fichier de pointage (Sage) et persiste les heures par employé pour la période.
    /// </summary>
    /// <param name="fileStream">Contenu du fichier (XLSX ou CSV)</param>
    /// <param name="fileName">Nom du fichier (pour détecter l'extension)</param>
    /// <param name="month">Mois (1-12)</param>
    /// <param name="year">Année</param>
    /// <param name="mode">monthly ou bi_monthly</param>
    /// <param name="half">1 ou 2 si bi_monthly</param>
    /// <param name="companyId">Société cible (optionnel si userId fourni et employé associé)</param>
    /// <param name="userId">Utilisateur courant (optionnel)</param>
    Task<ServiceResult<TimesheetImportResultDto>> ImportFromFileAsync(
        Stream fileStream,
        string fileName,
        int month,
        int year,
        string mode,
        int? half,
        int? companyId,
        int? userId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Récupère les pointages d'une société pour un mois donné.
    /// </summary>
    Task<ServiceResult<IEnumerable<EmployeeAttendanceReadDto>>> GetTimesheetsAsync(
        int month,
        int year,
        int? companyId,
        int? userId,
        CancellationToken ct = default
    );
}
