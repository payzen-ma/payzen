namespace Payzen.Application.DTOs.Employee;

/// <summary>
/// Résultat de l'importation des absences.
/// </summary>
public class AbsenceImportResultDto
{
    /// <summary>
    /// Nombre total de lignes dans le fichier.
    /// </summary>
    public int TotalLines { get; set; }

    /// <summary>
    /// Nombre d'absences importées avec succès.
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Nombre d'erreurs rencontrées.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Liste des absences importées avec succès.
    /// </summary>
    public List<AbsenceImportRowDto> ImportedAbsences { get; set; } = new();

    /// <summary>
    /// Liste des erreurs détaillées.
    /// </summary>
    public List<AbsenceImportErrorDto> Errors { get; set; } = new();

    /// <summary>
    /// Détails des feuilles lues (nom + nombre de lignes lues).
    /// </summary>
    public List<AbsenceImportSheetDto> Sheets { get; set; } = new();

    /// <summary>
    /// Résultat de vérification des employés par ligne (existence + cohérence nom/prénom).
    /// </summary>
    public List<AbsenceEmployeeCheckDto> EmployeeChecks { get; set; } = new();

    /// <summary>
    /// Employés créés automatiquement depuis la feuille Nouveau_employés.
    /// </summary>
    public List<AbsenceCreatedEmployeeDto> AutoCreatedEmployees { get; set; } = new();
}

/// <summary>
/// Ligne importée avec succès.
/// </summary>
public class AbsenceImportRowDto
{
    public int Row { get; set; }
    public string Matricule { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string AbsenceDate { get; set; } = string.Empty;
    public string DurationType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Erreur lors de l'importation d'une absence.
/// </summary>
public class AbsenceImportErrorDto
{
    /// <summary>
    /// Numéro de ligne dans le fichier.
    /// </summary>
    public int Row { get; set; }

    /// <summary>
    /// Matricule de l'employé concerné.
    /// </summary>
    public string? Matricule { get; set; }

    /// <summary>
    /// Message d'erreur descriptif.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Résumé de lecture d'une feuille du fichier importé.
/// </summary>
public class AbsenceImportSheetDto
{
    /// <summary>
    /// Nom de la feuille.
    /// </summary>
    public string SheetName { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de lignes lues dans cette feuille.
    /// </summary>
    public int ReadLines { get; set; }

    /// <summary>
    /// Nombre de lignes effectivement importées depuis cette feuille.
    /// </summary>
    public int ImportedLines { get; set; }
}

/// <summary>
/// Diagnostic de reconnaissance employé pour une ligne importée.
/// </summary>
public class AbsenceEmployeeCheckDto
{
    public int Row { get; set; }
    public string? Matricule { get; set; }
    public string? EmployeeName { get; set; }
    public bool Exists { get; set; }
    public bool IsLastNameMatch { get; set; }
    public bool IsFirstNameMatch { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AbsenceCreatedEmployeeDto
{
    public string Matricule { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}