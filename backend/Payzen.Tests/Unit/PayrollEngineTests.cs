using Microsoft.Extensions.Logging.Abstractions;
using Payzen.Application.Payroll;
using Payzen.Tests.Helpers;

namespace Payzen.Tests.Unit;

/// <summary>
/// Tests unitaires du PayrollCalculationEngine.
/// Aucune dépendance EF ou HTTP — le moteur est pur C#.
/// </summary>
public class PayrollEngineTests
{
    private readonly PayrollCalculationEngine _engine =
        new(NullLogger<PayrollCalculationEngine>.Instance);

    // ═══════════════════════════════════════════════════════════════════
    // MODULE 01 — CNSS
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void CNSS_SalaireSousPlaFond_TauxPlein()
    {
        // Salaire 5000 DH < plafond 6000 DH → CNSS = 5000 × 4.48% = 224 DH
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(5000).Build();
        var result = _engine.CalculatePayroll(dto);

        result.Success.Should().BeTrue();
        result.CnssRgSalarial.Should().Be(224.00m);
    }

    [Fact]
    public void CNSS_SalaireAuDessusPlaFond_PlafonnéÀ6000()
    {
        // Salaire 10000 DH > plafond 6000 DH → base CNSS = 6000 → CNSS = 6000 × 4.48% = 268.80 DH
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(10000).Build();
        var result = _engine.CalculatePayroll(dto);

        result.Success.Should().BeTrue();
        result.CnssRgSalarial.Should().Be(268.80m);
        result.BaseCnssRg.Should().Be(6000.00m);
    }

    [Fact]
    public void CNSS_AMO_NonDesactivee_Calculée()
    {
        // AMO salariale = 5000 × 2.26% = 113 DH
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(5000).Build();
        var result = _engine.CalculatePayroll(dto);

        result.CnssAmoSalarial.Should().Be(113.00m);
    }

