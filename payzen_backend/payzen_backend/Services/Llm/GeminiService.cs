using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using payzen_backend.Models.Employee.Dtos;

namespace payzen_backend.Services.Llm
{
    /// <summary>
    /// Service pour interagir avec Google Gemini API (100% gratuit jusqu'à 15 req/min)
    /// </summary>
    public class GeminiService : IClaudeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<GeminiService> logger)
        {
            _apiKey = config["Google:ApiKey"]
                ?? throw new InvalidOperationException("Google:ApiKey non configuré dans appsettings.json");
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task<string> AnalyseSalarieAsync(
            string regleContent,
            EmployeePayrollDto payrollData,
            string instruction,
            CancellationToken cancellationToken = default)
        {
            // 📊 LOGS DÉTAILLÉS DES PRIMES AVANT ENVOI AU LLM
            _logger.LogInformation("📊 ═══════════════════════════════════════════════════════════════");
            _logger.LogInformation("📊 PRIMES ENVOYÉES À GEMINI (depuis la DB)");
            _logger.LogInformation("📊 ⚠️  Mode : UNIQUEMENT SalaryComponents (PackageItems ignorés)");
            _logger.LogInformation("📊 ═══════════════════════════════════════════════════════════════");
            
            _logger.LogInformation($"👤 Employé : {payrollData.FullName}");
            _logger.LogInformation($"💰 Salaire de base : {payrollData.BaseSalary:N2} MAD");
            _logger.LogInformation("");
            
            _logger.LogInformation($"📋 SalaryComponents : {payrollData.SalaryComponents?.Count ?? 0}");
            if (payrollData.SalaryComponents != null && payrollData.SalaryComponents.Any())
            {
                var imposables = payrollData.SalaryComponents.Where(c => c.IsTaxable).ToList();
                var nonImposables = payrollData.SalaryComponents.Where(c => !c.IsTaxable).ToList();
                
                _logger.LogInformation("");
                _logger.LogInformation($"✅ IMPOSABLES : {imposables.Count} items");
                foreach (var comp in imposables)
                {
                    _logger.LogInformation($"   • {comp.ComponentType,-30} : {comp.Amount,10:N2} MAD");
                    _logger.LogInformation($"     └─ IsTaxable={comp.IsTaxable}, IsSocial={comp.IsSocial}, IsCIMR={comp.IsCIMR}");
                }
                
                _logger.LogInformation("");
                _logger.LogInformation($"⚪ NON IMPOSABLES : {nonImposables.Count} items");
                foreach (var comp in nonImposables)
                {
                    _logger.LogInformation($"   • {comp.ComponentType,-30} : {comp.Amount,10:N2} MAD");
                    _logger.LogInformation($"     └─ IsTaxable={comp.IsTaxable}, IsSocial={comp.IsSocial}, IsCIMR={comp.IsCIMR}");
                }
            }
            _logger.LogInformation("📊 ═══════════════════════════════════════════════════════════════");
            _logger.LogInformation("");
            
            var dataJson = JsonSerializer.Serialize(payrollData, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // Adapter les données au format DSL
            var dslAdaptedData = AdaptToDslFormat(payrollData);
            var dslJson = JsonSerializer.Serialize(dslAdaptedData, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var systemPrompt = $$"""
                Tu es un moteur de calcul de paie marocaine expert en PAYZEN DSL v3.1.
                
                RÈGLES DSL CI-DESSOUS (À APPLIQUER STRICTEMENT) :
                {{regleContent}}
                
                INSTRUCTIONS CRITIQUES :
                1. Exécute le @PIPELINE dans l'ordre strict : MODULE[01] à MODULE[12]
                2. Respecte TOUTES les RÈGLES CRITIQUES du DSL
                3. Le MODULE[09] est CRITIQUE : RNI = brut − cnss − cimr − mutuelle − fp
                4. ⭐ ARRONDI OBLIGATOIRE : Tous les montants DOIVENT être arrondis à 2 décimales exactes
                   - Utilise ROUND(valeur, 2) pour CHAQUE calcul
                   - Format : 1234.00, 500.50, 0.00 (JAMAIS 1234, JAMAIS 1234.5, JAMAIS 1234.456)
                   - Exemples corrects : 9900.00, 492.54, 2475.00
                   - Exemples INCORRECTS : 9900, 492.5, 2475
                5. ⭐ ARRONDI COMPTABLE DU NET À PAYER :
                   - Calcule d'abord le salaire_net_avant_arrondi (avec 2 décimales)
                   - Arrondi le net à l'unité SUPÉRIEURE : salaire_net = CEIL(salaire_net_avant_arrondi)
                   - Calcule l'arrondi : arrondi_net = salaire_net - salaire_net_avant_arrondi
                   - Exemple : 8827.72 → salaire_net = 8828.00, arrondi_net = 0.28
                   - Exemple : 5432.10 → salaire_net = 5433.00, arrondi_net = 0.90
                   - Exemple : 3000.00 → salaire_net = 3000.00, arrondi_net = 0.00
                6. ⭐ PLAFONDS INDEMNITÉS NON IMPOSABLES (MODULE[06] - CRITIQUE) :
                   - OBLIGATOIRE : Applique les plafonds définis dans les RÈGLES DSL
                   - Transport : MAX 500.00 MAD (PLAFOND_NI_TRANSPORT)
                   - Panier : MAX 34.20 MAD/jour × jours travaillés (PLAFOND_NI_PANIER_JOUR)
                   - Représentation : MAX 10% du salaire de base (PLAFOND_NI_REPRESENTATION)
                   - Tournée : MAX 1500.00 MAD (PLAFOND_NI_TOURNEE)
                   - Caisse : MAX 239.00 MAD pour CNSS, 190.00 MAD pour DGI
                   - Outillage : MAX 119.00 MAD pour CNSS, 100.00 MAD pour DGI
                   - Salissure : MAX 239.00 MAD pour CNSS, 210.00 MAD pour DGI
                   - IMPORTANT : partie_exoneree = MIN(montant_saisi, plafond)
                   - IMPORTANT : partie_imposable = MAX(0, montant_saisi - plafond)
                
                FORMAT DE SORTIE :
                - Réponds UNIQUEMENT avec un objet JSON valide
                - NE PAS utiliser de markdown, backticks, ou ```json
                - Commence par { et termine par }
                - ⭐ TOUS les champs numériques doivent avoir EXACTEMENT 2 décimales (.00 ou .XX)
                
                STRUCTURE JSON ATTENDUE :
                {
                  "employe": {"nom": "...", "prenom": "..."},
                  "periode": {"mois_paie": "...", "anciennete_annees": ...},
                  "salaire_base_mensuel": 0.00,
                  "prime_anciennete": 0.00,
                  "primes_imposables": [
                    {"label": "Prime 1", "montant": 0.00},
                    {"label": "Prime 2", "montant": 0.00}
                  ],
                  "total_primes_imposables": 0.00,
                  "indemnites_non_imposables": [
                    {"label": "Indemnité 1", "montant_saisi": 0.00, "plafond_applicable": 0.00, "partie_exoneree": 0.00, "partie_imposable": 0.00}
                  ],
                  "total_ni_exonere": 0.00,
                  "total_ni_imposable": 0.00,
                  "total_hsupp": 0.00,
                  "salaire_brut_imposable": 0.00,
                  "cnss_rg_salarial": 0.00,
                  "cnss_amo_salarial": 0.00,
                  "total_cnss_salarial": 0.00,
                  "cimr_salarial": 0.00,
                  "mutuelle_salariale": 0.00,
                  "montant_fp": 0.00,
                  "revenu_net_imposable": 0.00,
                  "ir_final": 0.00,
                  "salaire_net_avant_arrondi": 0.00,
                  "arrondi_net": 0.00,
                  "salaire_net": 0.00,
                  "cnss_rg_patronal": 0.00,
                  "cnss_amo_patronal": 0.00,
                  "total_cnss_patronal": 0.00,
                  "total_charges_patronales": 0.00,
                  "cout_employeur_total": 0.00
                }
                
                ⭐ EXEMPLE INDEMNITÉS NON IMPOSABLES (CRITIQUE) :
                "indemnites_non_imposables": [
                  {
                    "label": "TRANSPORT",
                    "montant_saisi": 1000.00,
                    "plafond_applicable": 500.00,
                    "partie_exoneree": 500.00,
                    "partie_imposable": 500.00
                  },
                  {
                    "label": "Prime de Panier",
                    "montant_saisi": 1000.00,
                    "plafond_applicable": 889.20,
                    "partie_exoneree": 889.20,
                    "partie_imposable": 110.80
                  }
                ],
                "total_ni_exonere": 1389.20,
                "total_ni_imposable": 610.80
                
                ⚠️  NE PAS FAIRE :
                "les primes imposables": {"Prime 1": 1000, "Prime 2": 500}  ❌ MAUVAIS FORMAT
                
                ✅ FORMAT CORRECT :
                "primes_imposables": [{"label": "...", "montant": 0.00}, ...]
                """;

            var userMessage = $$"""
                {{instruction}}
                
                Données de l'employé à traiter (format DSL) :
                {{dslJson}}
                
                Rappel : Réponds UNIQUEMENT avec le JSON, sans markdown, sans explication.
                """;

            // Construire la requête pour Gemini 1.5 Flash (gratuit)
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = $"{systemPrompt}\n\n{userMessage}" }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    topK = 40,
                    topP = 0.95,
                    maxOutputTokens = 8192,  // ⭐ Réduit de 16384 à 8192 (suffisant pour une fiche de paie)
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // URL de l'API Gemini 2.5 Flash (modèle le plus récent et performant)
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            _logger.LogInformation("🌐 Appel à Gemini 1.5 Flash API...");
            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Erreur Gemini API: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Gemini API error: {response.StatusCode} - {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var geminiResponse = JsonDocument.Parse(responseJson);

            // Extraire le texte de la réponse
            var candidates = geminiResponse.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("Gemini n'a retourné aucun candidat de réponse.");
            }

            var firstCandidate = candidates[0];
            
            // ⭐ Vérifier si la réponse a été tronquée
            if (firstCandidate.TryGetProperty("finishReason", out var finishReason))
            {
                var reason = finishReason.GetString();
                Console.WriteLine($"⚙️  Gemini finishReason: {reason}");
                
                if (reason == "MAX_TOKENS")
                {
                    _logger.LogWarning("⚠️  ATTENTION : La réponse de Gemini a été TRONQUÉE (MAX_TOKENS atteint). Augmentez maxOutputTokens.");
                }
            }
            
            var contentProp = firstCandidate.GetProperty("content");
            var parts = contentProp.GetProperty("parts");
            var textPart = parts[0].GetProperty("text").GetString();

            if (string.IsNullOrEmpty(textPart))
            {
                throw new InvalidOperationException("Gemini a retourné une réponse vide.");
            }

            // Logger les statistiques
            if (geminiResponse.RootElement.TryGetProperty("usageMetadata", out var usage))
            {
                var inputTokens = usage.GetProperty("promptTokenCount").GetInt32();
                var outputTokens = usage.GetProperty("candidatesTokenCount").GetInt32();
                var totalTokens = usage.GetProperty("totalTokenCount").GetInt32();
                
                Console.WriteLine($"📊 Statistiques Gemini :");
                Console.WriteLine($"   - Modèle : gemini-2.5-flash");
                Console.WriteLine($"   - Input Tokens : {inputTokens}");
                Console.WriteLine($"   - Output Tokens : {outputTokens}");
                Console.WriteLine($"   - Total Tokens : {totalTokens}");
                Console.WriteLine($"   - 💰 Coût : GRATUIT (15 req/min, 1500 req/jour)");
                
                // ⭐ Alerte si on approche de la limite
                if (outputTokens > 15000)
                {
                    _logger.LogWarning($"⚠️  Output tokens élevé ({outputTokens}/16384). Risque de troncation.");
                }
            }

            // Nettoyer la réponse
            var cleanedText = CleanJsonResponse(textPart.Trim());
            
            return cleanedText;
        }

        /// <summary>
        /// Adapte les données au format DSL (identique à ClaudeService)
        /// </summary>
        private object AdaptToDslFormat(EmployeePayrollDto data)
        {
            var joursAbsents = data.Absences?.Count(a => a.DurationType == "FullDay") ?? 0;
            var joursTravailles = 26 - joursAbsents;
            
            var hSup25 = data.Overtimes?.Where(o => o.RateMultiplier >= 1.20m && o.RateMultiplier < 1.40m).Sum(o => o.DurationInHours) ?? 0;
            var hSup50 = data.Overtimes?.Where(o => o.RateMultiplier >= 1.40m && o.RateMultiplier < 1.75m).Sum(o => o.DurationInHours) ?? 0;
            var hSup100 = data.Overtimes?.Where(o => o.RateMultiplier >= 1.75m).Sum(o => o.DurationInHours) ?? 0;
            
            var primesImposables = new List<object>();
            var primesNonImposables = new List<object>();
            
            _logger.LogInformation("🔄 Conversion au format DSL pour Gemini :");
            _logger.LogInformation("   ⚠️  Mode : UNIQUEMENT SalaryComponents (PackageItems ignorés)");
            
            // Extraire UNIQUEMENT les primes depuis SalaryComponents
            var salaryComponents = data.SalaryComponents?.ToList() ?? new();
            var componentsImposables = salaryComponents.Where(c => c.IsTaxable).ToList();
            var componentsNonImposables = salaryComponents.Where(c => !c.IsTaxable).ToList();
            
            _logger.LogInformation($"   - SalaryComponents imposables : {componentsImposables.Count}");
            foreach (var comp in componentsImposables)
            {
                primesImposables.Add(new { label = comp.ComponentType, montant = comp.Amount });
                _logger.LogInformation($"     → Prime ajoutée : {comp.ComponentType} = {comp.Amount:N2} MAD");
            }
            
            _logger.LogInformation($"   - SalaryComponents non imposables : {componentsNonImposables.Count}");
            foreach (var comp in componentsNonImposables)
            {
                primesNonImposables.Add(new { label = comp.ComponentType, montant = comp.Amount });
                _logger.LogInformation($"     → Indemnité ajoutée : {comp.ComponentType} = {comp.Amount:N2} MAD");
            }
            
            _logger.LogInformation($"   - TOTAL primes imposables envoyées au LLM : {primesImposables.Count}");
            _logger.LogInformation($"   - TOTAL indemnités non imposables envoyées au LLM : {primesNonImposables.Count}");
            
            var regimeCimr = "AUCUN";
            if (data.CimrEmployeeRate.HasValue && data.CimrEmployeeRate > 0)
            {
                regimeCimr = "AL_KAMIL";
            }
            
            decimal mutuelleSalariale = 0;
            if (data.HasPrivateInsurance && data.PrivateInsuranceRate.HasValue)
            {
                mutuelleSalariale = data.BaseSalary * data.PrivateInsuranceRate.Value / 100;
            }
            
            _logger.LogInformation($"   - Régime CIMR : {regimeCimr}");
            _logger.LogInformation($"   - Mutuelle salariale : {mutuelleSalariale:N2} MAD");
            _logger.LogInformation("");
            
            return new
            {
                nom = data.FullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "",
                prenom = data.FullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "",
                matricule = $"EMP-{data.CinNumber}",
                cin = data.CinNumber,
                cnss_numero = data.CnssNumber,
                fonction = data.JobPosition,
                contrat = MapContractType(data.ContractType),
                date_embauche = data.ContractStartDate,
                mois_paie = $"{data.PayYear:D4}-{data.PayMonth:D2}",
                situation_fam = data.NumberOfChildren + (data.HasSpouse ? 1 : 0),
                salaire_base_26j = data.BaseSalary,
                jours_travailles = joursTravailles,
                jours_feries = 0,
                jours_conge = (int)(data.Leaves?.Sum(l => l.DaysCount) ?? 0),
                heures_mois = 191,
                h_sup_25pct = hSup25,
                h_sup_50pct = hSup50,
                h_sup_100pct = hSup100,
                primes_imposables = primesImposables,
                indemnites_non_imposables = primesNonImposables,
                regime_cimr = regimeCimr,
                cimr_taux_salarial = (data.CimrEmployeeRate ?? 0) / 100,
                cimr_taux_patronal = (data.CimrCompanyRate ?? 0) / 100,
                mutuelle_salariale = mutuelleSalariale,
                mutuelle_patronale = 0,
                avance_salaire = 0,
                interet_pret_logement = 0
            };
        }
        
        private static string MapContractType(string? contractType)
        {
            if (string.IsNullOrEmpty(contractType)) return "PP";
            if (contractType.Contains("CDI", StringComparison.OrdinalIgnoreCase) || 
                contractType.Contains("Permanent", StringComparison.OrdinalIgnoreCase))
                return "PP";
            if (contractType.Contains("Partiel", StringComparison.OrdinalIgnoreCase))
                return "PO";
            if (contractType.Contains("Stage", StringComparison.OrdinalIgnoreCase))
                return "STG";
            if (contractType.Contains("IDMAJ", StringComparison.OrdinalIgnoreCase))
                return "ANAPEC_IDMAJ";
            if (contractType.Contains("TAHFIZ", StringComparison.OrdinalIgnoreCase))
                return "ANAPEC_TAHFIZ";
            return "PP";
        }

        private static string CleanJsonResponse(string response)
        {
            if (response.StartsWith("```json"))
            {
                response = response.Substring(7);
            }
            else if (response.StartsWith("```"))
            {
                response = response.Substring(3);
            }
            
            if (response.EndsWith("```"))
            {
                response = response.Substring(0, response.Length - 3);
            }
            
            return response.Trim();
        }
    }
}
