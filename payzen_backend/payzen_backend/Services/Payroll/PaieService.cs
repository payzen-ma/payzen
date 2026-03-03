using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Employee.Dtos;
using payzen_backend.Models.Payroll;
using payzen_backend.Services.Llm;

namespace payzen_backend.Services.Payroll
{
    public class PaieService
    {
        private readonly AppDbContext _db;
        private readonly IClaudeService _claudeService;
        private readonly EmployeePayrollDataService _dataService;
        private readonly PayrollCalculationEngine _nativeEngine;
        private readonly ILogger<PaieService> _logger;
        
        // Détection automatique du type de LLM utilisé
        private string LlmModelName => _claudeService switch
        {
            MockClaudeService => "mock-llm",
            GeminiService => "gemini-2.5-flash",
            ClaudeService => "claude-sonnet-4-5-20250929",
            _ => "unknown-llm"
        };

        public PaieService(
            AppDbContext db,
            IClaudeService claudeService,
            EmployeePayrollDataService dataService,
            PayrollCalculationEngine nativeEngine,
            ILogger<PaieService> logger)
        {
            _db = db;
            _claudeService = claudeService;
            _dataService = dataService;
            _nativeEngine = nativeEngine;
            _logger = logger;
        }

        public async Task TraiterTousLesSalariesAsync(int companyId, int month, int year, bool useNativeEngine = true)
        {
            if (useNativeEngine)
            {
                Console.WriteLine($"🚀 Début du calcul de paie NATIF pour l'entreprise {companyId} - {month}/{year}"); 
                await TraiterAvecMoteurNatifAsync(companyId, month, year);
            }
            else
            {
                Console.WriteLine($"🚀 Début du calcul de paie LLM pour l'entreprise {companyId} - {month} / {year}"); 
                await TraiterAvecLlmAsync(companyId, month, year);
            }
        }

        private async Task TraiterAvecMoteurNatifAsync(int companyId, int month, int year)
        {
            var employeeIds = await _db.Employees
                .Where(e => e.Status.AffectsPayroll == false && e.CompanyId == companyId)
                .Select(e => e.Id)
                .ToListAsync();
    
            _logger.LogInformation("🚀 Début du calcul de paie NATIF pour {Count} employés de l'entreprise {CompanyId} - {Month}/{Year}", 
                employeeIds.Count, companyId, month, year);
            _logger.LogInformation("⚡ Mode : Moteur C# natif (SANS LLM) - Calcul instantané !");

            int successCount = 0;
            int errorCount = 0;
            var startTime = DateTime.UtcNow;

            foreach (var employeeId in employeeIds)
            {
                try
                {
                    _logger.LogInformation("📊 Traitement de l'employé {EmployeeId}...", employeeId);
                    
                    // Assembler toutes les données
                    var payrollData = await _dataService.BuildPayrollDataAsync(employeeId, month, year);

                    // 🚀 Calcul NATIF (pas de LLM, pas de tokens, instantané!)
                    var result = _nativeEngine.CalculatePayroll(payrollData);

                    if (!result.Success)
                    {
                        throw new Exception(result.ErrorMessage ?? "Erreur de calcul");
                    }

                    // Mapper vers PayrollResult
                    var payrollResult = MapNativeResultToPayrollResult(result, employeeId, companyId, month, year);
                    
                    // Vérifier si un résultat existe déjà
                    var existingResult = await _db.PayrollResults
                        .FirstOrDefaultAsync(pr => pr.EmployeeId == employeeId 
                            && pr.Month == month 
                            && pr.Year == year 
                            && pr.DeletedAt == null);
                    
                    if (existingResult != null)
                    {
                        _logger.LogWarning("   ⚠️  Un résultat existe déjà. Suppression (soft delete)...");
                        existingResult.DeletedAt = DateTimeOffset.UtcNow;
                        existingResult.DeletedBy = 0;
                    }
                    
                    _db.PayrollResults.Add(payrollResult);
                    await _db.SaveChangesAsync();

                    // Enregistrer l'audit trail du calcul (un enregistrement par module)
                    if (result.AuditSteps != null && result.AuditSteps.Count > 0)
                    {
                        foreach (var step in result.AuditSteps)
                        {
                            _db.PayrollCalculationAuditSteps.Add(new PayrollCalculationAuditStep
                            {
                                PayrollResultId = payrollResult.Id,
                                StepOrder = step.StepOrder,
                                ModuleName = step.ModuleName,
                                FormulaDescription = step.FormulaDescription,
                                InputsJson = step.InputsJson,
                                OutputsJson = step.OutputsJson,
                                CreatedAt = DateTimeOffset.UtcNow
                            });
                        }
                        await _db.SaveChangesAsync();
                    }

                    successCount++;
                    _logger.LogInformation("✅ Employé {EmployeeId} traité avec succès (Net: {NetAPayer} MAD)", 
                        employeeId, result.SalaireNet);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "❌ Erreur pour employé {EmployeeId}, {Month}/{Year}", employeeId, month, year);
                    
                    _db.PayrollResults.Add(new PayrollResult
                    {
                        EmployeeId = employeeId,
                        CompanyId = companyId,
                        Month = month,
                        Year = year,
                        Status = PayrollResultStatus.Error,
                        ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message,
                        ProcessedAt = DateTime.UtcNow,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = 0
                    });
                    
                    await _db.SaveChangesAsync();
                }
            }
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("🎉 Calcul de paie NATIF terminé - Succès: {SuccessCount}, Erreurs: {ErrorCount}, Total: {Total}, Durée: {Duration}", 
                successCount, errorCount, employeeIds.Count, duration.ToString(@"mm\:ss"));
        }

