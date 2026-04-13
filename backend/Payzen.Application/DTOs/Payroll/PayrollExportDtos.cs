namespace Payzen.Application.DTOs.Payroll;

/// <summary>
/// Ligne du Journal de Paie (export mensuel complet)
/// </summary>
public class JournalPaieRow
{
    // Ligne 1 - Informations principales
    public string Matricule { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string DateNaissance { get; set; } = string.Empty;
    public int NbrJrsTravailles
    {
        get; set;
    }
    public decimal JrsFeries
    {
        get; set;
    }
    public decimal SBPlusConge
    {
        get; set;
    }
    public decimal SalaireBaseDuMois
    {
        get; set;
    }
    public int NbrDeductions
    {
        get; set;
    }
    public decimal CNSSPartSalariale
    {
        get; set;
    }
    public decimal AMOPartSalariale
    {
        get; set;
    }
    public decimal FraisProfesADeduireM
    {
        get; set;
    }
    public decimal IRAPayer
    {
        get; set;
    }
    public decimal SalaireNet
    {
        get; set;
    }
    public decimal InteretLogement
    {
        get; set;
    }
    public decimal HeuresNormales
    {
        get; set;
    }
    public decimal HeuresSup50
    {
        get; set;
    }

    // Ligne 2 - Informations complémentaires
    public string SF { get; set; } = string.Empty;  // Situation Familiale
    public string CIN { get; set; } = string.Empty;
    public string CNSS { get; set; } = string.Empty;
    public string DateEmbauche { get; set; } = string.Empty;
    public string Fonction { get; set; } = string.Empty;
    public decimal NbrJrsConge
    {
        get; set;
    }
    public decimal MTAnciennete
    {
        get; set;
    }
    public decimal LesPrimesImposables
    {
        get; set;
    }
    public decimal BrutImposable
    {
        get; set;
    }
    public decimal MutuellePartSalariale
    {
        get; set;
    }
    public decimal CIMR
    {
        get; set;
    }
    public decimal NetImposable
    {
        get; set;
    }
    public decimal MtExonere
    {
        get; set;
    }
    public decimal BrutGlobal
    {
        get; set;
    }
    public decimal Avance
    {
        get; set;
    }
    public decimal HeuresSup25
    {
        get; set;
    }
    public decimal HeuresSup100
    {
        get; set;
    }
}

/// <summary>
/// Ligne de l'État CNSS (format Damancom)
/// </summary>
public class EtatCnssRow
{
    public string NomPrenom { get; set; } = string.Empty;
    public string NumeroCnss { get; set; } = string.Empty;
    public decimal SalaireBrutDeclare
    {
        get; set;
    }
    /// <summary>Nombre de jours déclarés (26 par défaut)</summary>
    public int NombreJoursDeclare { get; set; } = 26;
}

/// <summary>
/// Ligne enrichie de l'État CNSS pour export PDF (toutes cotisations détaillées).
/// </summary>
public class EtatCnssFullRow
{
    public int Ordre
    {
        get; set;
    }
    public string NomPrenom { get; set; } = string.Empty;
    public string NumeroCnss { get; set; } = string.Empty;
    public string CIN { get; set; } = string.Empty;
    public int NombreJours { get; set; } = 26;

    /// <summary>Salaire brut total déclaré (non plafonné).</summary>
    public decimal SalaireBrut
    {
        get; set;
    }

    /// <summary>Base CNSS plafonnée (≤ 6 000 MAD).</summary>
    public decimal BaseCnss
    {
        get; set;
    }

    // ── Part salariale ──────────────────────────────────────
    /// <summary>PS salariale 4,48 % × BaseCnss.</summary>
    public decimal RgSalarial
    {
        get; set;
    }
    /// <summary>AMO salariale 2,26 % × SalaireBrut.</summary>
    public decimal AmoSalarial
    {
        get; set;
    }
    public decimal TotalSalarial => RgSalarial + AmoSalarial;

    // ── Part patronale ──────────────────────────────────────
    /// <summary>PS patronale 8,98 % × BaseCnss.</summary>
    public decimal RgPatronal
    {
        get; set;
    }
    /// <summary>Allocations familiales 6,40 % × SalaireBrut.</summary>
    public decimal AfPatronal
    {
        get; set;
    }
    /// <summary>Formation professionnelle 1,60 % × SalaireBrut.</summary>
    public decimal FpPatronal
    {
        get; set;
    }
    /// <summary>AMO patronale (2,26 % + 1,85 %) × SalaireBrut.</summary>
    public decimal AmoPatronal
    {
        get; set;
    }
    public decimal TotalPatronal => RgPatronal + AfPatronal + FpPatronal + AmoPatronal;
    public decimal TotalGeneral => TotalSalarial + TotalPatronal;

    // ── Récapitulatif AMO (pour PDF) ────────────────────────
    /// <summary>Cotisation AMO = sal. 2,26 % + pat. 2,26 % = 4,52 % × SalaireBrut.</summary>
    public decimal CotisationAmo
    {
        get; set;
    }
    /// <summary>Participation AMO patronale supplémentaire 1,85 % × SalaireBrut.</summary>
    public decimal ParticipationAmo
    {
        get; set;
    }
    public decimal TotalAmo => CotisationAmo + ParticipationAmo;

    /// <summary>CNSS hors AMO : PS (sal+pat) + AF + FP.</summary>
    public decimal TotalAPayer => RgSalarial + RgPatronal + AfPatronal + FpPatronal;
}

/// <summary>
/// Jeu de données complet pour le PDF état CNSS (header entreprise + lignes).
/// </summary>
public class EtatCnssPdfData
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyCnss { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public string CompanyIce { get; set; } = string.Empty;
    public int Month
    {
        get; set;
    }
    public int Year
    {
        get; set;
    }
    public List<EtatCnssFullRow> Rows { get; set; } = [];
}

/// <summary>
/// Ligne de l'État IR (Impôt sur le Revenu)
/// </summary>
public class EtatIrRow
{
    public string NomPrenom { get; set; } = string.Empty;
    public string CIN { get; set; } = string.Empty;
    public string CNSS { get; set; } = string.Empty;
    public decimal BrutImposable
    {
        get; set;
    }
    public decimal IRRetenu
    {
        get; set;
    }
}

/// <summary>
/// Ligne enrichie de l'État IR pour export PDF.
/// </summary>
public class EtatIrFullRow
{
    public int Matricule
    {
        get; set;
    }
    public string NomPrenom { get; set; } = string.Empty;
    public decimal SalImposable
    {
        get; set;
    }
    public decimal MontantIGR
    {
        get; set;
    }
}

/// <summary>
/// Jeu de données complet pour le PDF état IR (header entreprise + lignes).
/// </summary>
public class EtatIrPdfData
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public int Month
    {
        get; set;
    }
    public int Year
    {
        get; set;
    }
    public List<EtatIrFullRow> Rows { get; set; } = [];
}
