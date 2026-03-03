using Microsoft.Extensions.Logging;
using payzen_backend.Models.Employee.Dtos;
using payzen_backend.Services.Payroll;
using Xunit;

namespace payzen_backend.Tests;

/// <summary>
/// Tests alignés sur les @EXAMPLE et CHECKPOINTS du DSL regles_paie.txt v3.1
/// Tolérance : 0.05 MAD (comme indiqué dans le guide IA du DSL)
/// </summary>
public class PayrollCalculationEngineDslTests
{
    private const decimal Tolerance = 0.05m;

    private static PayrollCalculationEngine CreateEngine()
    {
        var logger = new LoggerFactory().CreateLogger<PayrollCalculationEngine>();
        return new PayrollCalculationEngine(logger);
    }

    /// <summary>
    /// @EXAMPLE salarie_9000_5ans — Salarié 9 000 MAD/mois, 5 ans d'ancienneté,
    /// 26 jours travaillés, sans CIMR, sans NI, sans charge de famille
    /// </summary>
    [Fact]
    public void Salarie_9000_5ans_Checkpoints()
    {
        var data = new EmployeePayrollDto
        {
            FullName = "Test 9000",
            PayMonth = 2,
            PayYear = 2026,
            BaseSalary = 9000m,
            ContractStartDate = new DateTime(2021, 2, 1),
            AncienneteYears = 5,
            NumberOfChildren = 0,
            HasSpouse = false,
            SalaryComponents = new List<PayrollSalaryComponentDto>(),
            PackageItems = new List<PayrollPackageItemDto>(),
            Absences = new List<PayrollAbsenceDto>(),
            Overtimes = new List<PayrollOvertimeDto>(),
            Leaves = new List<PayrollLeaveDto>()
        };

        var engine = CreateEngine();
        var result = engine.CalculatePayroll(data);

        Assert.True(result.Success, result.ErrorMessage ?? "Erreur calcul");

        // MODULE 01 — prime ancienneté = 9000 × 10% = 900
        AssertEqual(900.00m, result.PrimeAnciennete, "CP01_prime_anciennete");

        // MODULE 02 — salaire base mensuel = 9000 (26/26)
        AssertEqual(9000.00m, result.SalaireBase, "CP02_salaire_base_mensuel");

        // MODULE 05 — brut imposable = 9000 + 900 + 0 = 9900
        AssertEqual(9900.00m, result.BrutImposable, "CP05_salaire_brut_imposable");

        // MODULE 06 — CNSS (base_cnss_rg = MIN(9900,6000) = 6000)
        AssertEqual(268.80m, result.CnssRgSalarial, "CP06_cnss_rg_salarial");
        AssertEqual(223.74m, result.AmoSalarial, "CP06_cnss_amo_salarial");
        AssertEqual(492.54m, result.TotalCotisationsSalariales, "CP06_total_cnss_salarial");

        // MODULE 08 — FP : base_fp = 9900, taux 25%, montant = MIN(2475, 2500) = 2475
        AssertEqual(2475.00m, result.FraisProfessionnels, "CP08_montant_fp");

        // MODULE 09 — RNI = 9900 − 492.54 − 0 − 0 − 2475 − 0 = 6932.46
        AssertEqual(6932.46m, result.RevenuNetImposable, "CP09_revenu_net_imposable");

        // MODULE 10 — IR 30%, déduction 1500, ir_final = 579.74
        AssertEqual(579.74m, result.IR, "CP10_ir_final");

        // MODULE 11 — total retenues = 492.54 + 0 + 0 + 579.74 = 1072.28 ; net = 9900 − 1072.28 = 8827.72
        AssertEqual(8827.72m, result.SalaireNetAvantArrondi, "CP11_salaire_net_avant_arrondi");
        AssertEqual(8828.00m, result.SalaireNet, "CP11_salaire_net_arrondi");

        // MODULE 12 — coût employeur
        AssertEqual(1737.69m, result.TotalCotisationsPatronales, "CP12_total_cnss_patronal");
        AssertEqual(11637.69m, result.CoutEmployeurTotal, "CP12_cout_employeur_total");
    }