        private async Task TraiterAvecLlmAsync(int companyId, int month, int year)
        {
            var regleContent = await File.ReadAllTextAsync("rules/regles_paie_compact.txt");

            var employeeIds = await _db.Employees
                .Where(e => e.Status.AffectsPayroll == false && e.CompanyId == companyId)
                .Select(e => e.Id)
                .ToListAsync();
    
            _logger.LogInformation("🚀 Début du calcul de paie SÉQUENTIEL pour {Count} employés de l'entreprise {CompanyId} - {Month}/{Year}", 
                employeeIds.Count, companyId, month, year);
            _logger.LogInformation("⚡ Mode : Un employé à la fois pour optimiser le cache Gemini");

            int successCount = 0;
            int errorCount = 0;
            var startTime = DateTime.UtcNow;

            for (int i = 0; i < employeeIds.Count; i++)
            {
                var employeeId = employeeIds[i];
                
                try
                {
                    _logger.LogInformation("📊 [{Current}/{Total}] Traitement de l'employé {EmployeeId}...", 
                        i + 1, employeeIds.Count, employeeId);
                    
                    // Plus besoin de récupérer companyId, on l'a déjà en paramètre

                    // Assembler toutes les données
                    var payrollData = await _dataService.BuildPayrollDataAsync(employeeId, month, year);

                    // ⭐ Envoyer AU LLM - UN SEUL EMPLOYÉ À LA FOIS
                    _logger.LogInformation("   🤖 Envoi à Gemini (employé {EmployeeId})...", employeeId);
                    var jsonBrut = await _claudeService.AnalyseSalarieAsync(
                        regleContent,
                        payrollData,
                        "Calcule la fiche de paie complète selon les règles marocaines 2025. Retourne un JSON."
                    );
                    _logger.LogInformation("   ✅ Réponse reçue de Gemini ({Length} caractères)", jsonBrut.Length);

                // Parser le JSON pour extraire les montants
                JsonElement resultatParse;
                try
                {
                    using var doc = JsonDocument.Parse(jsonBrut);
                    resultatParse = doc.RootElement.Clone();
                    
                    // 🔍 LOG pour debug : afficher la structure du JSON retourné
                    _logger.LogInformation("   📄 Structure JSON reçue de Gemini :");
                    if (resultatParse.TryGetProperty("primes_imposables", out var primesArray))
                    {
                        _logger.LogInformation("      - primes_imposables: array de {Count} éléments", primesArray.GetArrayLength());
                    }
                    else
                    {
                        _logger.LogWarning("      ⚠️  'primes_imposables' NON TROUVÉ dans le JSON");
                    }
                    
                    if (resultatParse.TryGetProperty("indemnites_non_imposables", out var indemnitesArray))
                    {
                        _logger.LogInformation("      - indemnites_non_imposables: array de {Count} éléments", indemnitesArray.GetArrayLength());
                    }
                    else
                    {
                        _logger.LogWarning("      ⚠️  'indemnites_non_imposables' NON TROUVÉ dans le JSON");
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Paie: JSON invalide retourné par le LLM pour employé {EmployeeId}. Réponse brute: {Response}", 
                        employeeId, jsonBrut.Length > 500 ? jsonBrut.Substring(0, 500) + "..." : jsonBrut);
                    throw new InvalidOperationException($"Le LLM a retourné un JSON invalide: {jsonEx.Message}. Début de la réponse: {(jsonBrut.Length > 100 ? jsonBrut.Substring(0, 100) : jsonBrut)}", jsonEx);
                }

                static decimal? GetDecimal(JsonElement root, string name)
                {
                    if (!root.TryGetProperty(name, out var prop)) return null;
                    try { return prop.GetDecimal(); }
                    catch { return null; }
                }
                
                // Helper pour extraire les primes imposables depuis un array JSON
                static decimal? GetPrimeFromArray(JsonElement root, string arrayName, int index)
                {
                    if (!root.TryGetProperty(arrayName, out var arr) || arr.ValueKind != JsonValueKind.Array)
                        return null;
                    
                    if (index >= arr.GetArrayLength()) return null;
                    
                    var item = arr[index];
                    if (item.TryGetProperty("montant", out var montant))
                    {
                        try { return montant.GetDecimal(); }
                        catch { return null; }
                    }
                    return null;
                }
                
                // 🆕 Helper pour extraire TOUTES les primes depuis un array JSON
                static List<PayrollResultPrime> ExtractAllPrimes(JsonElement root, string arrayName, bool isTaxable)
                {
                    var primes = new List<PayrollResultPrime>();
                    
                    if (!root.TryGetProperty(arrayName, out var arr) || arr.ValueKind != JsonValueKind.Array)
                        return primes;
                    
                    int ordre = 1;
                    foreach (var item in arr.EnumerateArray())
                    {
                        string? label = null;
                        decimal montant = 0;
                        
                        if (item.TryGetProperty("label", out var labelProp))
                            label = labelProp.GetString();
                        
                        if (item.TryGetProperty("montant", out var montantProp))
                        {
                            try { montant = montantProp.GetDecimal(); }
                            catch { continue; }
                        }
                        
                        if (!string.IsNullOrEmpty(label) && montant > 0)
                        {
                            primes.Add(new PayrollResultPrime
                            {
                                Label = label,
                                Montant = montant,
                                Ordre = ordre++,
                                IsTaxable = isTaxable
                            });
                        }
                    }
                    
                    return primes;
                }

                // 📊 Stocker le résultat avec extraction COMPLETE de tous les champs de la fiche de paie
                var payrollResult = new PayrollResult
                {
                    EmployeeId = employeeId,
                    CompanyId = companyId,
                    Month = month,
                    Year = year,
                    Status = PayrollResultStatus.OK,
                    ResultatJson = jsonBrut,
                    ClaudeModel = LlmModelName,
                    ProcessedAt = DateTime.UtcNow,
                    
                    // 📊 SALAIRE DE BASE ET HEURES
                    SalaireBase = GetDecimal(resultatParse, "salaire_base_mensuel"),
                    HeuresSupp25 = GetDecimal(resultatParse, "hs_25_montant"),
                    HeuresSupp50 = GetDecimal(resultatParse, "hs_50_montant"),
                    HeuresSupp100 = GetDecimal(resultatParse, "hs_100_montant"),
                    Conges = GetDecimal(resultatParse, "conges_montant"),
                    JoursFeries = GetDecimal(resultatParse, "jours_feries_montant"),
                    PrimeAnciennete = GetDecimal(resultatParse, "prime_anciennete"),
                    
                    // 💰 PRIMES IMPOSABLES (extraction depuis l'array dynamique)
                    PrimeImposable1 = GetPrimeFromArray(resultatParse, "primes_imposables", 0),
                    PrimeImposable2 = GetPrimeFromArray(resultatParse, "primes_imposables", 1),
                    PrimeImposable3 = GetPrimeFromArray(resultatParse, "primes_imposables", 2),
                    TotalPrimesImposables = GetDecimal(resultatParse, "total_primes_imposables"),
                    
                    // 📈 SALAIRE BRUT
                    TotalBrut = GetDecimal(resultatParse, "salaire_brut_imposable"),
                    BrutImposable = GetDecimal(resultatParse, "salaire_brut_imposable"),
                    
                    // 🏢 FRAIS PROFESSIONNELS
                    FraisProfessionnels = GetDecimal(resultatParse, "montant_fp"),
                    
                    // 🎁 INDEMNITES NON IMPOSABLES (depuis l'array indemnites_non_imposables)
                    IndemniteRepresentation = GetDecimal(resultatParse, "indemnite_representation"),
                    PrimeTransport = GetDecimal(resultatParse, "prime_transport"),
                    PrimePanier = GetDecimal(resultatParse, "prime_panier"),
                    IndemniteDeplacement = GetDecimal(resultatParse, "indemnite_deplacement"),
                    IndemniteCaisse = GetDecimal(resultatParse, "indemnite_caisse"),
                    PrimeSalissure = GetDecimal(resultatParse, "prime_salissure"),
                    GratificationsFamilial = GetDecimal(resultatParse, "gratifications_familial"),
                    PrimeVoyageMecque = GetDecimal(resultatParse, "prime_voyage_mecque"),
                    IndemniteLicenciement = GetDecimal(resultatParse, "indemnite_licenciement"),
                    IndemniteKilometrique = GetDecimal(resultatParse, "indemnite_kilometrique"),
                    PrimeTourne = GetDecimal(resultatParse, "prime_tourne"),
                    PrimeOutillage = GetDecimal(resultatParse, "prime_outillage"),
                    AideMedicale = GetDecimal(resultatParse, "aide_medicale"),
                    AutresPrimesNonImposable = GetDecimal(resultatParse, "autres_primes_non_imposable"),
                    TotalIndemnites = GetDecimal(resultatParse, "total_ni_exonere"),
                    
                    // 🔴 COTISATIONS SALARIALES
                    CnssPartSalariale = GetDecimal(resultatParse, "cnss_rg_salarial"),
                    CimrPartSalariale = GetDecimal(resultatParse, "cimr_salarial"),
                    AmoPartSalariale = GetDecimal(resultatParse, "cnss_amo_salarial"),
                    MutuellePartSalariale = GetDecimal(resultatParse, "mutuelle_salariale"),
                    TotalCotisationsSalariales = GetDecimal(resultatParse, "total_cnss_salarial"),
                    
                    // 🔵 COTISATIONS PATRONALES
                    CnssPartPatronale = GetDecimal(resultatParse, "cnss_rg_patronal"),
                    CimrPartPatronale = GetDecimal(resultatParse, "cimr_patronal"),
                    AmoPartPatronale = GetDecimal(resultatParse, "cnss_amo_patronal"),
                    MutuellePartPatronale = GetDecimal(resultatParse, "mutuelle_patronale"),
                    TotalCotisationsPatronales = GetDecimal(resultatParse, "total_charges_patronales"),
                    
                    // 💸 IMPOT SUR LE REVENU
                    ImpotRevenu = GetDecimal(resultatParse, "ir_final"),
                    
                    // 🔄 ARRONDI
                    Arrondi = GetDecimal(resultatParse, "arrondi_net"),
                    
                    // 💳 AVANCES ET DIVERS
                    AvanceSurSalaire = GetDecimal(resultatParse, "avance_salaire"),
                    InteretSurLogement = GetDecimal(resultatParse, "interet_logement"),
                    
                    // 📊 TOTAUX FINAUX
                    NetImposable = GetDecimal(resultatParse, "revenu_net_imposable"),
                    TotalGains = GetDecimal(resultatParse, "salaire_brut_imposable"),
                    TotalRetenues = GetDecimal(resultatParse, "total_retenues_salariales"),
                    NetAPayer = GetDecimal(resultatParse, "salaire_net"),
                    
                    // 🎯 ANCIENS CHAMPS (compatibilité)
                    TotalNet = GetDecimal(resultatParse, "salaire_net_avant_arrondi") 
                        ?? GetDecimal(resultatParse, "salaire_net"),
                    TotalNet2 = GetDecimal(resultatParse, "salaire_net"),
                    
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = 0
                };
                
                // 🆕 Extraire et ajouter TOUTES les primes dynamiquement
                var primesImposables = ExtractAllPrimes(resultatParse, "primes_imposables", isTaxable: true);
                var indemnites = ExtractAllPrimes(resultatParse, "indemnites_non_imposables", isTaxable: false);
                
                foreach (var prime in primesImposables.Concat(indemnites))
                {
                    payrollResult.Primes.Add(prime);
                }
                
                _logger.LogInformation("💰 Extraction dynamique : {PrimesCount} primes imposables + {IndemnitesCount} indemnités = {TotalCount} total", 
                    primesImposables.Count, indemnites.Count, payrollResult.Primes.Count);
                
                // ⭐ Vérifier si un résultat existe déjà pour cet employé/période
                var existingResult = await _db.PayrollResults
                    .FirstOrDefaultAsync(pr => pr.EmployeeId == employeeId 
                        && pr.Month == month 
                        && pr.Year == year 
                        && pr.DeletedAt == null);
                
                if (existingResult != null)
                {
                    _logger.LogWarning("   ⚠️  Un résultat existe déjà (ID: {ExistingId}). Suppression (soft delete)...", existingResult.Id);
                    existingResult.DeletedAt = DateTimeOffset.UtcNow;
                    existingResult.DeletedBy = 0;
                }
                
                _db.PayrollResults.Add(payrollResult);
                
                // ⭐ SAUVEGARDER APRÈS CHAQUE EMPLOYÉ (pas à la fin du batch)
                // Cela permet de ne pas perdre les données en cas d'erreur
                await _db.SaveChangesAsync();
                
                successCount++;
                _logger.LogInformation("✅ [{Current}/{Total}] Employé {EmployeeId} traité et sauvegardé avec succès (Net: {NetAPayer} MAD)", 
                    i + 1, employeeIds.Count, employeeId, payrollResult.NetAPayer ?? 0);
                
                // ⏱️ Délai entre les appels pour respecter les rate limits de Gemini (15 req/min)
                // 4 secondes = max 15 requêtes par minute
                if (i < employeeIds.Count - 1) // Pas de délai après le dernier
                {
                    _logger.LogInformation("   ⏱️  Attente de 4 secondes avant le prochain employé (rate limit)...");
                    await Task.Delay(4000);
                }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "❌ [{Current}/{Total}] Erreur pour employé {EmployeeId}, {Month}/{Year}", 
                        i + 1, employeeIds.Count, employeeId, month, year);
                    
                    _db.PayrollResults.Add(new PayrollResult
                    {
                        EmployeeId = employeeId,
                        CompanyId = companyId,
                        Month = month,
                        Year = year,
                        Status = PayrollResultStatus.Error,
                        ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message,
                        ProcessedAt = DateTime.UtcNow,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = 0
                    });
                    
                    // Sauvegarder l'erreur aussi
                    await _db.SaveChangesAsync();
                    
                    // Continuer avec le prochain employé malgré l'erreur
                    _logger.LogInformation("   ⏭️  Passage au prochain employé...");
                }
            }
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("🎉 Calcul de paie terminé - Succès: {SuccessCount}, Erreurs: {ErrorCount}, Total: {Total}, Durée: {Duration}", 
                successCount, errorCount, employeeIds.Count, duration.ToString(@"mm\:ss"));
        }

        /// <summary>
        /// Calcule la paie pour un seul employé.
        /// Si useNativeEngine est true : moteur natif .NET (recommandé).
        /// Sinon : LLM (Gemini/Claude).
        /// </summary>
        public async Task<PayrollResult> TraiterUnSeulEmployeAsync(int employeeId, int month, int year, bool useNativeEngine = true)
        {
            int? companyId = null;
            try
            {
                companyId = await _db.Employees
                    .Where(e => e.Id == employeeId)
                    .Select(e => e.CompanyId)
                    .FirstOrDefaultAsync();
                if (companyId == 0) companyId = null;

                var cid = companyId ?? 0;

                if (useNativeEngine)
                {
                    return await TraiterUnSeulEmployeAvecMoteurNatifAsync(employeeId, cid, month, year);
                }
                else
                {
                    return await TraiterUnSeulEmployeAvecLlmAsync(employeeId, cid, month, year);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paie: erreur pour employé {EmployeeId}, {Month}/{Year}", employeeId, month, year);
                var cid = companyId ?? await _db.Employees.Where(e => e.Id == employeeId).Select(e => e.CompanyId).FirstOrDefaultAsync();
                
                var errorResult = new PayrollResult
                {
                    EmployeeId = employeeId,
                    CompanyId = cid,
                    Month = month,
                    Year = year,
                    Status = PayrollResultStatus.Error,
                    ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message,
                    ProcessedAt = DateTime.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = 0
                };

                _db.PayrollResults.Add(errorResult);
                await _db.SaveChangesAsync();

                return errorResult;
            }
        }

        private async Task<PayrollResult> TraiterUnSeulEmployeAvecMoteurNatifAsync(int employeeId, int companyId, int month, int year)
        {
            var payrollData = await _dataService.BuildPayrollDataAsync(employeeId, month, year);
            var nativeResult = _nativeEngine.CalculatePayroll(payrollData);

            if (!nativeResult.Success)
                throw new InvalidOperationException(nativeResult.ErrorMessage ?? "Erreur de calcul");

            var payrollResult = MapNativeResultToPayrollResult(nativeResult, employeeId, companyId, month, year);

            var existingResult = await _db.PayrollResults
                .FirstOrDefaultAsync(pr => pr.EmployeeId == employeeId && pr.Month == month && pr.Year == year && pr.DeletedAt == null);
            if (existingResult != null)
            {
                existingResult.DeletedAt = DateTimeOffset.UtcNow;
                existingResult.DeletedBy = 0;
            }

            _db.PayrollResults.Add(payrollResult);
            await _db.SaveChangesAsync();

            if (nativeResult.AuditSteps != null && nativeResult.AuditSteps.Count > 0)
            {
                foreach (var step in nativeResult.AuditSteps)
                {
                    _db.PayrollCalculationAuditSteps.Add(new PayrollCalculationAuditStep
                    {
                        PayrollResultId = payrollResult.Id,
                        StepOrder = step.StepOrder,
                        ModuleName = step.ModuleName,
                        FormulaDescription = step.FormulaDescription,
                        InputsJson = step.InputsJson,
                        OutputsJson = step.OutputsJson,
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
                await _db.SaveChangesAsync();
            }

            _logger.LogInformation("✅ Paie employé {EmployeeId} calculée (moteur natif) — Net: {Net} MAD", employeeId, nativeResult.SalaireNet);
            return payrollResult;
        }

        private async Task<PayrollResult> TraiterUnSeulEmployeAvecLlmAsync(int employeeId, int companyId, int month, int year)
        {
            var regleContent = await File.ReadAllTextAsync("rules/regles_paie_compact.txt");
            var payrollData = await _dataService.BuildPayrollDataAsync(employeeId, month, year);

            var jsonBrut = await _claudeService.AnalyseSalarieAsync(
                regleContent,
                payrollData,
                "Calcule la fiche de paie complète selon les règles marocaines 2025. Retourne un JSON."
            );

            JsonElement resultatParse;
            try
            {
                using var doc = JsonDocument.Parse(jsonBrut);
                resultatParse = doc.RootElement.Clone();
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Paie: JSON invalide retourné par le LLM pour employé {EmployeeId}", employeeId);
                throw new InvalidOperationException($"Le LLM a retourné un JSON invalide: {jsonEx.Message}", jsonEx);
            }

            static decimal? GetDecimal(JsonElement root, string name)
            {
                if (!root.TryGetProperty(name, out var prop)) return null;
                try { return prop.GetDecimal(); } catch { return null; }
            }
            static decimal? GetPrimeFromArray(JsonElement root, string arrayName, int index)
            {
                if (!root.TryGetProperty(arrayName, out var arr) || arr.ValueKind != JsonValueKind.Array) return null;
                if (index >= arr.GetArrayLength()) return null;
                var item = arr[index];
                if (item.TryGetProperty("montant", out var montant)) { try { return montant.GetDecimal(); } catch { return null; } }
                return null;
            }
            static List<PayrollResultPrime> ExtractAllPrimes(JsonElement root, string arrayName, bool isTaxable)
            {
                var primes = new List<PayrollResultPrime>();
                if (!root.TryGetProperty(arrayName, out var arr) || arr.ValueKind != JsonValueKind.Array) return primes;
                int ordre = 1;
                foreach (var item in arr.EnumerateArray())
                {
                    string? label = null;
                    decimal montant = 0;
                    if (item.TryGetProperty("label", out var labelProp)) label = labelProp.GetString();
                    if (item.TryGetProperty("montant", out var montantProp)) { try { montant = montantProp.GetDecimal(); } catch { continue; } }
                    if (!string.IsNullOrEmpty(label) && montant > 0)
                        primes.Add(new PayrollResultPrime { Label = label, Montant = montant, Ordre = ordre++, IsTaxable = isTaxable });
                }
                return primes;
            }

            var payrollResult = new PayrollResult
            {
                EmployeeId = employeeId,
                CompanyId = companyId,
                Month = month,
                Year = year,
                Status = PayrollResultStatus.OK,
                ResultatJson = jsonBrut,
                ClaudeModel = LlmModelName,
                ProcessedAt = DateTime.UtcNow,
                SalaireBase = GetDecimal(resultatParse, "salaire_base_mensuel"),
                HeuresSupp25 = GetDecimal(resultatParse, "hs_25_montant"),
                HeuresSupp50 = GetDecimal(resultatParse, "hs_50_montant"),
                HeuresSupp100 = GetDecimal(resultatParse, "hs_100_montant"),
                Conges = GetDecimal(resultatParse, "conges_montant"),
                JoursFeries = GetDecimal(resultatParse, "jours_feries_montant"),
                PrimeAnciennete = GetDecimal(resultatParse, "prime_anciennete"),
                PrimeImposable1 = GetPrimeFromArray(resultatParse, "primes_imposables", 0),
                PrimeImposable2 = GetPrimeFromArray(resultatParse, "primes_imposables", 1),
                PrimeImposable3 = GetPrimeFromArray(resultatParse, "primes_imposables", 2),
                TotalPrimesImposables = GetDecimal(resultatParse, "total_primes_imposables"),
                TotalBrut = GetDecimal(resultatParse, "salaire_brut_imposable"),
                BrutImposable = GetDecimal(resultatParse, "salaire_brut_imposable"),
                FraisProfessionnels = GetDecimal(resultatParse, "montant_fp"),
                IndemniteRepresentation = GetDecimal(resultatParse, "indemnite_representation"),
                PrimeTransport = GetDecimal(resultatParse, "prime_transport"),
                PrimePanier = GetDecimal(resultatParse, "prime_panier"),
                IndemniteDeplacement = GetDecimal(resultatParse, "indemnite_deplacement"),
                IndemniteCaisse = GetDecimal(resultatParse, "indemnite_caisse"),
                PrimeSalissure = GetDecimal(resultatParse, "prime_salissure"),
                GratificationsFamilial = GetDecimal(resultatParse, "gratifications_familial"),
                PrimeVoyageMecque = GetDecimal(resultatParse, "prime_voyage_mecque"),
                IndemniteLicenciement = GetDecimal(resultatParse, "indemnite_licenciement"),
                IndemniteKilometrique = GetDecimal(resultatParse, "indemnite_kilometrique"),
                PrimeTourne = GetDecimal(resultatParse, "prime_tourne"),
                PrimeOutillage = GetDecimal(resultatParse, "prime_outillage"),
                AideMedicale = GetDecimal(resultatParse, "aide_medicale"),
                AutresPrimesNonImposable = GetDecimal(resultatParse, "autres_primes_non_imposable"),
                TotalIndemnites = GetDecimal(resultatParse, "total_ni_exonere"),
                CnssPartSalariale = GetDecimal(resultatParse, "cnss_rg_salarial"),
                CimrPartSalariale = GetDecimal(resultatParse, "cimr_salarial"),
                AmoPartSalariale = GetDecimal(resultatParse, "cnss_amo_salarial"),
                MutuellePartSalariale = GetDecimal(resultatParse, "mutuelle_salariale"),
                TotalCotisationsSalariales = GetDecimal(resultatParse, "total_cnss_salarial"),
                CnssPartPatronale = GetDecimal(resultatParse, "cnss_rg_patronal"),
                CimrPartPatronale = GetDecimal(resultatParse, "cimr_patronal"),
                AmoPartPatronale = GetDecimal(resultatParse, "cnss_amo_patronal"),
                MutuellePartPatronale = GetDecimal(resultatParse, "mutuelle_patronale"),
                TotalCotisationsPatronales = GetDecimal(resultatParse, "total_charges_patronales"),
                ImpotRevenu = GetDecimal(resultatParse, "ir_final"),
                Arrondi = GetDecimal(resultatParse, "arrondi_net"),
                AvanceSurSalaire = GetDecimal(resultatParse, "avance_salaire"),
                InteretSurLogement = GetDecimal(resultatParse, "interet_logement"),
                NetImposable = GetDecimal(resultatParse, "revenu_net_imposable"),
                TotalGains = GetDecimal(resultatParse, "salaire_brut_imposable"),
                TotalRetenues = GetDecimal(resultatParse, "total_retenues_salariales"),
                NetAPayer = GetDecimal(resultatParse, "salaire_net"),
                TotalNet = GetDecimal(resultatParse, "salaire_net_avant_arrondi") ?? GetDecimal(resultatParse, "salaire_net"),
                TotalNet2 = GetDecimal(resultatParse, "salaire_net"),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = 0
            };

            foreach (var prime in ExtractAllPrimes(resultatParse, "primes_imposables", true).Concat(ExtractAllPrimes(resultatParse, "indemnites_non_imposables", false)))
                payrollResult.Primes.Add(prime);

            var existingResult = await _db.PayrollResults
                .FirstOrDefaultAsync(pr => pr.EmployeeId == employeeId && pr.Month == month && pr.Year == year && pr.DeletedAt == null);
            if (existingResult != null)
            {
                existingResult.DeletedAt = DateTimeOffset.UtcNow;
                existingResult.DeletedBy = 0;
            }
            _db.PayrollResults.Add(payrollResult);
            await _db.SaveChangesAsync();
            _logger.LogInformation("✅ Paie employé {EmployeeId} calculée (LLM) — Net: {Net} MAD", employeeId, payrollResult.NetAPayer);
            return payrollResult;
        }

        /// <summary>
        /// Mappe le résultat du moteur natif vers PayrollResult
        /// </summary>
        private PayrollResult MapNativeResultToPayrollResult(
            PayrollCalculationResult nativeResult,
            int employeeId,
            int companyId,
            int month,
            int year)
        {
            var payrollResult = new PayrollResult
            {
                EmployeeId = employeeId,
                CompanyId = companyId,
                Month = month,
                Year = year,
                Status = PayrollResultStatus.OK,
                ClaudeModel = "native-engine-csharp",
                ProcessedAt = DateTime.UtcNow,
                
                // Salaire de base et primes
                SalaireBase = nativeResult.SalaireBase,
                PrimeAnciennete = nativeResult.PrimeAnciennete,
                TotalPrimesImposables = nativeResult.PrimesImposables,
                
                // Extraire les 3 premières primes (compatibilité)
                PrimeImposable1 = nativeResult.PrimesImposablesDetail.ElementAtOrDefault(0)?.Montant,
                PrimeImposable2 = nativeResult.PrimesImposablesDetail.ElementAtOrDefault(1)?.Montant,
                PrimeImposable3 = nativeResult.PrimesImposablesDetail.ElementAtOrDefault(2)?.Montant,
                
                // Brut
                TotalBrut = nativeResult.BrutImposableAjuste,
                BrutImposable = nativeResult.BrutImposable,
                
                // Indemnités
                TotalIndemnites = nativeResult.IndemnitesNonImposables,
                TotalNiExcedentImposable = nativeResult.IndemnitesImposables,
                
                // Cotisations salariales
                CnssPartSalariale = nativeResult.CnssRgSalarial,
                AmoPartSalariale = nativeResult.AmoSalarial,
                CimrPartSalariale = nativeResult.CimrSalarial,
                MutuellePartSalariale = nativeResult.MutuelleSalariale,
                TotalCotisationsSalariales = nativeResult.TotalCotisationsSalariales,
                
                // Cotisations patronales
                CnssPartPatronale = nativeResult.CnssRgPatronal,
                AmoPartPatronale = nativeResult.AmoPatronal,
                CimrPartPatronale = nativeResult.CimrPatronal,
                MutuellePartPatronale = nativeResult.MutuellePatronale,
                TotalCotisationsPatronales = nativeResult.TotalCotisationsPatronales,
                
                // Frais professionnels
                FraisProfessionnels = nativeResult.FraisProfessionnels,
                
                // IR et net
                ImpotRevenu = nativeResult.IR,
                NetImposable = nativeResult.RevenuNetImposable,
                TotalNet = nativeResult.SalaireNetAvantArrondi,
                TotalNet2 = nativeResult.SalaireNet,
                NetAPayer = nativeResult.SalaireNet,
                Arrondi = nativeResult.Arrondi,
                
                // Totaux
                TotalGains = nativeResult.BrutImposableAjuste + nativeResult.IndemnitesNonImposables,
                TotalRetenues = nativeResult.TotalCotisationsSalariales + nativeResult.IR,
                
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = 0
            };
            
            // Ajouter les primes dynamiques
            foreach (var prime in nativeResult.PrimesImposablesDetail)
            {
                payrollResult.Primes.Add(new PayrollResultPrime
                {
                    Label = prime.Label,
                    Montant = prime.Montant,
                    Ordre = payrollResult.Primes.Count + 1,
                    IsTaxable = true
                });
            }
            
            foreach (var indemnite in nativeResult.IndemnitesDetail)
            {
                payrollResult.Primes.Add(new PayrollResultPrime
                {
                    Label = indemnite.Label,
                    Montant = indemnite.PartieExoneree,
                    Ordre = payrollResult.Primes.Count + 1,
                    IsTaxable = false
                });
            }
            
            return payrollResult;
        }
    }
}
