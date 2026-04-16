using Payzen.Domain.Enums;

namespace Payzen.Application.Payroll;

/// <summary>Composante prime imposable dans le pipeline.</summary>
public class PrimeImposableItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Montant { get; set; }
}

// ════════════════════════════════════════════════════════════
// CONTEXT — DSL @INPUT + intermédiaires des 13 modules
// ════════════════════════════════════════════════════════════

/// <summary>
/// Objet central du moteur de paie.
/// Rempli depuis <see cref="Payzen.Application.DTOs.Payroll.EmployeePayrollDto"/>
/// par <c>PayrollCalculationEngine.BuildContextFromDto()</c>,
/// puis enrichi séquentiellement par les MODULE[01] → MODULE[13].
/// Références légales : CNSS Décret 2.25.266 (2025), CGI Art.59, Code du Travail Maroc.
/// </summary>
public class PayrollCalculationContext
{
    // ── INPUT ────────────────────────────────────────────────────────────────

    /// <summary>Salaire de base contractuel sur 26 jours.</summary>
    public decimal SalaireBase26j { get; set; }

    /// <summary>Date d'embauche — sert au calcul de l'ancienneté (MODULE 01).</summary>
    public DateTime DateEmbauche { get; set; }

    public int MoisPaie { get; set; }
    public int AnneePaie { get; set; }

    /// <summary>0..6 : conjoint (1) + enfants (max 5) — déduction IR famille (MODULE 10).</summary>
    public int SituationFam { get; set; }

    /// <summary>Jours effectivement travaillés (hors congé, hors férié).</summary>
    public int JoursTravailles { get; set; }

    /// <summary>Jours fériés payés dans la période.</summary>
    public int JoursFeries { get; set; }

    /// <summary>Jours de congé pris dans la période.</summary>
    public int JoursConge { get; set; }

    /// <summary>Référence mensuelle contractuelle — défaut : 191 h (PayrollConstants).</summary>
    public int HeuresMois { get; set; } = PayrollConstants.WorkHoursRef;

    /// <summary>Heures travaillées importées depuis le pointage.</summary>
    public decimal HeuresTravaillées { get; set; }

    /// <summary>Taux horaire contractuel — utilisé en fallback si SalaireBase26j = 0.</summary>
    public decimal BaseSalaryHourly { get; set; }

    // Heures supplémentaires par tranche (MODULE 03)
    public decimal HSup25Pct { get; set; }
    public decimal HSup50Pct { get; set; }
    public decimal HSup100Pct { get; set; }

    /// <summary>Primes imposables (SalaryComponents + PackageItems taxables).</summary>
    public List<PrimeImposableItem> PrimesImposables { get; set; } = new();

    // CIMR (MODULE 07)
    public RegimeCimr RegimeCimr { get; set; }
    public decimal CimrTauxSalarial { get; set; }
    public decimal CimrTauxPatronal { get; set; }

    // Mutuelle / assurance privée (MODULE 09)
    public decimal MutuelleSalariale { get; set; } // taux (ex. 0.03)
    public decimal MutuellePatronale { get; set; } // taux (ex. 0.03)
    public decimal MutuelleSalarialeAmount { get; set; } // montant calculé en MODULE 09
    public decimal MutuellePatronaleAmount { get; set; } // montant calculé en MODULE 09

    // Indemnités non imposables — montants bruts avant plafonnement (MODULE 04)
    public decimal NiTransport { get; set; }
    public decimal NiKilometrique { get; set; }
    public decimal NiTournee { get; set; }
    public decimal NiRepresentation { get; set; }
    public decimal NiPanier { get; set; }
    public decimal NiCaisse { get; set; }
    public decimal NiSalissure { get; set; }
    public decimal NiLait { get; set; }
    public decimal NiOutillage { get; set; }
    public decimal NiAideMedicale { get; set; }
    public decimal NiGratifSociale { get; set; }
    public decimal NiAutres { get; set; }

    // Autres déductions
    public decimal AvanceSalaire { get; set; }
    public decimal InteretPretLogement { get; set; }

