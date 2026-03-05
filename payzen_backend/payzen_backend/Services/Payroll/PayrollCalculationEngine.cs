using System.Collections.Generic;
using System.Text.Json;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Employee.Dtos;
using payzen_backend.Models.Payroll;

namespace payzen_backend.Services.Payroll;

/// <summary>
/// Moteur de calcul de paie marocain — implémentation stricte du DSL PAYZEN regles_paie.txt v3.1
/// Pipeline : MODULE[01] → … → MODULE[12]. CNSS Décret 2.25.266 (2025), CGI Art.59, Code du Travail
/// </summary>
public class PayrollCalculationEngine
{
    private readonly ILogger<PayrollCalculationEngine> _logger;

    public PayrollCalculationEngine(ILogger<PayrollCalculationEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Calcule la fiche de paie : adapte le DTO vers le contexte DSL, exécute le pipeline, mappe vers PayrollCalculationResult.
    /// </summary>
    public PayrollCalculationResult CalculatePayroll(EmployeePayrollDto data)
    {
        _logger.LogInformation("🧮 Début du calcul de paie pour {FullName}", data.FullName);

        var result = new PayrollCalculationResult
        {
            EmployeeName = data.FullName,
            Month = data.PayMonth,
            Year = data.PayYear
        };

        try
        {
            var ctx = BuildContextFromDto(data);
            result.AuditSteps = new List<PayrollAuditStepDto>();
            RunPipeline(ctx, result);
            MapContextToResult(ctx, result, data);
            result.Success = true;
            _logger.LogInformation("✅ Calcul terminé — Net à payer : {Net:N2} MAD", result.SalaireNet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur lors du calcul de paie");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    // ══════════════════════════════════════════════════════════════
    // ADAPTATEUR EmployeePayrollDto → PayrollCalculationContext
    // ══════════════════════════════════════════════════════════════

    private static PayrollCalculationContext BuildContextFromDto(EmployeePayrollDto data)
    {
        var ctx = new PayrollCalculationContext
        {
            SalaireBase26j = data.BaseSalary,
            DateEmbauche = data.ContractStartDate,
            MoisPaie = data.PayMonth,
            AnneePaie = data.PayYear,
            SituationFam = Math.Min(6, data.NumberOfChildren + (data.HasSpouse ? 1 : 0)),
            HeuresMois = PayrollConstants.WorkHoursRef,
            JoursFeries = 0,
            AvanceSalaire = 0,
            InteretPretLogement = 0,
            DisableAmo = data.DisableAmo
        };

        // Jours travaillés / congés / absences — le calcul de paie tient compte des deux :
        // - Congés (Leaves) : jours payés, déduits des 26 j de référence pour obtenir JoursTravailles.
        // - Absences approuvées (hors maternité) : jours non payés, déduits aussi.
        var joursAbsents = data.Absences?.Where(a => string.Equals(a.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                && a.AbsenceType != "MATERNIT"
                && (a.DurationType == "FullDay" || a.DurationType == "HalfDay"))
            .Sum(a => a.DurationType == "FullDay" ? 1m : 0.5m) ?? 0;
        var joursAbsentsEntiers = (int)Math.Round(joursAbsents);
        var joursConge = (int)Math.Round(data.Leaves?.Sum(l => l.DaysCount) ?? 0);
        ctx.JoursConge = joursConge;
        ctx.JoursTravailles = Math.Max(0, PayrollConstants.WorkDaysRef - joursConge - joursAbsentsEntiers);

        // Heures supplémentaires par tranche (25%, 50%, 100%)
        if (data.Overtimes != null)
        {
            foreach (var o in data.Overtimes)
            {
                if (Math.Abs(o.RateMultiplier - 1.25m) < 0.01m) ctx.HSup25Pct += o.DurationInHours;
                else if (Math.Abs(o.RateMultiplier - 1.50m) < 0.01m) ctx.HSup50Pct += o.DurationInHours;
                else if (Math.Abs(o.RateMultiplier - 2.00m) < 0.01m) ctx.HSup100Pct += o.DurationInHours;
            }
        }

        // Primes imposables (SalaryComponents + PackageItems)
        var primes = new List<PrimeImposableItem>();
        foreach (var c in data.SalaryComponents?.Where(c => c.Istaxable) ?? Array.Empty<PayrollSalaryComponentDto>())
            primes.Add(new PrimeImposableItem { Label = c.ComponentType, Montant = c.Amount });
        foreach (var i in data.PackageItems?.Where(i => i.IsTaxable) ?? Array.Empty<PayrollPackageItemDto>())
            primes.Add(new PrimeImposableItem { Label = i.Label, Montant = i.DefaultValue });
        ctx.PrimesImposables = primes;

        // CIMR
        ctx.RegimeCimr = (data.CimrEmployeeRate.HasValue && data.CimrEmployeeRate.Value > 0)
            ? RegimeCimr.AL_KAMIL
            : RegimeCimr.AUCUN;
        ctx.CimrTauxSalarial = (data.CimrEmployeeRate ?? 0) / 100m;
        ctx.CimrTauxPatronal = (data.CimrCompanyRate ?? 0) / 100m;

        // Mutuelle
        if (data.HasPrivateInsurance && data.PrivateInsuranceRate.HasValue)
        {
            var taux = data.PrivateInsuranceRate.Value / 100m;
            ctx.MutuelleSalariale = taux;
            ctx.MutuellePatronale = taux;
        }

        // Indemnités non imposables (NI) — mapping par libellé
        var compos = data.SalaryComponents?.Where(c => !c.Istaxable).ToList() ?? new List<PayrollSalaryComponentDto>();
        var items = data.PackageItems?.Where(i => !i.IsTaxable).ToList() ?? new List<PayrollPackageItemDto>();
        foreach (var c in compos)
            MapNiToContext(ctx, c.ComponentType, c.Amount, data.BaseSalary, ctx.JoursTravailles);
        foreach (var i in items)
            MapNiToContext(ctx, i.Label, i.DefaultValue, data.BaseSalary, ctx.JoursTravailles);

        return ctx;
    }

    private static void MapNiToContext(PayrollCalculationContext ctx, string label, decimal amount,
        decimal salaireBase26j, int joursTravailles)
    {
        var u = label.ToUpperInvariant().Trim();
        if (u.Contains("TRANSPORT")) { ctx.NiTransport += amount; return; }
        if (u.Contains("KILOM") || u.Contains("KILOMETRIQUE")) { ctx.NiKilometrique += amount; return; }
        if (u.Contains("TOURNEE") || u.Contains("TOURNÉE")) { ctx.NiTournee += amount; return; }
        if (u.Contains("REPRESENTATION") || u.Contains("REPRÉSENTATION")) { ctx.NiRepresentation += amount; return; }
        if (u.Contains("PANIER")) { ctx.NiPanier += amount; return; }
        if (u.Contains("CAISSE")) { ctx.NiCaisse += amount; return; }
        if (u.Contains("SALISSURE")) { ctx.NiSalissure += amount; return; }
        if (u.Contains("LAIT")) { ctx.NiLait += amount; return; }
        if (u.Contains("OUTILLAGE")) { ctx.NiOutillage += amount; return; }
        if (u.Contains("AIDE") && u.Contains("MEDICAL")) { ctx.NiAideMedicale += amount; return; }
        if (u.Contains("GRATIF") || u.Contains("SOCIAL")) { ctx.NiGratifSociale += amount; return; }
        ctx.NiAutres += amount;
    }

    /// <summary>Exécute le pipeline @PIPELINE calcul_fiche_paie (STEP 1 à 13) et enregistre l'audit.</summary>
    private void RunPipeline(PayrollCalculationContext ctx, PayrollCalculationResult result)
    {
        Module01_Anciennete(ctx);
        RecordAuditStep(result, 1, "Module01_Anciennete",
            "PrimeAnciennete = SalaireBase26j × TauxAnciennete (0% <2a, 5% <5a, 10% <12a, 15% <20a, 20% ≥20a)",
            new Dictionary<string, object> { ["SalaireBase26j"] = ctx.SalaireBase26j, ["DateEmbauche"] = ctx.DateEmbauche.ToString("yyyy-MM-dd"), ["AncienneteAnnees"] = ctx.AncienneteAnnees, ["TauxAnciennete"] = ctx.TauxAnciennete },
            new Dictionary<string, object> { ["PrimeAnciennete"] = ctx.PrimeAnciennete });

        Module02_Presence(ctx);
        RecordAuditStep(result, 2, "Module02_Presence",
            "SalaireBaseMensuel = SalaireBase26j si JoursPayes≥26, sinon SalaireBase26j × (JoursPayesTotal / 26)",
            new Dictionary<string, object> { ["SalaireBase26j"] = ctx.SalaireBase26j, ["JoursTravailles"] = ctx.JoursTravailles, ["JoursFeries"] = ctx.JoursFeries, ["JoursConge"] = ctx.JoursConge, ["JoursPayesTotal"] = ctx.JoursPayesTotal },
            new Dictionary<string, object> { ["SalaireBaseMensuel"] = ctx.SalaireBaseMensuel });

        Module03_HeuresSupplementaires(ctx);
        RecordAuditStep(result, 3, "Module03_HeuresSupplementaires",
            "TauxHoraire = (SalaireBaseMensuel + PrimeAnciennete) / HeuresMois ; MontHsupp = Heures × TauxHoraire × (1.25 | 1.50 | 2.00)",
            new Dictionary<string, object> { ["SalaireBaseMensuel"] = ctx.SalaireBaseMensuel, ["PrimeAnciennete"] = ctx.PrimeAnciennete, ["HeuresMois"] = ctx.HeuresMois, ["HSup25Pct"] = ctx.HSup25Pct, ["HSup50Pct"] = ctx.HSup50Pct, ["HSup100Pct"] = ctx.HSup100Pct },
            new Dictionary<string, object> { ["TauxHoraire"] = ctx.TauxHoraire, ["MontHsupp25"] = ctx.MontHsupp25, ["MontHsupp50"] = ctx.MontHsupp50, ["MontHsupp100"] = ctx.MontHsupp100, ["TotalHsupp"] = ctx.TotalHsupp });

        Module04_IndemnitesNonImposables(ctx);
        RecordAuditStep(result, 4, "Module04_IndemnitesNonImposables",
            "Scission par type (transport, panier, représentation, etc.) avec plafonds DGI ; TotalNiExonere + TotalNiExcedentImposable",
            new Dictionary<string, object> { ["NiTransport"] = ctx.NiTransport, ["NiPanier"] = ctx.NiPanier, ["NiRepresentation"] = ctx.NiRepresentation, ["JoursTravailles"] = ctx.JoursTravailles, ["SalaireBase26j"] = ctx.SalaireBase26j },
            new Dictionary<string, object> { ["TotalNiExonere"] = ctx.TotalNiExonere, ["TotalNiExcedentImposable"] = ctx.TotalNiExcedentImposable });

        Module05_SalaireBrutImposable(ctx);
        RecordAuditStep(result, 5, "Module05_SalaireBrutImposable",
            "SalaireBrutImposable = SalaireBaseMensuel + PrimeAnciennete + TotalHsupp + TotalPrimesImposables + TotalNiExcedentImposable",
            new Dictionary<string, object> { ["SalaireBaseMensuel"] = ctx.SalaireBaseMensuel, ["PrimeAnciennete"] = ctx.PrimeAnciennete, ["TotalHsupp"] = ctx.TotalHsupp, ["TotalPrimesImposables"] = ctx.TotalPrimesImposables, ["TotalNiExcedentImposable"] = ctx.TotalNiExcedentImposable },
            new Dictionary<string, object> { ["SalaireBrutImposable"] = ctx.SalaireBrutImposable });

        Module06_Cnss(ctx);
        RecordAuditStep(result, 6, "Module06_Cnss",
            "BaseCnssRg = min(BrutImposable, 6000) ; RG salarial 4.48%, AMO salarial 2.26% ; Patronal RG 8.98%, AllocFam 6.4%, FP 1.6%, AMO 2.26%+1.85% (Décret 2.25.266)",
            new Dictionary<string, object> { ["SalaireBrutImposable"] = ctx.SalaireBrutImposable, ["DisableAmo"] = ctx.DisableAmo },
            new Dictionary<string, object> { ["BaseCnssRg"] = ctx.BaseCnssRg, ["CnssRgSalarial"] = ctx.CnssRgSalarial, ["CnssAmoSalarial"] = ctx.CnssAmoSalarial, ["TotalCnssSalarial"] = ctx.TotalCnssSalarial, ["TotalCnssPatronal"] = ctx.TotalCnssPatronal });

        Module07_Cimr(ctx);
        RecordAuditStep(result, 7, "Module07_Cimr",
            "Régime AL_KAMIL : Cimr = BrutImposable × Taux ; AL_MOUNASSIB : base = max(0, Brut - 6000) × Taux",
            new Dictionary<string, object> { ["RegimeCimr"] = ctx.RegimeCimr.ToString(), ["SalaireBrutImposable"] = ctx.SalaireBrutImposable, ["CimrTauxSalarial"] = ctx.CimrTauxSalarial, ["CimrTauxPatronal"] = ctx.CimrTauxPatronal },
            new Dictionary<string, object> { ["CimrSalarial"] = ctx.CimrSalarial, ["CimrPatronal"] = ctx.CimrPatronal });

        Module08_FraisProfessionnels(ctx);
        RecordAuditStep(result, 8, "Module08_FraisProfessionnels",
            "BaseFp = BrutImposable ; si Base≤6500 : Taux 35% ; sinon Taux 25%; Plafond 2916.67 Selon la loi de finance 2025; MontantFp = min(Base×Taux, Plafond)",
            new Dictionary<string, object> { ["SalaireBrutImposable"] = ctx.SalaireBrutImposable, ["BaseFp"] = ctx.BaseFp },
            new Dictionary<string, object> { ["TauxFp"] = ctx.TauxFp, ["PlafondFp"] = ctx.PlafondFp, ["MontantFp"] = ctx.MontantFp });

        Module09_BaseIr(ctx);
        RecordAuditStep(result, 9, "Module09_BaseIr",
            "RNI = SalaireBrutImposable - TotalCnssSalarial - CimrSalarial - MutuelleSalariale - MontantFp - InteretPretLogement",
            new Dictionary<string, object> { ["SalaireBrutImposable"] = ctx.SalaireBrutImposable, ["TotalCnssSalarial"] = ctx.TotalCnssSalarial, ["CimrSalarial"] = ctx.CimrSalarial, ["MontantFp"] = ctx.MontantFp, ["MutuelleSalariale"] = ctx.MutuelleSalarialeAmount, ["InteretPretLogement"] = ctx.InteretPretLogement },
            new Dictionary<string, object> { ["RevenuNetImposable"] = ctx.RevenuNetImposable });

        Module10_Ir(ctx);
        RecordAuditStep(result, 10, "Module10_Ir",
            "Barème IR mensuel 2026 ; DeductionFamille = SituationFam × 30 ; IrFinal = max(0, RNI×Taux - DeductionBareme - DeductionFamille)",
            new Dictionary<string, object> { ["RevenuNetImposable"] = ctx.RevenuNetImposable, ["SituationFam"] = ctx.SituationFam, ["TauxIr"] = ctx.TauxIr, ["DeductionBareme"] = ctx.DeductionBareme, ["DeductionFamille"] = ctx.DeductionFamille },
            new Dictionary<string, object> { ["IrBrut"] = ctx.IrBrut, ["IrFinal"] = ctx.IrFinal });

        Module11_NetAPayer(ctx);
        RecordAuditStep(result, 11, "Module11_NetAPayer",
            "TotalRetenuesSalariales = CNSS + CIMR + Mutuelle + IR + AvanceSalaire ; SalaireNet = BrutImposable - TotalRetenues + TotalNiExonere",
            new Dictionary<string, object> { ["SalaireBrutImposable"] = ctx.SalaireBrutImposable, ["TotalCnssSalarial"] = ctx.TotalCnssSalarial, ["CimrSalarial"] = ctx.CimrSalarial, ["MutuelleSalarialeAmount"] = ctx.MutuelleSalarialeAmount, ["IrFinal"] = ctx.IrFinal, ["TotalNiExonere"] = ctx.TotalNiExonere },
            new Dictionary<string, object> { ["TotalRetenuesSalariales"] = ctx.TotalRetenuesSalariales, ["SalaireNet"] = ctx.SalaireNet });

        Module12_CoutEmployeur(ctx);
        RecordAuditStep(result, 12, "Module12_CoutEmployeur",
            "TotalChargesPatronales = TotalCnssPatronal + CimrPatronal + MutuellePatronale ; CoutEmployeurTotal = BrutImposable + TotalChargesPatronales + TotalNiExonere",
            new Dictionary<string, object> { ["SalaireBrutImposable"] = ctx.SalaireBrutImposable, ["TotalCnssPatronal"] = ctx.TotalCnssPatronal, ["CimrPatronal"] = ctx.CimrPatronal, ["MutuellePatronaleAmount"] = ctx.MutuellePatronaleAmount, ["TotalNiExonere"] = ctx.TotalNiExonere },
            new Dictionary<string, object> { ["TotalChargesPatronales"] = ctx.TotalChargesPatronales, ["CoutEmployeurTotal"] = ctx.CoutEmployeurTotal });

        Module13_CongesAnnuels(ctx);
        RecordAuditStep(result, 13, "Module13_CongesAnnuels",
            "JoursCongeAnnuels selon ancienneté : <5a 18j, <10a 19.5j, <15a 21j, <20a 22.5j, <25a 24j, <30a 25.5j, <35a 27j, <40a 28.5j, ≥40a 30j",
            new Dictionary<string, object> { ["AncienneteAnnees"] = ctx.AncienneteAnnees },
            new Dictionary<string, object> { ["JoursCongeAnnuels"] = ctx.JoursCongeAnnuels });
    }

    private static void RecordAuditStep(PayrollCalculationResult result, int stepOrder, string moduleName, string formulaDescription,
        Dictionary<string, object> inputs, Dictionary<string, object> outputs)
    {
        result.AuditSteps!.Add(new PayrollAuditStepDto
        {
            StepOrder = stepOrder,
            ModuleName = moduleName,
            FormulaDescription = formulaDescription,
            InputsJson = JsonSerializer.Serialize(inputs),
            OutputsJson = JsonSerializer.Serialize(outputs)
        });
    }

    // ══════════════════════════════════════════════════════════════
    // MODULE[01] anciennete
    // ══════════════════════════════════════════════════════════════
    private static void Module01_Anciennete(PayrollCalculationContext ctx)
    {
        var lastDay = new DateTime(ctx.AnneePaie, ctx.MoisPaie, DateTime.DaysInMonth(ctx.AnneePaie, ctx.MoisPaie));
        ctx.AncienneteAnnees = (int)((lastDay - ctx.DateEmbauche).TotalDays / 365);

        ctx.TauxAnciennete = ctx.AncienneteAnnees switch
        {
            < 2 => 0.00m,
            < 5 => 0.05m,
            < 12 => 0.10m,
            < 20 => 0.15m,
            _ => 0.20m
        };

        ctx.PrimeAnciennete = Round(ctx.SalaireBase26j * ctx.TauxAnciennete, 2);
    }

    // ══════════════════════════════════════════════════════════════
    // MODULE[02] presence
    // ══════════════════════════════════════════════════════════════
    private static void Module02_Presence(PayrollCalculationContext ctx)
    {
        ctx.JoursPayesTotal = ctx.JoursTravailles + ctx.JoursFeries + ctx.JoursConge;
        ctx.SalaireBaseMensuel = ctx.JoursPayesTotal >= PayrollConstants.WorkDaysRef
            ? ctx.SalaireBase26j
            : Round(ctx.SalaireBase26j * ctx.JoursPayesTotal / PayrollConstants.WorkDaysRef, 2);
    }

    // ══════════════════════════════════════════════════════════════
    // MODULE[03] heures_supplementaires
    // ══════════════════════════════════════════════════════════════
    private static void Module03_HeuresSupplementaires(PayrollCalculationContext ctx)
    {
        var baseHsupp = ctx.SalaireBaseMensuel + ctx.PrimeAnciennete;
        ctx.TauxHoraire = Round(baseHsupp / ctx.HeuresMois, 4);
        ctx.MontHsupp25 = Round(ctx.HSup25Pct * ctx.TauxHoraire * 1.25m, 2);
        ctx.MontHsupp50 = Round(ctx.HSup50Pct * ctx.TauxHoraire * 1.50m, 2);
        ctx.MontHsupp100 = Round(ctx.HSup100Pct * ctx.TauxHoraire * 2.00m, 2);
        ctx.TotalHsupp = ctx.MontHsupp25 + ctx.MontHsupp50 + ctx.MontHsupp100;
    }

    // ══════════════════════════════════════════════════════════════
    // MODULE[04] indemnites_non_imposables — scission + agrégation (DGI pour IR)
    // ══════════════════════════════════════════════════════════════
    private static void Module04_IndemnitesNonImposables(PayrollCalculationContext ctx)
    {
        decimal niTransportExo = Math.Min(ctx.NiTransport, PayrollConstants.PLAFOND_NI_TRANSPORT);
        decimal niTransportImp = Math.Max(0, ctx.NiTransport - PayrollConstants.PLAFOND_NI_TRANSPORT);

        decimal niKilometriqueExo = ctx.NiKilometrique;

        decimal niTourneeExo = Math.Min(ctx.NiTournee, PayrollConstants.PLAFOND_NI_TOURNEE);
        decimal niTourneeImp = Math.Max(0, ctx.NiTournee - PayrollConstants.PLAFOND_NI_TOURNEE);

        decimal plafondRep = ctx.SalaireBase26j * PayrollConstants.PLAFOND_NI_REPRESENTATION;
        decimal niRepresentationExo = Math.Min(ctx.NiRepresentation, plafondRep);
        decimal niRepresentationImp = Math.Max(0, ctx.NiRepresentation - plafondRep);

        decimal plafondPanier = PayrollConstants.PLAFOND_NI_PANIER_JOUR * ctx.JoursTravailles;
        decimal niPanierExo = Math.Min(ctx.NiPanier, plafondPanier);
        decimal niPanierImp = Math.Max(0, ctx.NiPanier - plafondPanier);

        decimal niCaisseExoDgi = Math.Min(ctx.NiCaisse, PayrollConstants.PLAFOND_NI_CAISSE_DGI);
        decimal niCaisseImpIr = Math.Max(0, ctx.NiCaisse - PayrollConstants.PLAFOND_NI_CAISSE_DGI);

        decimal niLaitExoDgi = Math.Min(ctx.NiLait, PayrollConstants.PLAFOND_NI_LAIT_DGI);
        decimal niLaitImpIr = Math.Max(0, ctx.NiLait - PayrollConstants.PLAFOND_NI_LAIT_DGI);

        decimal niOutillageExoDgi = Math.Min(ctx.NiOutillage, PayrollConstants.PLAFOND_NI_OUTILLAGE_DGI);
        decimal niOutillageImpIr = Math.Max(0, ctx.NiOutillage - PayrollConstants.PLAFOND_NI_OUTILLAGE_DGI);

        decimal niSalissureExoDgi = Math.Min(ctx.NiSalissure, PayrollConstants.PLAFOND_NI_SALISSURE_DGI);
        decimal niSalissureImpIr = Math.Max(0, ctx.NiSalissure - PayrollConstants.PLAFOND_NI_SALISSURE_DGI);

        decimal niAideMedicaleExo = ctx.NiAideMedicale;

        decimal niGratifExoDgi = Math.Min(ctx.NiGratifSociale, PayrollConstants.PLAFOND_NI_GRATIF_DGI);
        decimal niGratifImpIr = Math.Max(0, ctx.NiGratifSociale - PayrollConstants.PLAFOND_NI_GRATIF_DGI);

        decimal niAutresExo = ctx.NiAutres;

        ctx.TotalNiExonere = niTransportExo + niKilometriqueExo + niTourneeExo + niRepresentationExo + niPanierExo
            + niCaisseExoDgi + niLaitExoDgi + niOutillageExoDgi + niSalissureExoDgi
            + niAideMedicaleExo + niGratifExoDgi + niAutresExo;

        ctx.TotalNiExcedentImposable = niTransportImp + niTourneeImp + niRepresentationImp + niPanierImp
            + niCaisseImpIr + niLaitImpIr + niOutillageImpIr + niSalissureImpIr + niGratifImpIr;
    }

    // ══════════════════════════════════════════════════════════════
    // MODULE[05] salaire_brut_imposable
    // ══════════════════════════════════════════════════════════════
    private static void Module05_SalaireBrutImposable(PayrollCalculationContext ctx)
    {
        ctx.TotalPrimesImposables = ctx.PrimesImposables.Sum(p => p.Montant);
        ctx.SalaireBrutImposable = ctx.SalaireBaseMensuel + ctx.PrimeAnciennete + ctx.TotalHsupp
            + ctx.TotalPrimesImposables + ctx.TotalNiExcedentImposable;
    }

    // ══════════════════════════════════════════════════════════════
    // MODULE[06] cnss
    // ══════════════════════════════════════════════════════════════
    private static void Module06_Cnss(PayrollCalculationContext ctx)
    {
        ctx.BaseCnssRg = Math.Min(ctx.SalaireBrutImposable, PayrollConstants.PLAFOND_CNSS_MENSUEL);

        ctx.CnssRgSalarial = Round(ctx.BaseCnssRg * PayrollConstants.CNSS_RG_SALARIAL, 2);
        ctx.CnssAmoSalarial = ctx.DisableAmo ? 0 : Round(ctx.SalaireBrutImposable * PayrollConstants.CNSS_AMO_SALARIAL, 2);
        ctx.TotalCnssSalarial = ctx.CnssRgSalarial + ctx.CnssAmoSalarial;

        ctx.CnssRgPatronal = Round(ctx.BaseCnssRg * PayrollConstants.CNSS_RG_PATRONAL, 2);
        ctx.CnssAllocFamPatronal = Round(ctx.SalaireBrutImposable * PayrollConstants.CNSS_ALLOC_FAM_PAT, 2);
        ctx.CnssFpPatronal = Round(ctx.SalaireBrutImposable * PayrollConstants.CNSS_FP_PAT, 2);
        ctx.CnssAmoPatronal = ctx.DisableAmo ? 0 : Round(ctx.SalaireBrutImposable * PayrollConstants.CNSS_AMO_PATRONAL, 2);
        ctx.CnssParticipAmoPatronal = ctx.DisableAmo ? 0 : Round(ctx.SalaireBrutImposable * PayrollConstants.CNSS_AMO_PARTICIPATION_PAT, 2);
        ctx.TotalCnssPatronal = ctx.CnssRgPatronal + ctx.CnssAllocFamPatronal + ctx.CnssFpPatronal
            + ctx.CnssAmoPatronal + ctx.CnssParticipAmoPatronal;
    }

    // ══════════════════════════════════════════════════════════════
    // MODULE[07] cimr
    // ══════════════════════════════════════════════════════════════
    private static void Module07_Cimr(PayrollCalculationContext ctx)
    {
        switch (ctx.RegimeCimr)
        {
            case RegimeCimr.AUCUN:
                ctx.BaseCimr = 0;
                ctx.CimrSalarial = 0;
                ctx.CimrPatronal = 0;
                break;
            case RegimeCimr.AL_KAMIL:
                ctx.BaseCimr = ctx.SalaireBrutImposable;
                ctx.CimrSalarial = Round(ctx.SalaireBrutImposable * ctx.CimrTauxSalarial, 2);
                ctx.CimrPatronal = Round(ctx.SalaireBrutImposable * ctx.CimrTauxPatronal, 2);
                break;
            case RegimeCimr.AL_MOUNASSIB:
                var baseMounassib = Math.Max(0, ctx.SalaireBrutImposable - PayrollConstants.PLAFOND_CNSS_MENSUEL);
                ctx.BaseCimr = baseMounassib;
                ctx.CimrSalarial = Round(baseMounassib * ctx.CimrTauxSalarial, 2);
                ctx.CimrPatronal = Round(baseMounassib * ctx.CimrTauxPatronal, 2);
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // MODULE[08] frais_professionnels — base_fp = salaire_brut_imposable (pas de déduction CNSS)
    // ══════════════════════════════════════════════════════════════
    private static void Module08_FraisProfessionnels(PayrollCalculationContext ctx)
    {
        ctx.BaseFp = ctx.SalaireBrutImposable;
        if (ctx.BaseFp <= PayrollConstants.FP_SEUIL_35)
        {
            ctx.TauxFp = PayrollConstants.FP_TAUX_35;
            ctx.PlafondFp = PayrollConstants.FP_PLAFOND_35;
        }
        else
        {
            ctx.TauxFp = PayrollConstants.FP_TAUX_25;
            ctx.PlafondFp = PayrollConstants.FP_PLAFOND_25;
        }
        ctx.MontantFp = Math.Min(Round(ctx.BaseFp * ctx.TauxFp, 2), ctx.PlafondFp);
    }

    // ══════════════════════════════════════════════════════════════
    // MODULE[09] base_ir — RNI = brut − cnss − cimr − mutuelle − fp − interet_pret
    // ══════════════════════════════════════════════════════════════
    private static void Module09_BaseIr(PayrollCalculationContext ctx)
    {
        ctx.MutuelleSalarialeAmount = Round(ctx.SalaireBrutImposable * ctx.MutuelleSalariale, 2);
        ctx.MutuellePatronaleAmount = Round(ctx.SalaireBrutImposable * ctx.MutuellePatronale, 2);
        ctx.RevenuNetImposable = ctx.SalaireBrutImposable
            - ctx.TotalCnssSalarial
            - ctx.CimrSalarial
            - ctx.MutuelleSalarialeAmount
            - ctx.MontantFp
            - ctx.InteretPretLogement;
        ctx.RevenuNetImposable = Math.Max(0, ctx.RevenuNetImposable);
    }

    // ══════════════════════════════════════════════════════════════
    // MODULE[10] ir — barème mensuel 2026 + charges de famille
    // ══════════════════════════════════════════════════════════════
    private static void Module10_Ir(PayrollCalculationContext ctx)
    {
        ctx.DeductionFamille = ctx.SituationFam * PayrollConstants.IR_DEDUCTION_FAMILLE;

        foreach (var t in PayrollConstants.BaremeIRMensuel2026)
        {
            if (ctx.RevenuNetImposable >= t.RniMin && ctx.RevenuNetImposable <= t.RniMax)
            {
                ctx.TauxIr = t.Taux;
                ctx.DeductionBareme = t.Deduction;
                break;
            }
        }
        ctx.IrBrut = Round(ctx.RevenuNetImposable * ctx.TauxIr, 2);
        ctx.IrFinal = Math.Max(0, Round(ctx.IrBrut - ctx.DeductionBareme - ctx.DeductionFamille, 2));
    }

    // ══════════════════════════════════════════════════════════════
    // MODULE[11] net_a_payer
    // ══════════════════════════════════════════════════════════════
    private static void Module11_NetAPayer(PayrollCalculationContext ctx)
    {
        ctx.TotalRetenuesSalariales = ctx.TotalCnssSalarial + ctx.CimrSalarial + ctx.MutuelleSalarialeAmount
            + ctx.IrFinal + ctx.AvanceSalaire;
        ctx.SalaireNet = ctx.SalaireBrutImposable - ctx.TotalRetenuesSalariales + ctx.TotalNiExonere;
    }

    // ══════════════════════════════════════════════════════════════
    // MODULE[12] cout_employeur
    // ══════════════════════════════════════════════════════════════
    private static void Module12_CoutEmployeur(PayrollCalculationContext ctx)
    {
        ctx.TotalChargesPatronales = ctx.TotalCnssPatronal + ctx.CimrPatronal + ctx.MutuellePatronaleAmount;
        ctx.CoutEmployeurTotal = ctx.SalaireBrutImposable + ctx.TotalChargesPatronales + ctx.TotalNiExonere;
    }

    // ══════════════════════════════════════════════════════════════
    // MODULE[13] conges_annuels (informatif)
    // ══════════════════════════════════════════════════════════════
    private static void Module13_CongesAnnuels(PayrollCalculationContext ctx)
    {
        ctx.JoursCongeAnnuels = ctx.AncienneteAnnees switch
        {
            < 5 => 18m,
            < 10 => 19.5m,
            < 15 => 21m,
            < 20 => 22.5m,
            < 25 => 24m,
            < 30 => 25.5m,
            < 35 => 27m,
            < 40 => 28.5m,
            _ => 30m
        };
    }

    private static decimal Round(decimal value, int decimals) =>
        Math.Round(value, decimals, MidpointRounding.AwayFromZero);

    // ══════════════════════════════════════════════════════════════
    // MAPPING Contexte → PayrollCalculationResult (compatibilité PaieService)
    // ══════════════════════════════════════════════════════════════
    private void MapContextToResult(PayrollCalculationContext ctx, PayrollCalculationResult result, EmployeePayrollDto data)
    {
        result.SalaireBase = ctx.SalaireBaseMensuel;
        result.PrimesImposables = ctx.TotalPrimesImposables;
        result.PrimesImposablesDetail = ctx.PrimesImposables.Select(p => new PrimeDetail
        {
            Label = p.Label,
            Montant = p.Montant,
            IsTaxable = true
        }).ToList();
        result.HeuresSupplementaires = ctx.TotalHsupp;
        result.PrimeAnciennete = ctx.PrimeAnciennete;

        result.BrutImposable = ctx.SalaireBrutImposable;
        result.BrutImposableAjuste = ctx.SalaireBrutImposable;

        result.IndemnitesNonImposables = ctx.TotalNiExonere;
        result.IndemnitesImposables = ctx.TotalNiExcedentImposable;
        result.IndemnitesDetail = BuildIndemnitesDetail(data, ctx);

        result.CnssRgSalarial = ctx.CnssRgSalarial;
        result.AmoSalarial = ctx.CnssAmoSalarial;
        result.CimrSalarial = ctx.CimrSalarial;
        result.MutuelleSalariale = ctx.MutuelleSalarialeAmount;

        result.CnssBase = ctx.BaseCnssRg;
        result.AmoBase = ctx.DisableAmo ? 0 : ctx.SalaireBrutImposable;
        result.CimrBase = ctx.BaseCimr;
        result.MutuelleBase = ctx.SalaireBrutImposable;
        result.TotalCotisationsSalariales = ctx.TotalCnssSalarial + ctx.CimrSalarial + ctx.MutuelleSalarialeAmount;

        result.FraisProfessionnels = ctx.MontantFp;
        result.RevenuNetImposable = ctx.RevenuNetImposable;
        result.IR = ctx.IrFinal;
        result.IrTaux = ctx.TauxIr;

        result.SalaireNetAvantArrondi = ctx.SalaireNet;
        result.SalaireNet = Math.Ceiling(ctx.SalaireNet);
        result.Arrondi = result.SalaireNet - result.SalaireNetAvantArrondi;

        result.CnssRgPatronal = ctx.CnssRgPatronal;
        result.AmoPatronal = ctx.CnssAmoPatronal + ctx.CnssParticipAmoPatronal;
        result.CimrPatronal = ctx.CimrPatronal;
        result.MutuellePatronale = ctx.MutuellePatronale;
        result.TotalCotisationsPatronales = ctx.TotalChargesPatronales;
        result.CoutEmployeurTotal = ctx.CoutEmployeurTotal;
    }

    private static List<IndemniteDetail> BuildIndemnitesDetail(EmployeePayrollDto data, PayrollCalculationContext ctx)
    {
        var list = new List<IndemniteDetail>();
        var compos = data.SalaryComponents?.Where(c => !c.Istaxable).ToList() ?? new List<PayrollSalaryComponentDto>();
        var items = data.PackageItems?.Where(i => !i.IsTaxable).ToList() ?? new List<PayrollPackageItemDto>();
        decimal plafondTransport = PayrollConstants.PLAFOND_NI_TRANSPORT;
        decimal plafondPanier = PayrollConstants.PLAFOND_NI_PANIER_JOUR * Math.Max(1, ctx.JoursTravailles);
        decimal plafondRep = data.BaseSalary * PayrollConstants.PLAFOND_NI_REPRESENTATION;

        foreach (var c in compos)
        {
            var plafond = GetPlafondForLabel(c.ComponentType, plafondTransport, plafondPanier, plafondRep);
            var exo = Math.Min(c.Amount, plafond);
            var imp = Math.Max(0, c.Amount - plafond);
            list.Add(new IndemniteDetail
            {
                Label = c.ComponentType,
                MontantSaisi = c.Amount,
                PlafondApplicable = plafond,
                PartieExoneree = exo,
                PartieImposable = imp
            });
        }
        foreach (var i in items)
        {
            var plafond = GetPlafondForLabel(i.Label, plafondTransport, plafondPanier, plafondRep);
            var exo = Math.Min(i.DefaultValue, plafond);
            var imp = Math.Max(0, i.DefaultValue - plafond);
            list.Add(new IndemniteDetail
            {
                Label = i.Label,
                MontantSaisi = i.DefaultValue,
                PlafondApplicable = plafond,
                PartieExoneree = exo,
                PartieImposable = imp
            });
        }
        return list;
    }

    private static decimal GetPlafondForLabel(string label, decimal plafondTransport, decimal plafondPanier, decimal plafondRep)
    {
        var u = label.ToUpperInvariant().Trim();
        if (u.Contains("TRANSPORT")) return plafondTransport;
        if (u.Contains("PANIER")) return plafondPanier;
        if (u.Contains("REPRESENTATION") || u.Contains("REPRÉSENTATION")) return plafondRep;
        if (u.Contains("TOURNEE") || u.Contains("TOURNÉE")) return PayrollConstants.PLAFOND_NI_TOURNEE;
        if (u.Contains("CAISSE")) return PayrollConstants.PLAFOND_NI_CAISSE_DGI;
        if (u.Contains("OUTILLAGE")) return PayrollConstants.PLAFOND_NI_OUTILLAGE_DGI;
        if (u.Contains("SALISSURE")) return PayrollConstants.PLAFOND_NI_SALISSURE_DGI;
        if (u.Contains("GRATIF") || u.Contains("SOCIAL")) return PayrollConstants.PLAFOND_NI_GRATIF_DGI;
        return 0;
    }
}

// ══════════════════════════════════════════════════════════════
// RÉSULTAT (compatibilité PaieService / MapNativeResultToPayrollResult)
// ══════════════════════════════════════════════════════════════

public class PayrollCalculationResult
{
    public string EmployeeName { get; set; } = "";
    public int Month { get; set; }
    public int Year { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Audit trail : un enregistrement par module (formule + entrées/sorties JSON).</summary>
    public List<PayrollAuditStepDto>? AuditSteps { get; set; }

    public decimal SalaireBase { get; set; }
    public decimal PrimesImposables { get; set; }
    public List<PrimeDetail> PrimesImposablesDetail { get; set; } = new();
    public decimal HeuresSupplementaires { get; set; }
    public decimal PrimeAnciennete { get; set; }
    public decimal BrutImposable { get; set; }
    public decimal BrutImposableAjuste { get; set; }
    public decimal IndemnitesNonImposables { get; set; }
    public decimal IndemnitesImposables { get; set; }
    public List<IndemniteDetail> IndemnitesDetail { get; set; } = new();
    public decimal CnssRgSalarial { get; set; }
    public decimal AmoSalarial { get; set; }
    public decimal CimrSalarial { get; set; }
    public decimal MutuelleSalariale { get; set; }
    public decimal TotalCotisationsSalariales { get; set; }
    public decimal CnssBase { get; set; }
    public decimal AmoBase { get; set; }
    public decimal CimrBase { get; set; }
    public decimal MutuelleBase { get; set; }
    public decimal IrTaux { get; set; }
    public decimal FraisProfessionnels { get; set; }
    public decimal RevenuNetImposable { get; set; }
    public decimal IR { get; set; }
    public decimal SalaireNetAvantArrondi { get; set; }
    public decimal Arrondi { get; set; }
    public decimal SalaireNet { get; set; }
    public decimal CnssRgPatronal { get; set; }
    public decimal AmoPatronal { get; set; }
    public decimal CimrPatronal { get; set; }
    public decimal MutuellePatronale { get; set; }
    public decimal TotalCotisationsPatronales { get; set; }
    public decimal CoutEmployeurTotal { get; set; }
}

public class PrimeDetail
{
    public string Label { get; set; } = "";
    public decimal Montant { get; set; }
    public bool IsTaxable { get; set; }
}

public class IndemniteDetail
{
    public string Label { get; set; } = "";
    public decimal MontantSaisi { get; set; }
    public decimal PlafondApplicable { get; set; }
    public decimal PartieExoneree { get; set; }
    public decimal PartieImposable { get; set; }
}

/// <summary>DTO pour une étape d'audit du calcul (en mémoire puis persisté en base).</summary>
public class PayrollAuditStepDto
{
    public int StepOrder { get; set; }
    public string ModuleName { get; set; } = "";
    public string FormulaDescription { get; set; } = "";
    public string? InputsJson { get; set; }
    public string? OutputsJson { get; set; }
}
