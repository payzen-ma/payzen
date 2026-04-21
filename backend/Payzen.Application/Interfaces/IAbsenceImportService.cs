using Payzen.Application.Common;
using Payzen.Application.DTOs.Employee;

namespace Payzen.Application.Interfaces;

/// <summary>
/// Service d'importation des absences depuis fichiers XLSX/CSV.
/// </summary>
public interface IAbsenceImportService
{
    /// <summary>
    /// Importe les absences depuis un fichier et les enregistre en base.
    /// </summary>
    /// <param name="fileStream">Contenu du fichier</param>
    /// <param name="fileName">Nom du fichier</param>
    /// <param name="userId">Utilisateur qui fait l'import</param>
    /// <param name="ct">Token d'annulation</param>
    Task<ServiceResult<AbsenceImportResultDto>> ImportAbsencesFromFileAsync(
        Stream fileStream,
        string fileName,
        int? userId,
        CancellationToken ct = default
    );
}