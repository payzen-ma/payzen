namespace payzen_backend.Services.Payroll;

/// <summary>
/// Régime CIMR (DSL @ENUM RegimeCIMR)
/// </summary>
public enum RegimeCimr
{
    AUCUN,
    AL_KAMIL,
    AL_MOUNASSIB
}

/// <summary>
/// Entrée type Salarie (DSL @INPUT) + sorties intermédiaires de chaque module.
/// Rempli par l'adapter depuis EmployeePayrollDto, puis par le pipeline des 14 modules.
/// </summary>
public class PayrollCalculationContext
{
    // ═══════════════════════════════════════════════════════════
    // INPUT (Salarie)
    // ═══════════════════════════════════════════════════════════
    public decimal SalaireBase26j { get; set; }
    public DateTime DateEmbauche { get; set; }
    public int MoisPaie { get; set; }
    public int AnneePaie { get; set; }
    public int SituationFam { get; set; }
    public int JoursTravailles { get; set; }
    public int JoursFeries { get; set; }
    public int JoursConge { get; set; }
    public int HeuresMois { get; set; } = PayrollConstants.WorkHoursRef;
    public decimal HSup25Pct { get; set; }
    public decimal HSup50Pct { get; set; }
    public decimal HSup100Pct { get; set; }
    public List<PrimeImposableItem> PrimesImposables { get; set; } = new();
    public RegimeCimr RegimeCimr { get; set; }
    public decimal CimrTauxSalarial { get; set; }
    public decimal CimrTauxPatronal { get; set; }
    /// <summary>Taux mutuelle part salariale (ex. 0.02 pour 2%).</summary>
    public decimal MutuelleSalariale { get; set; }
    public decimal MutuellePatronale { get; set; }
    /// <summary>Montant déduit (calculé = Brut × taux) pour RNI et retenues.</summary>
    public decimal MutuelleSalarialeAmount { get; set; }
    public decimal MutuellePatronaleAmount { get; set; }
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
    public decimal AvanceSalaire { get; set; }
    public decimal InteretPretLogement { get; set; }
    public bool DisableAmo { get; set; }

    // ═══════════════════════════════════════════════════════════
    // MODULE 01 — Ancienneté
    // ═══════════════════════════════════════════════════════════
    public int AncienneteAnnees { get; set; }
    public decimal TauxAnciennete { get; set; }
    public decimal PrimeAnciennete { get; set; }

    // MODULE 02 — Présence
    public int JoursPayesTotal { get; set; }
    public decimal SalaireBaseMensuel { get; set; }

    // MODULE 03 — Heures supplémentaires
    public decimal TauxHoraire { get; set; }
    public decimal MontHsupp25 { get; set; }
    public decimal MontHsupp50 { get; set; }
    public decimal MontHsupp100 { get; set; }
    public decimal TotalHsupp { get; set; }

    // MODULE 04 — Indemnités non imposables (agrégats)
    public decimal TotalNiExonere { get; set; }
    public decimal TotalNiExcedentImposable { get; set; }

    // MODULE 05 — Salaire brut imposable
    public decimal TotalPrimesImposables { get; set; }
    public decimal SalaireBrutImposable { get; set; }

    // MODULE 06 — CNSS
    public decimal BaseCnssRg { get; set; }
    public decimal CnssRgSalarial { get; set; }
    public decimal CnssAmoSalarial { get; set; }
    public decimal TotalCnssSalarial { get; set; }
    public decimal CnssRgPatronal { get; set; }
    public decimal CnssAllocFamPatronal { get; set; }
    public decimal CnssFpPatronal { get; set; }
    public decimal CnssAmoPatronal { get; set; }
    public decimal CnssParticipAmoPatronal { get; set; }
    public decimal TotalCnssPatronal { get; set; }

    // MODULE 07 — CIMR
    public decimal BaseCimr { get; set; }
    public decimal CimrSalarial { get; set; }
    public decimal CimrPatronal { get; set; }

    // MODULE 08 — Frais professionnels
    public decimal BaseFp { get; set; }
    public decimal TauxFp { get; set; }
    public decimal PlafondFp { get; set; }
    public decimal MontantFp { get; set; }

    // MODULE 09 — Base IR
    public decimal RevenuNetImposable { get; set; }

    // MODULE 10 — IR
    public decimal TauxIr { get; set; }
    public decimal DeductionBareme { get; set; }
    public decimal IrBrut { get; set; }
    public decimal DeductionFamille { get; set; }
    public decimal IrFinal { get; set; }

    // MODULE 11 — Net à payer
    public decimal TotalRetenuesSalariales { get; set; }
    public decimal SalaireNet { get; set; }

    // MODULE 12 — Coût employeur
    public decimal TotalChargesPatronales { get; set; }
    public decimal CoutEmployeurTotal { get; set; }

    // MODULE 13 — Congés annuels (informatif)
    public decimal JoursCongeAnnuels { get; set; }
}

public class PrimeImposableItem
{
    public string Label { get; set; } = "";
    public decimal Montant { get; set; }
}
