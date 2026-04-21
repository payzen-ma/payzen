namespace Payzen.Application.Payroll;

// ════════════════════════════════════════════════════════════
// AUDIT STEP
// ════════════════════════════════════════════════════════════

/// <summary>Trace d'une étape du pipeline — persistée en Phase 3.</summary>
public class PayrollAuditStep
{
    public int StepOrder { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string FormulaDescription { get; set; } = string.Empty;
    public string InputsJson { get; set; } = string.Empty;
    public string OutputsJson { get; set; } = string.Empty;
}

// ════════════════════════════════════════════════════════════
// RESULT
// ════════════════════════════════════════════════════════════

/// <summary>
/// Résultat complet du moteur de calcul de paie.
/// Retourné par <see cref="PayrollCalculationEngine.CalculatePayroll"/>.
/// Mappé depuis <see cref="PayrollCalculationContext"/> après exécution du pipeline.
/// Persisté dans <c>PayrollResult</c> (entité Domain) par PayrollService en Phase 3.
/// </summary>
public class PayrollCalculationResult
{
    // ── Identification ──────────────────────────────────────────────────────

    public string EmployeeName { get; set; } = string.Empty;
    public int Month { get; set; }
    public int Year { get; set; }

    // ── Succès / erreur ─────────────────────────────────────────────────────

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    // ── MODULE 01 — Ancienneté ──────────────────────────────────────────────

    public int AncienneteAnnees { get; set; }
    public decimal TauxAnciennete { get; set; }
    public decimal PrimeAnciennete { get; set; }

    // ── MODULE 02 — Présence ────────────────────────────────────────────────

    public int JoursTravailles { get; set; }
    public int JoursConge { get; set; }
    public int JoursFeries { get; set; }
    public decimal SalaireBase26j { get; set; }
    public decimal SalaireBaseMensuel { get; set; }

    // ── MODULE 03 — Heures supplémentaires ──────────────────────────────────

    public decimal MontHsupp25 { get; set; }
    public decimal MontHsupp50 { get; set; }
    public decimal MontHsupp100 { get; set; }
    public decimal TotalHsupp { get; set; }

    // ── MODULE 04 & 05 — NI + Brut imposable ────────────────────────────────

    public decimal TotalPrimesImposables { get; set; }
    public decimal PrimeImposable1 { get; set; }
    public decimal PrimeImposable2 { get; set; }
    public decimal PrimeImposable3 { get; set; }
    public List<PayrollCalculatedPrime> PrimesImposablesDetail { get; set; } = new();
    public decimal TotalNiExonere { get; set; }
    public decimal TotalNiExcedentImposable { get; set; }
    public decimal SalaireBrutImposable { get; set; }

    /// <summary>NI exonérées ligne par ligne (après plafonds module 04).</summary>
    public decimal NiLineTransport { get; set; }
    public decimal NiLineKilometrique { get; set; }
    public decimal NiLineTournee { get; set; }
    public decimal NiLineRepresentation { get; set; }
    public decimal NiLinePanier { get; set; }
    public decimal NiLineCaisse { get; set; }
    public decimal NiLineLait { get; set; }
    public decimal NiLineOutillage { get; set; }
    public decimal NiLineSalissure { get; set; }
    public decimal NiLineAideMedicale { get; set; }
    public decimal NiLineGratifSociale { get; set; }
    public decimal NiLineAutres { get; set; }

    // ── MODULE 06 — CNSS salarial ────────────────────────────────────────────

    public decimal BaseCnssRg { get; set; }
    public decimal CnssRgSalarial { get; set; }
    public decimal CnssAmoSalarial { get; set; }
    public decimal TotalCnssSalarial { get; set; }

    // ── MODULE 06 — CNSS patronal ────────────────────────────────────────────

    public decimal CnssRgPatronal { get; set; }
    public decimal CnssAllocFamPatronal { get; set; }
    public decimal CnssFpPatronal { get; set; }
    public decimal CnssAmoPatronal { get; set; }
    public decimal CnssParticipAmoPatronal { get; set; }
    public decimal TotalCnssPatronal { get; set; }

    // ── MODULE 07 — CIMR ────────────────────────────────────────────────────

    public decimal BaseCimr { get; set; }
    public decimal CimrSalarial { get; set; }
    public decimal CimrPatronal { get; set; }

    // ── MODULE 09 — Mutuelle ────────────────────────────────────────────────

    public decimal MutuelleSalarialeAmount { get; set; }
    public decimal MutuellePatronaleAmount { get; set; }

    // ── MODULE 08 — Frais professionnels ─────────────────────────────────────

    public decimal TauxFp { get; set; }
    public decimal MontantFp { get; set; }

    // ── MODULE 09 — Base IR ─────────────────────────────────────────────────

    public decimal RevenuNetImposable { get; set; }

    // ── MODULE 10 — IR ──────────────────────────────────────────────────────

    public decimal TauxIr { get; set; }
    public decimal DeductionFamille { get; set; }
    public decimal IrBrut { get; set; }
    public decimal IrFinal { get; set; }

    // ── MODULE 11 — Net à payer ─────────────────────────────────────────────

    public decimal TotalRetenuesSalariales { get; set; }
    public decimal SalaireNet { get; set; }
    public decimal AvanceSalaire { get; set; }
    public decimal InteretPretLogement { get; set; }

    // ── MODULE 12 — Coût employeur ──────────────────────────────────────────

    public decimal TotalChargesPatronales { get; set; }
    public decimal CoutEmployeurTotal { get; set; }

    // ── MODULE 13 — Congés annuels ──────────────────────────────────────────

    public decimal JoursCongeAnnuels { get; set; }

    // ── Audit ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Trace détaillée étape par étape — alimentée par <c>RunPipeline()</c>.
    /// Initialisée à new List avant l'appel au pipeline ; non null après <c>CalculatePayroll</c>.
    /// </summary>
    public List<PayrollAuditStep>? AuditSteps { get; set; }
}

public class PayrollCalculatedPrime
{
    public string Label { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public int Ordre { get; set; }
}