    /// <summary>
    /// @EXAMPLE salarie_avec_5_primes — 8000 base, 3 ans ancienneté, 5 primes (3000 total), 2 personnes à charge
    /// </summary>
    [Fact]
    public void Salarie_avec_5_primes_Checkpoints()
    {
        var data = new EmployeePayrollDto
        {
            FullName = "Test 5 primes",
            PayMonth = 2,
            PayYear = 2026,
            BaseSalary = 8000m,
            ContractStartDate = new DateTime(2023, 2, 1),
            AncienneteYears = 3,
            NumberOfChildren = 2,
            HasSpouse = false,
            SalaryComponents = new List<PayrollSalaryComponentDto>
            {
                new() { ComponentType = "Prime de rendement", Amount = 1200m, Istaxable = true },
                new() { ComponentType = "Prime de fonction", Amount = 800m, Istaxable = true },
                new() { ComponentType = "Prime ancienneté", Amount = 500m, Istaxable = true },
                new() { ComponentType = "Commission", Amount = 300m, Istaxable = true },
                new() { ComponentType = "Prime d'astreinte", Amount = 200m, Istaxable = true }
            },
            PackageItems = new List<PayrollPackageItemDto>(),
            Absences = new List<PayrollAbsenceDto>(),
            Overtimes = new List<PayrollOvertimeDto>(),
            Leaves = new List<PayrollLeaveDto>()
        };

        var engine = CreateEngine();
        var result = engine.CalculatePayroll(data);

        Assert.True(result.Success, result.ErrorMessage ?? "Erreur calcul");

        // MODULE 01 — prime ancienneté = 8000 × 5% = 400
        AssertEqual(400.00m, result.PrimeAnciennete, "CP01_prime_anciennete");

        // MODULE 05 — total primes = 3000, brut = 8000 + 400 + 3000 = 11400
        AssertEqual(3000.00m, result.PrimesImposables, "CP05_total_primes_imposables");
        AssertEqual(11400.00m, result.BrutImposable, "CP05_salaire_brut_imposable");

        // MODULE 06 — CNSS
        AssertEqual(268.80m, result.CnssRgSalarial, "CP06_cnss_rg_salarial");
        AssertEqual(257.64m, result.AmoSalarial, "CP06_cnss_amo_salarial");
        AssertEqual(526.44m, result.TotalCotisationsSalariales, "CP06_total_cnss_salarial");

        // MODULE 08 — base_fp = 11400 > 6500 → 25% et plafond 2916.67 ; 11400×25%=2850 < 2916.67 → fp=2850. DSL example says 35% and 2500 for 11400 - that contradicts MODULE 08 (base_fp > 6500 → 25%). We follow the DSL rule: > 6500 → 25%.
        AssertEqual(2850.00m, result.FraisProfessionnels, "CP08_montant_fp_25pct");

        // MODULE 09 — RNI = 11400 − 526.44 − 0 − 0 − 2850 = 8023.56 (DSL example had 2500 fp → 8373.56; we use 25% so 8023.56)
        AssertEqual(8023.56m, result.RevenuNetImposable, "CP09_revenu_net_imposable");

        // MODULE 10 — 8023.56 in [8333.34-15000] would be 34% but 8023 < 8333.34 so 30%, deduction 1500; charges famille 2×30=60
        // ir_brut = 8023.56 × 0.30 = 2407.07 ; ir_final = 2407.07 − 1500 − 60 = 847.07
        AssertEqual(847.07m, result.IR, "CP10_ir_final");

        // MODULE 11 — retenues = 526.44 + 847.07 = 1373.51 ; net = 11400 − 1373.51 = 10026.49
        AssertEqual(10026.49m, result.SalaireNetAvantArrondi, "CP11_salaire_net");
    }

    private static void AssertEqual(decimal expected, decimal actual, string label)
    {
        Assert.True(Math.Abs(expected - actual) <= PayrollCalculationEngineDslTests.Tolerance,
            $"{label}: attendu {expected:N2}, obtenu {actual:N2} (écart {Math.Abs(expected - actual):N2})");
    }
}
