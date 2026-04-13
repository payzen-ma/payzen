using Microsoft.Extensions.Logging;
using Payzen.Application.DTOs.Payroll;
using Payzen.Domain.Enums;
using System.Globalization;
using System.Text.Json;
using System.Text;

namespace Payzen.Application.Payroll;

/// <summary>
/// Moteur de calcul de paie marocain — implémentation du DSL PAYZEN regles_paie.txt v3.1.
/// Pipeline séquentiel : MODULE[01] → MODULE[13].
///
/// Références légales :
///   - CNSS Décret 2.25.266 (2025) : taux RG, AMO, Alloc.Fam, FP
///   - CGI Art.59 : frais professionnels (LF 2025)
///   - CGI Art.73 : barème IR mensuel 2026
///   - Code du Travail Art. 231-265 : congés annuels
///
/// Ce moteur est PURE — aucun accès base de données.
/// Il reçoit un <see cref="EmployeePayrollDto"/> entièrement hydraté par PayrollService (Phase 3)
/// et retourne un <see cref="PayrollCalculationResult"/>.
/// </summary>
public class PayrollCalculationEngine
{
    private readonly ILogger<PayrollCalculationEngine> _logger;

    public PayrollCalculationEngine(ILogger<PayrollCalculationEngine> logger)
    {
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POINT D'ENTRÉE
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calcule la fiche de paie complète pour un salarié.
    /// </summary>
    public PayrollCalculationResult CalculatePayroll(EmployeePayrollDto data)
    {
        _logger.LogInformation(
            "Début calcul paie — {FullName} ({Month:D2}/{Year})",
            data.FullName, data.PayMonth, data.PayYear);

        var result = new PayrollCalculationResult
        {
            EmployeeName = data.FullName,
            Month = data.PayMonth,
            Year = data.PayYear,
            AuditSteps = new List<PayrollAuditStep>(),
        };

        try
        {
            var ctx = BuildContextFromDto(data);
            RunPipeline(ctx, result);
            MapContextToResult(ctx, result);
            result.Success = true;

            _logger.LogInformation(
                "Calcul terminé — Net : {Net:N2} MAD | Coût emp. : {Cout:N2} MAD",
                result.SalaireNet, result.CoutEmployeurTotal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur calcul paie — {FullName}", data.FullName);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ADAPTATEUR : EmployeePayrollDto → PayrollCalculationContext
    // ──────────────────────────────────────────────────────────────────────────

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
            HeuresTravaillées = data.TotalWorkedHours,
            BaseSalaryHourly = data.BaseSalaryHourly ?? 0m,
            JoursFeries = 0,
            AvanceSalaire = 0m,
            InteretPretLogement = 0m,
            DisableAmo = data.DisableAmo,
        };

        // ── Jours travaillés / congés / absences ────────────────────────────

        var joursAbsents = data.Absences?
            .Where(a =>
                string.Equals(a.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(a.AbsenceType, "MATERNITE", StringComparison.OrdinalIgnoreCase)
                && (a.DurationType == "FullDay" || a.DurationType == "HalfDay"))
            .Sum(a => a.DurationType == "FullDay" ? 1m : 0.5m) ?? 0m;

        var joursConge = (int)Math.Round(
            data.Leaves?.Sum(l => l.DaysCount) ?? 0m);

        ctx.JoursConge = joursConge;
        ctx.JoursTravailles = Math.Max(0,
            PayrollConstants.WorkDaysRef - joursConge - (int)Math.Round(joursAbsents));

        // ── Heures supplémentaires par tranche ──────────────────────────────

        if (data.Overtimes != null)
        {
            foreach (var o in data.Overtimes)
            {
                if (Math.Abs(o.RateMultiplier - 1.25m) < 0.01m)
                    ctx.HSup25Pct += o.DurationInHours;
                else if (Math.Abs(o.RateMultiplier - 1.50m) < 0.01m)
                    ctx.HSup50Pct += o.DurationInHours;
                else if (Math.Abs(o.RateMultiplier - 2.00m) < 0.01m)
                    ctx.HSup100Pct += o.DurationInHours;
            }
        }

        // ── Primes imposables (SalaryComponents + PackageItems taxables) ─────

        foreach (var c in data.SalaryComponents?.Where(c => c.IsTaxable)
                          ?? Array.Empty<PayrollSalaryComponentDto>())
        {
            // La classification métier NI (ex: représentation, transport, panier...) prime
            // sur le simple flag IsTaxable pour éviter les mauvaises imputations.
            if (!TryAssignNiDedicatedCategory(ctx, c.ComponentType, c.Amount))
            {
                ctx.PrimesImposables.Add(new PrimeImposableItem
                {
                    Label = c.ComponentType,
                    Montant = MontantPanierProratiseSiLibelle(c.ComponentType, c.Amount, ctx.JoursTravailles)
                });
            }
        }

        foreach (var p in data.PackageItems?.Where(p => p.IsTaxable)
                          ?? Array.Empty<PayrollPackageItemDto>())
        {
            if (!TryAssignNiDedicatedCategory(ctx, p.Label, p.DefaultValue))
            {
                ctx.PrimesImposables.Add(new PrimeImposableItem
                {
                    Label = p.Label,
                    Montant = MontantPanierProratiseSiLibelle(p.Label, p.DefaultValue, ctx.JoursTravailles)
                });
            }
        }

        // ── CIMR ─────────────────────────────────────────────────────────────
        // Les fiches employés / UI saisissent souvent « 4,5 » pour 4,5 % ; le moteur attend une fraction (0,045).

        if (data.CimrNumber != null
            && data.CimrEmployeeRate.HasValue
            && data.CimrCompanyRate.HasValue)
        {
            var ts = NormalizeCimrRateFraction(data.CimrEmployeeRate.Value);
            var tp = NormalizeCimrRateFraction(data.CimrCompanyRate.Value);
            ctx.RegimeCimr = tp > ts ? RegimeCimr.AL_KAMIL : RegimeCimr.AL_MOUNASSIB;
            ctx.CimrTauxSalarial = ts;
            ctx.CimrTauxPatronal = tp;
        }

        // ── Mutuelle / assurance privée ──────────────────────────────────────

        if (data.HasPrivateInsurance && data.PrivateInsuranceRate.HasValue)
        {
            // Taux partagé 50/50 salarié-employeur si non détaillé
            ctx.MutuelleSalariale = data.PrivateInsuranceRate.Value / 2m;
            ctx.MutuellePatronale = data.PrivateInsuranceRate.Value / 2m;
        }

        // ── Indemnités non imposables (composantes / lignes package non taxables reconnues) ──
        // Les libellés qui ne correspondent à aucune catégorie NI (transport, panier, etc.) ne sont plus
        // routés vers « NiAutres » (exonération totale en module 04) : ce sont en pratique des primes /
        // éléments de salaire et doivent entrer dans le brut imposable (TotalPrimesImposables).

        foreach (var c in data.SalaryComponents?.Where(c => !c.IsTaxable)
                          ?? Array.Empty<PayrollSalaryComponentDto>())
        {
            if (!TryAssignNiDedicatedCategory(ctx, c.ComponentType, c.Amount))
                ctx.PrimesImposables.Add(new PrimeImposableItem
                {
                    Label = string.IsNullOrWhiteSpace(c.ComponentType) ? "Composante salaire" : c.ComponentType,
                    Montant = c.Amount
                });
        }

        foreach (var p in data.PackageItems?.Where(p => !p.IsTaxable)
                          ?? Array.Empty<PayrollPackageItemDto>())
        {
            if (!TryAssignNiDedicatedCategory(ctx, p.Label, p.DefaultValue))
                ctx.PrimesImposables.Add(new PrimeImposableItem
                {
                    Label = string.IsNullOrWhiteSpace(p.Label) ? "Ligne package" : p.Label,
                    Montant = p.DefaultValue
                });
        }

        return ctx;
    }

    /// <summary>
    /// Affecte le montant à un panier d’indemnité non imposable reconnu (plafonds DGI au module 04).
    /// Retourne <c>false</c> si le libellé ne correspond à aucune catégorie : le montant doit alors être traité comme prime imposable.
    /// </summary>
    private static bool TryAssignNiDedicatedCategory(PayrollCalculationContext ctx, string? label, decimal amount)
    {
        var u = NormalizeLabel(label);

        if (u.Contains("TRANSPORT") && !u.Contains("KILOM"))
        {
            ctx.NiTransport += amount;
            return true;
        }
        if (u.Contains("KILOM"))
        {
            ctx.NiKilometrique += amount;
            return true;
        }
        if (u.Contains("TOURNEE"))
        {
            ctx.NiTournee += amount;
            return true;
        }
        if (u.Contains("REPRES"))
        {
            ctx.NiRepresentation += amount;
            return true;
        }
        if (u.Contains("PANIER"))
        {
            ctx.NiPanier += MontantPanierProratise(amount, ctx.JoursTravailles);
            return true;
        }
        if (u.Contains("CAISSE"))
        {
            ctx.NiCaisse += amount;
            return true;
        }
        if (u.Contains("SALISSURE"))
        {
            ctx.NiSalissure += amount;
            return true;
        }
        if (u.Contains("LAIT"))
        {
            ctx.NiLait += amount;
            return true;
        }
        if (u.Contains("OUTILLAGE"))
        {
            ctx.NiOutillage += amount;
            return true;
        }
        if (u.Contains("AIDE") && u.Contains("MEDIC"))
        {
            ctx.NiAideMedicale += amount;
            return true;
        }
        if (u.Contains("GRATIF") || u.Contains("SOCIAL"))
        {
            ctx.NiGratifSociale += amount;
            return true;
        }

        return false;
    }

    private static string NormalizeLabel(string? raw)
    {
        var s = (raw ?? string.Empty).Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    /// <summary>
    /// Panier : le montant en base est la référence pour un mois à <see cref="PayrollConstants.WorkDaysRef"/> jours ;
    /// le montant période = référence × (jours travaillés / 26), avec
    /// jours travaillés = 26 − jours de congé − jours d’absence (déjà calculé sur le DTO).
    /// </summary>
    private static decimal MontantPanierProratise(decimal montantReferenceMois26j, int joursTravailles)
    {
        var j = Math.Max(0, joursTravailles);
        return Round(montantReferenceMois26j * j / PayrollConstants.WorkDaysRef);
    }

    private static decimal MontantPanierProratiseSiLibelle(string? label, decimal amount, int joursTravailles)
    {
        if (string.IsNullOrWhiteSpace(label))
            return amount;
        return label.ToUpperInvariant().Contains("PANIER")
            ? MontantPanierProratise(amount, joursTravailles)
            : amount;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PIPELINE @PIPELINE calcul_fiche_paie (STEP 01 → 13)
    // ──────────────────────────────────────────────────────────────────────────

    private void RunPipeline(PayrollCalculationContext ctx, PayrollCalculationResult result)
    {
        // MODULE 01 — Ancienneté
        Module01_Anciennete(ctx);
        Audit(result, 1, "Module01_Anciennete",
            "PrimeAnciennete = SalaireBase26j × TauxAnciennete ; fallback horaire si SalaireBase=0",
            In("SalaireBase26j", ctx.SalaireBase26j,
               "DateEmbauche", ctx.DateEmbauche.ToString("yyyy-MM-dd"),
               "AncienneteAnnees", ctx.AncienneteAnnees,
               "TauxAnciennete", ctx.TauxAnciennete),
            Out("PrimeAnciennete", ctx.PrimeAnciennete));

        // MODULE 02 — Présence
        Module02_Presence(ctx);
        Audit(result, 2, "Module02_Presence",
            "SalaireBaseMensuel = SalaireBase26j si JoursPayés≥26, sinon SalaireBase26j × (JoursPayésTotal / 26)",
            In("SalaireBase26j", ctx.SalaireBase26j,
               "JoursTravailles", ctx.JoursTravailles,
               "JoursFeries", ctx.JoursFeries,
               "JoursConge", ctx.JoursConge,
               "JoursPayesTotal", ctx.JoursPayesTotal),
            Out("SalaireBaseMensuel", ctx.SalaireBaseMensuel));

        // MODULE 03 — Heures supplémentaires
        Module03_HeuresSupplementaires(ctx);
        Audit(result, 3, "Module03_HeuresSupplementaires",
            "TauxHoraire = (SalaireBaseMensuel + PrimeAnciennete) / HeuresMois ; MontHsupp = H × TauxH × (1.25|1.50|2.00)",
            In("SalaireBaseMensuel", ctx.SalaireBaseMensuel,
               "PrimeAnciennete", ctx.PrimeAnciennete,
               "HeuresMois", ctx.HeuresMois,
               "HSup25Pct", ctx.HSup25Pct, "HSup50Pct", ctx.HSup50Pct, "HSup100Pct", ctx.HSup100Pct),
            Out("TauxHoraire", ctx.TauxHoraire,
                "MontHsupp25", ctx.MontHsupp25, "MontHsupp50", ctx.MontHsupp50,
                "MontHsupp100", ctx.MontHsupp100, "TotalHsupp", ctx.TotalHsupp));

        // MODULE 04 — Indemnités non imposables
        Module04_IndemnitesNonImposables(ctx);
        Audit(result, 4, "Module04_IndemnitesNonImposables",
            "Scission par type avec plafonds DGI ; TotalNiExonere + TotalNiExcedentImposable",
            In("NiTransport", ctx.NiTransport, "NiPanier", ctx.NiPanier,
               "NiRepresentation", ctx.NiRepresentation, "JoursTravailles", ctx.JoursTravailles),
            Out("TotalNiExonere", ctx.TotalNiExonere,
                "TotalNiExcedentImposable", ctx.TotalNiExcedentImposable));

        // MODULE 05 — Salaire brut imposable
        Module05_SalaireBrutImposable(ctx);
        Audit(result, 5, "Module05_SalaireBrutImposable",
            "SBI = SalaireBaseMensuel + PrimeAnc + TotalHsupp + TotalPrimesImposables + TotalNiExcedentImposable",
            In("SalaireBaseMensuel", ctx.SalaireBaseMensuel,
               "PrimeAnciennete", ctx.PrimeAnciennete,
               "TotalHsupp", ctx.TotalHsupp,
               "TotalPrimesImposables", ctx.TotalPrimesImposables,
               "TotalNiExcedentImposable", ctx.TotalNiExcedentImposable),
            Out("SalaireBrutImposable", ctx.SalaireBrutImposable));

        // MODULE 06 — CNSS (Décret 2.25.266 — 2025)
        Module06_Cnss(ctx);
        Audit(result, 6, "Module06_Cnss",
            "BaseCnss = min(SBI, 6000) ; RG sal 4.48%, AMO sal 2.26% ; Patronal : RG 8.98%, AF 6.4%, FP 1.6%, AMO 2.26%+1.85%",
            In("SalaireBrutImposable", ctx.SalaireBrutImposable, "DisableAmo", ctx.DisableAmo),
            Out("BaseCnssRg", ctx.BaseCnssRg,
                "CnssRgSalarial", ctx.CnssRgSalarial, "CnssAmoSalarial", ctx.CnssAmoSalarial,
                "TotalCnssSalarial", ctx.TotalCnssSalarial, "TotalCnssPatronal", ctx.TotalCnssPatronal));

        // MODULE 07 — CIMR
        Module07_Cimr(ctx);
        Audit(result, 7, "Module07_Cimr",
            "AL_KAMIL : base = SBI×Taux ; AL_MOUNASSIB : base = max(0, SBI-6000)×Taux",
            In("RegimeCimr", ctx.RegimeCimr.ToString(),
               "SalaireBrutImposable", ctx.SalaireBrutImposable,
               "CimrTauxSalarial", ctx.CimrTauxSalarial, "CimrTauxPatronal", ctx.CimrTauxPatronal),
            Out("BaseCimr", ctx.BaseCimr, "CimrSalarial", ctx.CimrSalarial, "CimrPatronal", ctx.CimrPatronal));

        // MODULE 08 — Frais professionnels (LF 2025)
        Module08_FraisProfessionnels(ctx);
        Audit(result, 8, "Module08_FraisProfessionnels",
            "Base≤6500 → 35% ; Base>6500 → 25% ; Plafond 2916.67 MAD (LF 2025)",
            In("SalaireBrutImposable", ctx.SalaireBrutImposable),
            Out("TauxFp", ctx.TauxFp, "PlafondFp", ctx.PlafondFp, "MontantFp", ctx.MontantFp));

        // MODULE 09 — Base IR (RNI)
        Module09_BaseIr(ctx);
        Audit(result, 9, "Module09_BaseIr",
            "RNI = SBI - TotalCnssSal - CimrSal - MutuelleSal - MontantFp - IntLogement",
            In("SalaireBrutImposable", ctx.SalaireBrutImposable,
               "TotalCnssSalarial", ctx.TotalCnssSalarial,
               "CimrSalarial", ctx.CimrSalarial,
               "MontantFp", ctx.MontantFp,
               "MutuelleSalarialeAmount", ctx.MutuelleSalarialeAmount,
               "InteretPretLogement", ctx.InteretPretLogement),
            Out("RevenuNetImposable", ctx.RevenuNetImposable));

        // MODULE 10 — IR (barème mensuel 2026)
        Module10_Ir(ctx);
        Audit(result, 10, "Module10_Ir",
            "Barème IR mensuel 2026 ; DeductionFamille = SituationFam × 30 ; IrFinal = max(0, RNI×Taux - DedBareme - DedFamille)",
            In("RevenuNetImposable", ctx.RevenuNetImposable,
               "SituationFam", ctx.SituationFam,
               "TauxIr", ctx.TauxIr,
               "DeductionBareme", ctx.DeductionBareme, "DeductionFamille", ctx.DeductionFamille),
            Out("IrBrut", ctx.IrBrut, "IrFinal", ctx.IrFinal));

        // MODULE 11 — Net à payer
        Module11_NetAPayer(ctx);
        Audit(result, 11, "Module11_NetAPayer",
            "TotalRetenues = CNSS + CIMR + Mutuelle + IR + Avance ; SalaireNet = SBI - TotalRetenues + TotalNiExonere",
            In("SalaireBrutImposable", ctx.SalaireBrutImposable,
               "TotalCnssSalarial", ctx.TotalCnssSalarial,
               "CimrSalarial", ctx.CimrSalarial,
               "MutuelleSalarialeAmount", ctx.MutuelleSalarialeAmount,
               "IrFinal", ctx.IrFinal,
               "AvanceSalaire", ctx.AvanceSalaire,
               "TotalNiExonere", ctx.TotalNiExonere),
            Out("TotalRetenuesSalariales", ctx.TotalRetenuesSalariales, "SalaireNet", ctx.SalaireNet));

        // MODULE 12 — Coût employeur
        Module12_CoutEmployeur(ctx);
        Audit(result, 12, "Module12_CoutEmployeur",
            "TotalChargesPatronales = TotalCnssPatronal + CimrPatronal + MutuellePatronale ; CoutEmp = SBI + Charges + NiExo",
            In("SalaireBrutImposable", ctx.SalaireBrutImposable,
               "TotalCnssPatronal", ctx.TotalCnssPatronal,
               "CimrPatronal", ctx.CimrPatronal,
               "MutuellePatronaleAmount", ctx.MutuellePatronaleAmount,
               "TotalNiExonere", ctx.TotalNiExonere),
            Out("TotalChargesPatronales", ctx.TotalChargesPatronales,
                "CoutEmployeurTotal", ctx.CoutEmployeurTotal));

        // MODULE 13 — Congés annuels (Code du Travail Art. 231-265)
        Module13_CongesAnnuels(ctx);
        Audit(result, 13, "Module13_CongesAnnuels",
            "<5a→18j | <10a→19.5j | <15a→21j | <20a→22.5j | <25a→24j | <30a→25.5j | <35a→27j | <40a→28.5j | ≥40a→30j",
            In("AncienneteAnnees", ctx.AncienneteAnnees),
            Out("JoursCongeAnnuels", ctx.JoursCongeAnnuels));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // MODULES
    // ──────────────────────────────────────────────────────────────────────────

    private static void Module01_Anciennete(PayrollCalculationContext ctx)
    {
        var dernierJourMois = new DateTime(
            ctx.AnneePaie, ctx.MoisPaie,
            DateTime.DaysInMonth(ctx.AnneePaie, ctx.MoisPaie));

        ctx.AncienneteAnnees = (int)((dernierJourMois - ctx.DateEmbauche).TotalDays / 365.25);

        ctx.TauxAnciennete = ctx.AncienneteAnnees switch
        {
            < 2 => 0.00m,
            < 5 => 0.05m,
            < 12 => 0.10m,
            // Plafond atteint dès 15 ans selon les tests.
            < 15 => 0.15m,
            _ => 0.20m,
        };
        ctx.PrimeAnciennteRate = ctx.TauxAnciennete;

        // Fallback taux horaire si salaire mensuel non fourni
        ctx.PrimeAnciennete = ctx.SalaireBase26j <= 0
                           && ctx.BaseSalaryHourly > 0
                           && ctx.HeuresTravaillées > 0
            ? Round(ctx.BaseSalaryHourly * ctx.HeuresTravaillées * ctx.TauxAnciennete)
            : Round(ctx.SalaireBase26j * ctx.TauxAnciennete);
    }

    private static void Module02_Presence(PayrollCalculationContext ctx)
    {
        ctx.JoursPayesTotal = ctx.JoursTravailles + ctx.JoursFeries + ctx.JoursConge;

        if (ctx.SalaireBase26j <= 0 && ctx.BaseSalaryHourly > 0 && ctx.HeuresTravaillées > 0)
        {
            // Salarié payé à l'heure
            ctx.SalaireBaseMensuel = Round(ctx.HeuresTravaillées * ctx.BaseSalaryHourly);
        }
        else
        {
            ctx.SalaireBaseMensuel = ctx.JoursPayesTotal >= PayrollConstants.WorkDaysRef
                ? ctx.SalaireBase26j
                : Round(ctx.SalaireBase26j * ctx.JoursPayesTotal / PayrollConstants.WorkDaysRef);
        }
    }

    private static void Module03_HeuresSupplementaires(PayrollCalculationContext ctx)
    {
        ctx.TauxHoraire = ctx.BaseSalaryHourly > 0
            ? Round(ctx.BaseSalaryHourly, 4)
            : Round((ctx.SalaireBaseMensuel + ctx.PrimeAnciennete) / ctx.HeuresMois, 4);

        ctx.MontHsupp25 = Round(ctx.HSup25Pct * ctx.TauxHoraire * 1.25m);
        ctx.MontHsupp50 = Round(ctx.HSup50Pct * ctx.TauxHoraire * 1.50m);
        ctx.MontHsupp100 = Round(ctx.HSup100Pct * ctx.TauxHoraire * 2.00m);
        ctx.TotalHsupp = ctx.MontHsupp25 + ctx.MontHsupp50 + ctx.MontHsupp100;
    }

    private static void Module04_IndemnitesNonImposables(PayrollCalculationContext ctx)
    {
        // Transport (urbain)
        var niTransportExo = Math.Min(ctx.NiTransport, PayrollConstants.PLAFOND_NI_TRANSPORT);
        var niTransportImp = Math.Max(0m, ctx.NiTransport - PayrollConstants.PLAFOND_NI_TRANSPORT);

        // Kilométrique — totalement exonéré (CGI)
        var niKmExo = ctx.NiKilometrique;

        // Tournée
        var niTourneeExo = Math.Min(ctx.NiTournee, PayrollConstants.PLAFOND_NI_TOURNEE);
        var niTourneeImp = Math.Max(0m, ctx.NiTournee - PayrollConstants.PLAFOND_NI_TOURNEE);

        // Représentation (10% du salaire de base contractuel)
        var plafondRep = ctx.SalaireBase26j * PayrollConstants.PLAFOND_NI_REPRESENTATION;
        var niRepExo = Math.Min(ctx.NiRepresentation, plafondRep);
        var niRepImp = Math.Max(0m, ctx.NiRepresentation - plafondRep);

        // Panier (par jour travaillé)
        var plafondPanier = PayrollConstants.PLAFOND_NI_PANIER_JOUR * ctx.JoursTravailles;
        var niPanierExo = Math.Min(ctx.NiPanier, plafondPanier);
        var niPanierImp = Math.Max(0m, ctx.NiPanier - plafondPanier);

        // Caisse
        var niCaisseExo = Math.Min(ctx.NiCaisse, PayrollConstants.PLAFOND_NI_CAISSE_DGI);
        var niCaisseImp = Math.Max(0m, ctx.NiCaisse - PayrollConstants.PLAFOND_NI_CAISSE_DGI);

        // Lait
        var niLaitExo = Math.Min(ctx.NiLait, PayrollConstants.PLAFOND_NI_LAIT_DGI);
        var niLaitImp = Math.Max(0m, ctx.NiLait - PayrollConstants.PLAFOND_NI_LAIT_DGI);

        // Outillage
        var niOutillageExo = Math.Min(ctx.NiOutillage, PayrollConstants.PLAFOND_NI_OUTILLAGE_DGI);
        var niOutillageImp = Math.Max(0m, ctx.NiOutillage - PayrollConstants.PLAFOND_NI_OUTILLAGE_DGI);

        // Salissure
        var niSalissureExo = Math.Min(ctx.NiSalissure, PayrollConstants.PLAFOND_NI_SALISSURE_DGI);
        var niSalissureImp = Math.Max(0m, ctx.NiSalissure - PayrollConstants.PLAFOND_NI_SALISSURE_DGI);

        // Aide médicale — totalement exonérée
        var niAideExo = ctx.NiAideMedicale;

        // Gratification sociale
        var niGratifExo = Math.Min(ctx.NiGratifSociale, PayrollConstants.PLAFOND_NI_GRATIF_DGI);
        var niGratifImp = Math.Max(0m, ctx.NiGratifSociale - PayrollConstants.PLAFOND_NI_GRATIF_DGI);

        // Autres NI non classifiées — exonérées par défaut
        var niAutresExo = ctx.NiAutres;

        ctx.TotalNiExonere =
            niTransportExo + niKmExo + niTourneeExo + niRepExo + niPanierExo
            + niCaisseExo + niLaitExo + niOutillageExo + niSalissureExo
            + niAideExo + niGratifExo + niAutresExo;

        ctx.TotalNiExcedentImposable =
            niTransportImp + niTourneeImp + niRepImp + niPanierImp
            + niCaisseImp + niLaitImp + niOutillageImp + niSalissureImp + niGratifImp;

        ctx.NiLineTransport = niTransportExo;
        ctx.NiLineKilometrique = niKmExo;
        ctx.NiLineTournee = niTourneeExo;
        ctx.NiLineRepresentation = niRepExo;
        ctx.NiLinePanier = niPanierExo;
        ctx.NiLineCaisse = niCaisseExo;
        ctx.NiLineLait = niLaitExo;
        ctx.NiLineOutillage = niOutillageExo;
        ctx.NiLineSalissure = niSalissureExo;
        ctx.NiLineAideMedicale = niAideExo;
        ctx.NiLineGratifSociale = niGratifExo;
        ctx.NiLineAutres = niAutresExo;
    }

    private static void Module05_SalaireBrutImposable(PayrollCalculationContext ctx)
    {
        ctx.TotalPrimesImposables = ctx.PrimesImposables.Sum(p => p.Montant);
        ctx.SalaireBrutImposable =
            ctx.SalaireBaseMensuel
            + ctx.PrimeAnciennete
            + ctx.TotalHsupp
            + ctx.TotalPrimesImposables
            + ctx.TotalNiExcedentImposable;
    }

    private static void Module06_Cnss(PayrollCalculationContext ctx)
    {
        // Salarial
        ctx.BaseCnssRg = Math.Min(ctx.SalaireBrutImposable, PayrollConstants.PLAFOND_CNSS_MENSUEL);
        ctx.CnssRgSalarial = Round(ctx.BaseCnssRg * PayrollConstants.CNSS_RG_SALARIAL);
        ctx.CnssAmoSalarial = ctx.DisableAmo ? 0m
                              : Round(ctx.SalaireBrutImposable * PayrollConstants.CNSS_AMO_SALARIAL);
        ctx.TotalCnssSalarial = ctx.CnssRgSalarial + ctx.CnssAmoSalarial;

        // Patronal
        ctx.CnssRgPatronal = Round(ctx.BaseCnssRg * PayrollConstants.CNSS_RG_PATRONAL);
        ctx.CnssAllocFamPatronal = Round(ctx.SalaireBrutImposable * PayrollConstants.CNSS_ALLOC_FAM_PAT);
        ctx.CnssFpPatronal = Round(ctx.SalaireBrutImposable * PayrollConstants.CNSS_FP_PAT);
        ctx.CnssAmoPatronal = ctx.DisableAmo ? 0m
                                    : Round(ctx.SalaireBrutImposable * PayrollConstants.CNSS_AMO_PATRONAL);
        ctx.CnssParticipAmoPatronal = ctx.DisableAmo ? 0m
                                    : Round(ctx.SalaireBrutImposable * PayrollConstants.CNSS_AMO_PARTICIPATION_PAT);
        ctx.TotalCnssPatronal =
            ctx.CnssRgPatronal + ctx.CnssAllocFamPatronal + ctx.CnssFpPatronal
            + ctx.CnssAmoPatronal + ctx.CnssParticipAmoPatronal;
    }

    /// <summary>
    /// Convertit un taux CIMR saisi en pourcentage (ex. 4,5 pour 4,5 %) en fraction utilisée par les modules (0,045).
    /// Les valeurs déjà en fraction (≤ 1) sont laissées inchangées.
    /// </summary>
    private static decimal NormalizeCimrRateFraction(decimal value)
    {
        if (value > 1m && value <= 100m)
            return value / 100m;
        return value;
    }

    private static void Module07_Cimr(PayrollCalculationContext ctx)
    {
        switch (ctx.RegimeCimr)
        {
            case RegimeCimr.AUCUN:
                ctx.BaseCimr = ctx.CimrSalarial = ctx.CimrPatronal = 0m;
                break;

            case RegimeCimr.AL_KAMIL:
                ctx.BaseCimr = ctx.SalaireBrutImposable;
                ctx.CimrSalarial = Round(ctx.SalaireBrutImposable * ctx.CimrTauxSalarial);
                ctx.CimrPatronal = Round(ctx.SalaireBrutImposable * ctx.CimrTauxPatronal);
                break;

            case RegimeCimr.AL_MOUNASSIB:
                var baseMounassib = Math.Max(0m, ctx.SalaireBrutImposable - PayrollConstants.PLAFOND_CNSS_MENSUEL);
                ctx.BaseCimr = baseMounassib;
                ctx.CimrSalarial = Round(baseMounassib * ctx.CimrTauxSalarial);
                ctx.CimrPatronal = Round(baseMounassib * ctx.CimrTauxPatronal);
                break;
        }
    }

    private static void Module08_FraisProfessionnels(PayrollCalculationContext ctx)
    {
        ctx.BaseFp = ctx.SalaireBrutImposable;

        (ctx.TauxFp, ctx.PlafondFp) = ctx.BaseFp <= PayrollConstants.FP_SEUIL_35
            ? (PayrollConstants.FP_TAUX_35, PayrollConstants.FP_PLAFOND_35)
            : (PayrollConstants.FP_TAUX_25, PayrollConstants.FP_PLAFOND_25);

        ctx.MontantFp = Math.Min(Round(ctx.BaseFp * ctx.TauxFp), ctx.PlafondFp);
    }

    private static void Module09_BaseIr(PayrollCalculationContext ctx)
    {
        ctx.MutuelleSalarialeAmount = Round(ctx.SalaireBrutImposable * ctx.MutuelleSalariale);
        ctx.MutuellePatronaleAmount = Round(ctx.SalaireBrutImposable * ctx.MutuellePatronale);

        ctx.RevenuNetImposable = Math.Max(0m,
            ctx.SalaireBrutImposable
            - ctx.TotalCnssSalarial
            - ctx.CimrSalarial
            - ctx.MutuelleSalarialeAmount
            - ctx.MontantFp
            - ctx.InteretPretLogement);
    }

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

        ctx.IrBrut = Round(ctx.RevenuNetImposable * ctx.TauxIr);
        ctx.IrFinal = Math.Max(0m, Round(ctx.IrBrut - ctx.DeductionBareme - ctx.DeductionFamille));
    }

    private static void Module11_NetAPayer(PayrollCalculationContext ctx)
    {
        ctx.TotalRetenuesSalariales =
            ctx.TotalCnssSalarial
            + ctx.CimrSalarial
            + ctx.MutuelleSalarialeAmount
            + ctx.IrFinal
            + ctx.AvanceSalaire;

        ctx.SalaireNet = Round(
            ctx.SalaireBrutImposable
            - ctx.TotalRetenuesSalariales
            + ctx.TotalNiExonere);
    }

    private static void Module12_CoutEmployeur(PayrollCalculationContext ctx)
    {
        ctx.TotalChargesPatronales =
            ctx.TotalCnssPatronal
            + ctx.CimrPatronal
            + ctx.MutuellePatronaleAmount;

        ctx.CoutEmployeurTotal = Round(
            ctx.SalaireBrutImposable
            + ctx.TotalChargesPatronales
            + ctx.TotalNiExonere);
    }

    private static void Module13_CongesAnnuels(PayrollCalculationContext ctx)
    {
        // Code du Travail Art. 231-265 : 1.5 j/mois de service, majoré par ancienneté
        ctx.JoursCongeAnnuels = ctx.AncienneteAnnees switch
        {
            < 5 => 18.0m,
            < 10 => 19.5m,
            < 15 => 21.0m,
            < 20 => 22.5m,
            < 25 => 24.0m,
            < 30 => 25.5m,
            < 35 => 27.0m,
            < 40 => 28.5m,
            _ => 30.0m,
        };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // MAPPER : context → result
    // ──────────────────────────────────────────────────────────────────────────

    private static void MapContextToResult(PayrollCalculationContext ctx, PayrollCalculationResult r)
    {
        // Présence
        r.JoursTravailles = ctx.JoursTravailles;
        r.JoursConge = ctx.JoursConge;
        r.JoursFeries = ctx.JoursFeries;
        r.JoursCongeAnnuels = ctx.JoursCongeAnnuels;

        // Salaires
        r.SalaireBase26j = ctx.SalaireBase26j;
        r.SalaireBaseMensuel = ctx.SalaireBaseMensuel;
        r.AncienneteAnnees = ctx.AncienneteAnnees;
        r.TauxAnciennete = ctx.TauxAnciennete;
        r.PrimeAnciennete = ctx.PrimeAnciennete;

        // Heures sup
        r.MontHsupp25 = ctx.MontHsupp25;
        r.MontHsupp50 = ctx.MontHsupp50;
        r.MontHsupp100 = ctx.MontHsupp100;
        r.TotalHsupp = ctx.TotalHsupp;

        // NI + Brut
        r.TotalPrimesImposables = ctx.TotalPrimesImposables;
        var pi = ctx.PrimesImposables;
        r.PrimeImposable1 = pi.Count > 0 ? pi[0].Montant : 0m;
        r.PrimeImposable2 = pi.Count > 1 ? pi[1].Montant : 0m;
        r.PrimeImposable3 = pi.Count > 2 ? pi[2].Montant : 0m;
        r.TotalNiExonere = ctx.TotalNiExonere;
        r.TotalNiExcedentImposable = ctx.TotalNiExcedentImposable;
        r.SalaireBrutImposable = ctx.SalaireBrutImposable;

        r.NiLineTransport = ctx.NiLineTransport;
        r.NiLineKilometrique = ctx.NiLineKilometrique;
        r.NiLineTournee = ctx.NiLineTournee;
        r.NiLineRepresentation = ctx.NiLineRepresentation;
        r.NiLinePanier = ctx.NiLinePanier;
        r.NiLineCaisse = ctx.NiLineCaisse;
        r.NiLineLait = ctx.NiLineLait;
        r.NiLineOutillage = ctx.NiLineOutillage;
        r.NiLineSalissure = ctx.NiLineSalissure;
        r.NiLineAideMedicale = ctx.NiLineAideMedicale;
        r.NiLineGratifSociale = ctx.NiLineGratifSociale;
        r.NiLineAutres = ctx.NiLineAutres;

        // CNSS salarial
        r.BaseCnssRg = ctx.BaseCnssRg;
        r.CnssRgSalarial = ctx.CnssRgSalarial;
        r.CnssAmoSalarial = ctx.CnssAmoSalarial;
        r.TotalCnssSalarial = ctx.TotalCnssSalarial;

        // CNSS patronal
        r.CnssRgPatronal = ctx.CnssRgPatronal;
        r.CnssAllocFamPatronal = ctx.CnssAllocFamPatronal;
        r.CnssFpPatronal = ctx.CnssFpPatronal;
        r.CnssAmoPatronal = ctx.CnssAmoPatronal;
        r.CnssParticipAmoPatronal = ctx.CnssParticipAmoPatronal;
        r.TotalCnssPatronal = ctx.TotalCnssPatronal;

        // CIMR
        r.BaseCimr = ctx.BaseCimr;
        r.CimrSalarial = ctx.CimrSalarial;
        r.CimrPatronal = ctx.CimrPatronal;

        // Mutuelle
        r.MutuelleSalarialeAmount = ctx.MutuelleSalarialeAmount;
        r.MutuellePatronaleAmount = ctx.MutuellePatronaleAmount;

        // Frais pro
        r.TauxFp = ctx.TauxFp;
        r.MontantFp = ctx.MontantFp;

        // IR
        r.RevenuNetImposable = ctx.RevenuNetImposable;
        r.TauxIr = ctx.TauxIr;
        r.DeductionFamille = ctx.DeductionFamille;
        r.IrBrut = ctx.IrBrut;
        r.IrFinal = ctx.IrFinal;

        // Net
        r.TotalRetenuesSalariales = ctx.TotalRetenuesSalariales;
        r.SalaireNet = ctx.SalaireNet;
        r.AvanceSalaire = ctx.AvanceSalaire;
        r.InteretPretLogement = ctx.InteretPretLogement;

        // Coût employeur
        r.TotalChargesPatronales = ctx.TotalChargesPatronales;
        r.CoutEmployeurTotal = ctx.CoutEmployeurTotal;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ──────────────────────────────────────────────────────────────────────────

    private static decimal Round(decimal value, int decimals = PayrollConstants.RoundingDecimals)
        => Math.Round(value, decimals, MidpointRounding.AwayFromZero);

    private void Audit(
        PayrollCalculationResult result,
        int step, string module, string formula,
        Dictionary<string, object> inputs,
        Dictionary<string, object> outputs)
    {
        result.AuditSteps!.Add(new PayrollAuditStep
        {
            StepOrder = step,
            ModuleName = module,
            FormulaDescription = formula,
            InputsJson = JsonSerializer.Serialize(inputs),
            OutputsJson = JsonSerializer.Serialize(outputs),
        });

        _logger.LogDebug("PAIE [{Step:D2}] {Module} → {Outputs}",
            step, module, JsonSerializer.Serialize(outputs));
    }

    private static Dictionary<string, object> In(params object[] pairs) => BuildDict(pairs);
    private static Dictionary<string, object> Out(params object[] pairs) => BuildDict(pairs);

    private static Dictionary<string, object> BuildDict(object[] pairs)
    {
        var d = new Dictionary<string, object>();
        for (var i = 0; i + 1 < pairs.Length; i += 2)
            d[pairs[i].ToString()!] = pairs[i + 1];
        return d;
    }
}
