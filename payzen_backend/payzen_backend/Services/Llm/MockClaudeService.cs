using System.Text.Json;
using payzen_backend.Models.Employee.Dtos;

namespace payzen_backend.Services.Llm
{
    /// <summary>
    /// Service Mock de Claude pour les tests - Ne consomme PAS de tokens.
    /// Retourne un JSON simulé basé sur les données d'entrée.
    /// </summary>
    public class MockClaudeService : IClaudeService
    {
        private readonly ILogger<MockClaudeService> _logger;

        public MockClaudeService(ILogger<MockClaudeService> logger)
        {
            _logger = logger;
        }

        public Task<string> AnalyseSalarieAsync(
            string regleContent,
            EmployeePayrollDto payrollData,
            string instruction,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("🧪 MODE TEST : Utilisation du Mock Claude (pas de consommation de tokens)");
            _logger.LogInformation("Employé : {FullName}, Salaire de base : {BaseSalary}", 
                payrollData.FullName, payrollData.BaseSalary);
            
            _logger.LogInformation("📊 ═══════════════════════════════════════════════════════════════");
            _logger.LogInformation("📊 PRIMES ENVOYÉES AU LLM (depuis la DB)");
            _logger.LogInformation("📊 ⚠️  Mode : UNIQUEMENT SalaryComponents (PackageItems ignorés)");
            _logger.LogInformation("📊 ═══════════════════════════════════════════════════════════════");
            
            _logger.LogInformation($"📋 SalaryComponents : {payrollData.SalaryComponents?.Count ?? 0}");
            if (payrollData.SalaryComponents != null && payrollData.SalaryComponents.Any())
            {
                var imposables = payrollData.SalaryComponents.Where(c => c.Istaxable).ToList();
                var nonImposables = payrollData.SalaryComponents.Where(c => !c.Istaxable).ToList();
                
                _logger.LogInformation("");
                _logger.LogInformation($"✅ IMPOSABLES : {imposables.Count} items");
                foreach (var comp in imposables)
                {
                    _logger.LogInformation($"   • {comp.ComponentType,-30} : {comp.Amount,10:N2} MAD");
                    _logger.LogInformation($"     └─ Istaxable={comp.Istaxable}, IsSocial={comp.IsSocial}, IsCIMR={comp.IsCIMR}");
                }
                
                _logger.LogInformation("");
                _logger.LogInformation($"⚪ NON IMPOSABLES : {nonImposables.Count} items");
                foreach (var comp in nonImposables)
                {
                    _logger.LogInformation($"   • {comp.ComponentType,-30} : {comp.Amount,10:N2} MAD");
                    _logger.LogInformation($"     └─ Istaxable={comp.Istaxable}, IsSocial={comp.IsSocial}, IsCIMR={comp.IsCIMR}");
                }
            }
            _logger.LogInformation("📊 ═══════════════════════════════════════════════════════════════");

            // Simuler un calcul simple (CNSS, AMO, IR basiques)
            var salaireBrut = payrollData.BaseSalary;
            
            // Ajouter UNIQUEMENT les primes imposables depuis SalaryComponents (PackageItems ignorés)
            var salaryComponentsImposables = payrollData.SalaryComponents?.Where(c => c.Istaxable).ToList() ?? new();
            var totalPrimesImposables = salaryComponentsImposables.Sum(c => c.Amount);
            salaireBrut += totalPrimesImposables;
            
            _logger.LogInformation($"💰 Calcul du salaire brut :");
            _logger.LogInformation($"   - Salaire de base : {payrollData.BaseSalary:N2} MAD");
            _logger.LogInformation($"   - Primes imposables (SalaryComponents) : {totalPrimesImposables:N2} MAD");
            _logger.LogInformation($"   - ⚠️  PackageItems IGNORÉS");
            _logger.LogInformation($"   - TOTAL BRUT : {salaireBrut:N2} MAD");
            var cnss = Math.Min(salaireBrut, 6000) * 0.0448m;
            var amo = salaireBrut * 0.0226m;
            var cimr = payrollData.CimrEmployeeRate.HasValue 
                ? salaireBrut * (payrollData.CimrEmployeeRate.Value / 100)
                : 0;

            var totalCotisationsSalariales = cnss + amo + cimr;
            var salaireNetAvantIr = salaireBrut - totalCotisationsSalariales;

            // Calcul IR simplifié (approximatif)
            var fraisPro = Math.Min(salaireNetAvantIr * 0.20m, 2500);
            var chargesFamille = (payrollData.NumberOfChildren + (payrollData.HasSpouse ? 1 : 0)) * 30;
            var revenuImposable = (salaireNetAvantIr - fraisPro - cimr - chargesFamille) * 12;
            
            decimal ir = 0;
            if (revenuImposable > 180000) ir = (revenuImposable * 0.38m - 24400) / 12;
            else if (revenuImposable > 80000) ir = (revenuImposable * 0.34m - 17200) / 12;
            else if (revenuImposable > 60000) ir = (revenuImposable * 0.30m - 14000) / 12;
            else if (revenuImposable > 50000) ir = (revenuImposable * 0.20m - 8000) / 12;
            else if (revenuImposable > 30000) ir = (revenuImposable * 0.10m - 3000) / 12;

            var netAPayer = salaireNetAvantIr - ir;

            // Cotisations patronales
            var cnssPatronale = Math.Min(salaireBrut, 6000) * 0.1647m;
            var amoPatronale = salaireBrut * 0.0411m;
            var cimrPatronale = payrollData.CimrCompanyRate.HasValue
                ? salaireBrut * (payrollData.CimrCompanyRate.Value / 100)
                : 0;

            // Construire le JSON de réponse (format similaire à Claude)
            
            // Utiliser UNIQUEMENT les SalaryComponents (PackageItems ignorés)
            var primesImposablesList = new List<object>();
            var indemnitesList = new List<object>();
            
            var salaryComponentsNonImposables = payrollData.SalaryComponents?.Where(c => !c.Istaxable).ToList() ?? new();
            
            // Ajouter les SalaryComponents imposables
            foreach (var comp in salaryComponentsImposables)
            {
                primesImposablesList.Add(new { label = comp.ComponentType, montant = comp.Amount });
            }
            
            // Ajouter les SalaryComponents non imposables
            foreach (var comp in salaryComponentsNonImposables)
            {
                indemnitesList.Add(new { label = comp.ComponentType, montant = comp.Amount });
            }
            
            var result = new
            {
                _mock = true,
                _message = "⚠️ Données de TEST - Calculs simplifiés, ne pas utiliser en production. UNIQUEMENT SalaryComponents.",
                
                salaire_base = payrollData.BaseSalary,

                // Primes imposables (UNIQUEMENT SalaryComponents)
                primes_imposables = primesImposablesList,
                
                total_primes_imposables = totalPrimesImposables,
                
                // Indemnités non imposables (UNIQUEMENT SalaryComponents)
                indemnites_non_imposables = indemnitesList,

                heures_supplementaires = payrollData.Overtimes?.Sum(o => o.DurationInHours * o.RateMultiplier * (payrollData.BaseSalary / 191)) ?? 0,
                
                total_brut = salaireBrut,
                
                cotisations_salariales = new
                {
                    cnss = cnss,
                    amo = amo,
                    cimr = cimr,
                    total = cnss + amo + cimr
                },
                
                cotisations_patronales = new
                {
                    cnss = cnssPatronale,
                    amo = amoPatronale,
                    cimr = cimrPatronale,
                    total = cnssPatronale + amoPatronale + cimrPatronale
                },
                
                salaire_net_avant_ir = salaireNetAvantIr,
                
                deductions_fiscales = new
                {
                    frais_professionnels = fraisPro,
                    cimr_deductible = cimr,
                    charges_famille = chargesFamille,
                    total = fraisPro + cimr + chargesFamille
                },
                
                revenu_net_imposable_mensuel = (revenuImposable / 12),
                revenu_net_imposable_annuel = revenuImposable,
                
                ir = new
                {
                    ir_mensuel = Math.Max(ir, 0),
                    ir_annuel = Math.Max(ir * 12, 0)
                },
                
                net_a_payer = netAPayer,
                
                details_famille = new
                {
                    statut_marital = payrollData.MaritalStatus,
                    nombre_enfants = payrollData.NumberOfChildren,
                    nombre_personnes_charge = payrollData.NumberOfChildren + (payrollData.HasSpouse ? 1 : 0)
                },

                absences = payrollData.Absences?.Count ?? 0,
                overtimes = payrollData.Overtimes?.Count ?? 0,
                leaves = payrollData.Leaves?.Count ?? 0
            };

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            _logger.LogInformation("✅ Mock : Fiche de paie générée (Net à payer : {NetAPayer} MAD)", Math.Round(netAPayer, 2));

            return Task.FromResult(json);
        }
    }
}