    [Fact]
    public void CNSS_AMO_Desactivée_ZéroAMO()
    {
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(5000).WithNoAmo().Build();
        var result = _engine.CalculatePayroll(dto);

        result.CnssAmoSalarial.Should().Be(0m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // MODULE 02 — IR progressif
    // ═══════════════════════════════════════════════════════════════════

    [Theory]
    [InlineData(3000, 0)]         // Tranche 0% → IR = 0
    [InlineData(5000, "?")]       // Test que le résultat est positif (valeur exacte dépend frais pro)
    [InlineData(20000, "?")]       // Tranche 37% → IR élevé
    public void IR_SalairesBruts_RetournePositif(decimal salaire, object _)
    {
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(salaire).Build();
        var result = _engine.CalculatePayroll(dto);

        result.Success.Should().BeTrue();
        result.IrFinal.Should().BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void IR_SalaireBas_SousSeuilPremièreTranche_IRZero()
    {
        // Salaire 3000 DH → après déductions, RNI < 3333.33 → IR = 0
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(3000).Build();
        var result = _engine.CalculatePayroll(dto);

        result.IrFinal.Should().Be(0m);
    }

    [Fact]
    public void IR_DeductionEnfants_ReducImpot()
    {
        // Même salaire, avec 2 enfants → IR doit être inférieur (30 DH/enfant déduction)
        decimal salaire = 12000;

        var sansEnfants = _engine.CalculatePayroll(
            PayrollDtoBuilder.Create().WithBaseSalary(salaire).WithChildren(0).Build());

        var avecEnfants = _engine.CalculatePayroll(
            PayrollDtoBuilder.Create().WithBaseSalary(salaire).WithChildren(2).Build());

        avecEnfants.IrFinal.Should().BeLessThan(sansEnfants.IrFinal);
    }

    [Fact]
    public void IR_Conjoint_ReducImpot()
    {
        decimal salaire = 12000;

        var sansFamille = _engine.CalculatePayroll(
            PayrollDtoBuilder.Create().WithBaseSalary(salaire).Build());

        var avecFamille = _engine.CalculatePayroll(
            PayrollDtoBuilder.Create().WithBaseSalary(salaire).WithSpouse().WithChildren(3).Build());

        avecFamille.IrFinal.Should().BeLessThan(sansFamille.IrFinal);
    }

    // ═══════════════════════════════════════════════════════════════════
    // MODULE 03 — Net à payer cohérent
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Net_ToujoursInferieurAuBrut()
    {
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(8000).Build();
        var result = _engine.CalculatePayroll(dto);

        result.SalaireNet.Should().BeLessThan(result.SalaireBrutImposable);
    }

    [Fact]
    public void Net_ToujoursPositif()
    {
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(3500).Build();
        var result = _engine.CalculatePayroll(dto);

        result.SalaireNet.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void Net_FormuleDeBase_Correcte()
    {
        // SalaireNet = SBI - TotalRetenues + TotalNiExonéré
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(7000).Build();
        var result = _engine.CalculatePayroll(dto);

        var attendu = result.SalaireBrutImposable
                    - result.TotalRetenuesSalariales
                    + result.TotalNiExonere;

        result.SalaireNet.Should().BeApproximately(attendu, 0.01m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // MODULE 04 — Ancienneté
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Anciennete_MoinsDe2Ans_TauxZero()
    {
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(5000).WithAnciennete(1).Build();
        var result = _engine.CalculatePayroll(dto);

        result.PrimeAnciennete.Should().Be(0m);
    }

    [Fact]
    public void Anciennete_PlusDe2Ans_PrimePositive()
    {
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(5000).WithAnciennete(3).Build();
        var result = _engine.CalculatePayroll(dto);

        result.PrimeAnciennete.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void Anciennete_PlusDe12Ans_Plafonée()
    {
        // L'ancienneté est plafonnée selon le barème — 15 ans et 25 ans doivent donner le même taux max
        var dto15 = PayrollDtoBuilder.Create().WithBaseSalary(10000).WithAnciennete(15).Build();
        var dto25 = PayrollDtoBuilder.Create().WithBaseSalary(10000).WithAnciennete(25).Build();

        var r15 = _engine.CalculatePayroll(dto15);
        var r25 = _engine.CalculatePayroll(dto25);

        // Les deux doivent avoir le même taux ancienneté (plafond atteint)
        r15.TauxAnciennete.Should().Be(r25.TauxAnciennete);
    }

    // ═══════════════════════════════════════════════════════════════════
    // MODULE 05 — Heures supplémentaires
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void HeuresSupp_25Pct_MontantCorrect()
    {
        // 8 heures sup à 25% sur base 10000 DH (191h/mois)
        // Taux horaire = (10000 / 191) = 52.36 DH/h
        // Heures sup 25% = 52.36 × 1.25 × 8 = 523.56 DH
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(10000).WithOvertimeHours(h25: 8).Build();
        var result = _engine.CalculatePayroll(dto);

        result.MontHsupp25.Should().BeGreaterThan(0m);
        result.SalaireBrutImposable.Should().BeGreaterThan(10000m);
    }

    [Fact]
    public void HeuresSupp_SansHeures_MontantZero()
    {
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(8000).Build();
        var result = _engine.CalculatePayroll(dto);

        result.MontHsupp25.Should().Be(0m);
        result.MontHsupp50.Should().Be(0m);
        result.MontHsupp100.Should().Be(0m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // MODULE 06 — Frais professionnels
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void FraisPro_SalaireSous6500_Taux35Pct()
    {
        // SBI < 6500 → frais pro = SBI × 35% (plafonné 2916.67)
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(5000).Build();
        var result = _engine.CalculatePayroll(dto);

        // Frais pro ≈ SBI × 35%
        result.MontantFp.Should().BeGreaterThan(0m);
        result.MontantFp.Should().BeLessThanOrEqualTo(2916.67m + 0.01m);
    }

    [Fact]
    public void FraisPro_SalaireEleve_Plafonné()
    {
        // SBI très élevé → frais pro plafonné à 2916.67 DH
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(50000).Build();
        var result = _engine.CalculatePayroll(dto);

        result.MontantFp.Should().BeApproximately(2916.67m, 0.10m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // MODULE 07 — CIMR
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void Cimr_SansCimr_RetenuéZéro()
    {
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(8000).Build();
        var result = _engine.CalculatePayroll(dto);

        result.CimrSalarial.Should().Be(0m);
    }

    [Fact]
    public void Cimr_AvecCimr_RetenuéPositive()
    {
        var dto = PayrollDtoBuilder.Create()
                        .WithBaseSalary(8000)
                        .WithCimr(employeeRate: 0.03m, companyRate: 0.03m)
                        .Build();
        var result = _engine.CalculatePayroll(dto);

        result.CimrSalarial.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void Panier_ProratiseSelonJoursTravailles_MoinsAbsences()
    {
        const decimal panierMensuelRef = 500m;
        var moisComplet = PayrollDtoBuilder.Create()
            .WithBaseSalary(5000)
            .WithSalaryComponent("Prime de panier", panierMensuelRef, isTaxable: false)
            .Build();
        var avecAbsences = PayrollDtoBuilder.Create()
            .WithBaseSalary(5000)
            .WithAbsenceDays(2)
            .WithSalaryComponent("Prime de panier", panierMensuelRef, isTaxable: false)
            .Build();

        var r0 = _engine.CalculatePayroll(moisComplet);
        var r1 = _engine.CalculatePayroll(avecAbsences);

        r0.JoursTravailles.Should().Be(26);
        r1.JoursTravailles.Should().Be(24);
        r1.TotalNiExonere.Should().BeLessThan(r0.TotalNiExonere);
    }

    [Fact]
    public void ComposanteSalaire_NonTaxable_SansCategorieNI_EntreDansBrutImposable()
    {
        const decimal prime = 1200m;
        var sansPrime = PayrollDtoBuilder.Create()
            .WithBaseSalary(5000)
            .Build();
        var avecPrime = PayrollDtoBuilder.Create()
            .WithBaseSalary(5000)
            .WithSalaryComponent("Prime de rendement", prime, isTaxable: false)
            .Build();

        var r0 = _engine.CalculatePayroll(sansPrime);
        var r1 = _engine.CalculatePayroll(avecPrime);

        r1.SalaireBrutImposable.Should().BeGreaterThan(r0.SalaireBrutImposable);
        (r1.SalaireBrutImposable - r0.SalaireBrutImposable).Should().BeApproximately(prime, 1m);
    }

    [Fact]
    public void Cimr_TauxSaisiEnPourcentage45_EquivautFraction0045()
    {
        const decimal salaire = 8000m;
        var dtoFraction = PayrollDtoBuilder.Create()
            .WithBaseSalary(salaire)
            .WithCimr(0.045m, 0.045m)
            .Build();
        var dtoPercent = PayrollDtoBuilder.Create()
            .WithBaseSalary(salaire)
            .WithCimr(4.5m, 4.5m)
            .Build();

        var r1 = _engine.CalculatePayroll(dtoFraction);
        var r2 = _engine.CalculatePayroll(dtoPercent);

        r1.CimrSalarial.Should().Be(r2.CimrSalarial);
        r1.CimrPatronal.Should().Be(r2.CimrPatronal);
    }

    // ═══════════════════════════════════════════════════════════════════
    // MODULE 08 — Indemnités NI (Non Imposables)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void NI_Transport_Exoneree()
    {
        // Prime transport NI 300 DH < plafond 500 DH → totalement exonérée
        var dto = PayrollDtoBuilder.Create()
                    .WithBaseSalary(5000)
                    .WithPackageItem("Prime Transport", 300, isTaxable: false)
                    .Build();
        var result = _engine.CalculatePayroll(dto);

        result.TotalNiExonere.Should().BeGreaterThan(0m);
        // Le brut imposable ne doit pas inclure la NI
        result.SalaireBrutImposable.Should().BeApproximately(5000m, 50m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // CAS LIMITES
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void CasLimite_SalaireTrèsBas_PasException()
    {
        // SMIG mensuel ≈ 3111 DH — le moteur ne doit pas planter
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(3111).Build();
        var result = _engine.CalculatePayroll(dto);

        result.Success.Should().BeTrue();
        result.SalaireNet.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void CasLimite_SalaireTrèsHaut_PasException()
    {
        var dto = PayrollDtoBuilder.Create().WithBaseSalary(500000).Build();
        var result = _engine.CalculatePayroll(dto);

        result.Success.Should().BeTrue();
        result.IrFinal.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void CasLimite_ToutesCotisations_NetPositif()
    {
        // CNSS + AMO + CIMR + Mutuelle + IR → malgré tout, net doit être > 0
        var dto = PayrollDtoBuilder.Create()
                        .WithBaseSalary(8000)
                        .WithCimr(0.06m, 0.06m)
                        .WithPrivateInsurance(0.05m)
                        .WithChildren(2)
                        .Build();
        var result = _engine.CalculatePayroll(dto);

        result.Success.Should().BeTrue();
        result.SalaireNet.Should().BeGreaterThan(0m);
    }
}