    /// <summary>
    /// Si true : AMO salariale et patronale sont forcées à 0.
    /// Cas : fonctionnaires affiliés CNOPS ou contrats exonérés AMO.
    /// </summary>
    public bool DisableAmo { get; set; }

    // ── MODULE 01 — Ancienneté ───────────────────────────────────────────────

    public int AncienneteAnnees { get; set; }
    public decimal TauxAnciennete { get; set; }
    public decimal PrimeAnciennete { get; set; }

    /// <summary>Alias stocké pour l'export fiche de paie.</summary>
    public decimal PrimeAnciennteRate { get; set; }

    // ── MODULE 02 — Présence ────────────────────────────────────────────────

    /// <summary>JoursTravailles + JoursFeries + JoursConge</summary>
    public int JoursPayesTotal { get; set; }
    public decimal SalaireBaseMensuel { get; set; }

    // ── MODULE 03 — Heures supplémentaires ──────────────────────────────────

    public decimal TauxHoraire { get; set; }
    public decimal MontHsupp25 { get; set; }
    public decimal MontHsupp50 { get; set; }
    public decimal MontHsupp100 { get; set; }
    public decimal TotalHsupp { get; set; }

    // ── MODULE 04 — Indemnités non imposables ────────────────────────────────

    /// <summary>Partie exonérée totale (dans les plafonds DGI).</summary>
    public decimal TotalNiExonere { get; set; }

    /// <summary>Excédent requalifié en imposable.</summary>
    public decimal TotalNiExcedentImposable { get; set; }

    /// <summary>Montants NI après plafonnement (partie exonérée) — pour persistance fiche de paie.</summary>
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

    // ── MODULE 05 — Salaire brut imposable ──────────────────────────────────

    public decimal TotalPrimesImposables { get; set; }
    public decimal SalaireBrutImposable { get; set; }

    // ── MODULE 06 — CNSS (Décret 2.25.266 — 2025) ───────────────────────────

    public decimal BaseCnssRg { get; set; } // min(SBI, 6000)
    public decimal CnssRgSalarial { get; set; } // 4.48%
    public decimal CnssAmoSalarial { get; set; } // 2.26%
    public decimal TotalCnssSalarial { get; set; }

    public decimal CnssRgPatronal { get; set; } // 8.98%
    public decimal CnssAllocFamPatronal { get; set; } // 6.40%
    public decimal CnssFpPatronal { get; set; } // 1.60%
    public decimal CnssAmoPatronal { get; set; } // 2.26%
    public decimal CnssParticipAmoPatronal { get; set; } // 1.85%
    public decimal TotalCnssPatronal { get; set; }

    // ── MODULE 07 — CIMR ────────────────────────────────────────────────────

    public decimal BaseCimr { get; set; }
    public decimal CimrSalarial { get; set; }
    public decimal CimrPatronal { get; set; }

    // ── MODULE 08 — Frais professionnels (LF 2025) ──────────────────────────

    public decimal BaseFp { get; set; }
    public decimal TauxFp { get; set; }
    public decimal PlafondFp { get; set; }
    public decimal MontantFp { get; set; }

    // ── MODULE 09 — Base IR ─────────────────────────────────────────────────

    public decimal RevenuNetImposable { get; set; }

    // ── MODULE 10 — IR (barème mensuel 2026) ────────────────────────────────

    public decimal TauxIr { get; set; }
    public decimal DeductionBareme { get; set; }
    public decimal DeductionFamille { get; set; }
    public decimal IrBrut { get; set; }
    public decimal IrFinal { get; set; }

    // ── MODULE 11 — Net à payer ─────────────────────────────────────────────

    public decimal TotalRetenuesSalariales { get; set; }
    public decimal SalaireNet { get; set; }

    // ── MODULE 12 — Coût employeur ──────────────────────────────────────────

    public decimal TotalChargesPatronales { get; set; }
    public decimal CoutEmployeurTotal { get; set; }

    // ── MODULE 13 — Congés annuels (Code du Travail Art. 231-265) ───────────

    public decimal JoursCongeAnnuels { get; set; }
}
