namespace Payzen.Application.Payroll;

/// <summary>
/// Constantes métier issues du DSL PAYZEN regles_paie.txt v3.1
/// CNSS Décret 2.25.266 (2025), CGI Art.59, Code du Travail Maroc
/// </summary>
public static class PayrollConstants
{
    public const int WorkDaysRef = 26;
    public const int WorkHoursRef = 191;
    public const decimal SmigHoraire = 17.10m;
    public const int RoundingDecimals = 2;

    // CNSS
    public const decimal PLAFOND_CNSS_MENSUEL = 6000.00m;
    public const decimal CNSS_RG_SALARIAL = 0.0448m;
    public const decimal CNSS_RG_PATRONAL = 0.0898m;
    public const decimal CNSS_AMO_SALARIAL = 0.0226m;
    public const decimal CNSS_AMO_PATRONAL = 0.0226m;
    public const decimal CNSS_AMO_PARTICIPATION_PAT = 0.0185m;
    public const decimal CNSS_ALLOC_FAM_PAT = 0.0640m;
    public const decimal CNSS_FP_PAT = 0.0160m;

    // Plafonds Indemnités Non Imposables
    public const decimal PLAFOND_NI_TRANSPORT = 500.00m;
    public const decimal PLAFOND_NI_TRANSPORT_HU = 750.00m;
    public const decimal PLAFOND_NI_TOURNEE = 1500.00m;
    public const decimal PLAFOND_NI_REPRESENTATION = 0.10m;   // 10% du salaire
    public const decimal PLAFOND_NI_PANIER_JOUR = 34.20m;
    public const decimal PLAFOND_NI_CAISSE = 239.00m;
    public const decimal PLAFOND_NI_CAISSE_DGI = 190.00m;
    public const decimal PLAFOND_NI_LAIT = 196.00m;
    public const decimal PLAFOND_NI_LAIT_DGI = 150.00m;
    public const decimal PLAFOND_NI_OUTILLAGE = 119.00m;
    public const decimal PLAFOND_NI_OUTILLAGE_DGI = 100.00m;
    public const decimal PLAFOND_NI_SALISSURE = 239.00m;
    public const decimal PLAFOND_NI_SALISSURE_DGI = 210.00m;
    public const decimal PLAFOND_NI_GRATIF_ANNUEL = 5000.00m;
    public const decimal PLAFOND_NI_GRATIF_DGI = 2500.00m;

    // IR
    public const decimal IR_DEDUCTION_FAMILLE = 30.00m;

    // Frais professionnels (MODULE 08)
    public const decimal FP_SEUIL_35 = 6500.00m;
    public const decimal FP_TAUX_35 = 0.35m;
    public const decimal FP_PLAFOND_35 = 2916.67m;
    public const decimal FP_TAUX_25 = 0.25m;
    public const decimal FP_PLAFOND_25 = 2916.67m;

    /// <summary>Barème IR mensuel 2026 (RNI_min, RNI_max, taux, déduction mensuelle)</summary>
    public static readonly (decimal RniMin, decimal RniMax, decimal Taux, decimal Deduction)[] BaremeIRMensuel2026 =
    {
        (0,          3333.33m,        0.00m,  0.00m),
        (3333.34m,   5000.00m,        0.10m,  333.33m),
        (5000.01m,   6666.67m,        0.20m,  833.33m),
        (6666.68m,   8333.33m,        0.30m,  1500.00m),
        (8333.34m,   15000.00m,       0.34m,  1833.33m),
        (15000.01m,  decimal.MaxValue,0.37m,  2283.33m),
    };
}
