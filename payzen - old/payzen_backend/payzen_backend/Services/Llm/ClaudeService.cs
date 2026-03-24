using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using payzen_backend.Models.Employee.Dtos;

namespace payzen_backend.Services.Llm
{
    public class ClaudeService : IClaudeService
    {
        private readonly AnthropicClient _client;

        public ClaudeService(IConfiguration config)
        {
            var apiKey = config["Anthropic:ApiKey"]
                ?? throw new InvalidOperationException("Anthropic:ApiKey non configuré.");
            _client = new AnthropicClient() { ApiKey = apiKey };
        }

        /// <summary>
        /// Envoie les données de paie à Claude et retourne le JSON de la fiche de paie calculée.
        /// Utilise Claude Sonnet 4.5 avec prompt caching pour optimiser les coûts.
        /// Le caching permet d'économiser jusqu'à 90% sur les règles de paie (statiques).
        /// </summary>
        public async Task<string> AnalyseSalarieAsync(
            string regleContent,
            EmployeePayrollDto payrollData,
            string instruction,
            CancellationToken cancellationToken = default)
        {
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

            // System prompt avec les règles (cachées pour tous les appels)
            var systemPrompt = $$"""
                Tu es un moteur de calcul de paie marocaine expert en PAYZEN DSL v3.0.
                
                RÈGLES DSL CI-DESSOUS (À APPLIQUER STRICTEMENT) :
                {{regleContent}}
                
                INSTRUCTIONS CRITIQUES :
                1. Exécute le @PIPELINE dans l'ordre strict : MODULE[01] à MODULE[14]
                2. Respecte TOUTES les RÈGLES D'OR du guide (lignes 20-44)
                3. Vérifie CHAQUE checkpoint de l'@EXAMPLE pour auto-validation
                4. Évite TOUS les ANTIPATTERNS listés (ap.1 à ap.5)
                5. Le MODULE[09] est CRITIQUE : RNI = brut − cnss − cimr − mutuelle − fp
                6. ⭐ ARRONDI OBLIGATOIRE : Tous les montants à 2 décimales exactes (XX.XX)
                   - Format obligatoire : 1234.00, 500.50, 0.00
                   - JAMAIS : 1234 ou 1234.5 ou 1234.456
                   - Utilise ROUND(valeur, 2) systématiquement
                7. ⭐ ARRONDI COMPTABLE DU NET À PAYER :
                   - Calcule salaire_net_avant_arrondi (avec 2 décimales)
                   - Arrondi à l'unité SUPÉRIEURE : salaire_net = CEIL(salaire_net_avant_arrondi)
                   - Calcule : arrondi_net = salaire_net - salaire_net_avant_arrondi
                   - Exemples : 8827.72→8828.00 (arrondi=0.28), 5432.10→5433.00 (arrondi=0.90)
                
                FORMAT DE SORTIE :
                - Réponds UNIQUEMENT avec un objet JSON valide
                - NE PAS utiliser de markdown, backticks, ou ```json
                - Commence par { et termine par }
                - Inclus le @OUTPUT FicheDePaie complet défini dans le DSL
                - Ajoute un champ "checkpoints_validation" avec les valeurs calculées
                - ⭐ TOUS les montants avec EXACTEMENT 2 décimales (.00 ou .XX)
                
                STRUCTURE JSON ATTENDUE :
                {
                  "employe": {"nom": "...", "prenom": "..."},
                  "periode": {"mois_paie": "...", "anciennete_annees": ...},
                  "salaire_base_mensuel": 0.00,
                  "prime_anciennete": 0.00,
                  "total_hsupp": 0.00,
                  "salaire_brut_imposable": 0.00,
                  "total_cnss_salarial": 0.00,
                  "cimr_salarial": 0.00,
                  "mutuelle_salariale": 0.00,
                  "montant_fp": 0.00,
                  "revenu_net_imposable": 0.00,
                  "ir_final": 0.00,
                  "salaire_net_avant_arrondi": 0.00,
                  "arrondi_net": 0.00,
                  "salaire_net": 0.00,
                  "total_cnss_patronal": 0.00,
                  "total_charges_patronales": 0.00,
                  "cout_employeur_total": 0.00,
                  "checkpoints_validation": {...},
                  "audit_trail": [...]
                }
                """;

            var userMessage = $$"""
                {{instruction}}
                
                Données de l'employé à traiter (format DSL) :
                {{dslJson}}
                
                Rappel : Réponds UNIQUEMENT avec le JSON, sans markdown, sans explication.
                """;

            var parameters = new MessageCreateParams
            {
                Model = "claude-sonnet-4-5-20250929",
                MaxTokens = 8192,  // ⭐ Augmenté de 4096 à 8192 pour éviter la troncature

                // System avec cache control : les règles sont mises en cache
                System = new MessageCreateParamsSystem(
                    new[]
                    {
                        new TextBlockParam
                        {
                            Text = systemPrompt,
                            CacheControl = new CacheControlEphemeral()
                        }
                    }
                ),

                // User message : données spécifiques (non cachées)
                Messages =
                [
                    new()
                    {
                        Role = Role.User,
                        Content = userMessage
                    }
                ]
            };

            var response = await _client.Messages.Create(parameters, cancellationToken);
            
            // ⭐ Logger les statistiques d'utilisation du cache
            Console.WriteLine($"📊 Statistiques Claude :");
            Console.WriteLine($"   - Modèle : {response.Model}");
            Console.WriteLine($"   - StopReason : {response.StopReason}");
            Console.WriteLine($"   - Usage.InputTokens : {response.Usage?.InputTokens ?? 0}");
            Console.WriteLine($"   - Usage.OutputTokens : {response.Usage?.OutputTokens ?? 0}");
            
            // ⚠️ Avertir si la réponse a été tronquée
            if (response.StopReason == "max_tokens")
            {
                Console.WriteLine($"⚠️⚠️⚠️  ATTENTION : Réponse TRONQUÉE ! Claude a atteint la limite de {parameters.MaxTokens} tokens.");
                Console.WriteLine($"   Le JSON retourné est probablement INCOMPLET.");
            }
            
            // Vérifier si le cache est utilisé (si disponible dans l'objet Usage)
            var usageType = response.Usage?.GetType();
            var cacheReadProp = usageType?.GetProperty("CacheReadInputTokens");
            var cacheCreationProp = usageType?.GetProperty("CacheCreationInputTokens");
            
            if (cacheReadProp != null)
            {
                var cacheRead = cacheReadProp.GetValue(response.Usage);
                var cacheReadValue = cacheRead != null ? Convert.ToInt32(cacheRead) : 0;
                if (cacheReadValue > 0)
                {
                    Console.WriteLine($"   - 🎯 Cache Read Tokens : {cacheReadValue} (économie de ~90% !)");
                }
            }
            if (cacheCreationProp != null)
            {
                var cacheCreation = cacheCreationProp.GetValue(response.Usage);
                var cacheCreationValue = cacheCreation != null ? Convert.ToInt32(cacheCreation) : 0;
                if (cacheCreationValue > 0)
                {
                    Console.WriteLine($"   - 💾 Cache Creation Tokens : {cacheCreationValue} (première fois)");
                }
            }

            // Extraire le texte du premier bloc de type "text" dans la réponse
            TextBlock? textBlock = null;
            if (response.Content != null)
            {
                foreach (var contentBlock in response.Content)
                {
                    if (contentBlock.TryPickText(out var tb) && tb != null)
                    {
                        textBlock = tb;
                        break;
                    }
                }
            }
            var text = textBlock?.Text
                ?? throw new InvalidOperationException("Réponse Claude sans contenu texte.");

            // Nettoyer la réponse : enlever markdown et espaces
            var cleanedText = CleanJsonResponse(text.Trim());
            
            return cleanedText;
        }

        /// <summary>
        /// Adapte les données EmployeePayrollDto au format DSL attendu par les règles de paie.
        /// Agrège toutes les primes imposables (SalaryComponents + PackageItems taxables).
        /// </summary>
        private static object AdaptToDslFormat(EmployeePayrollDto data)
        {
            // Calculer jours travaillés à partir des absences
            var joursAbsents = data.Absences?.Count(a => a.DurationType == "FullDay") ?? 0;
            var joursTravailles = 26 - joursAbsents;
            
            // Calculer heures sup par catégorie selon le multiplicateur
            var hSup25 = data.Overtimes?
                .Where(o => o.RateMultiplier >= 1.20m && o.RateMultiplier < 1.40m)
                .Sum(o => o.DurationInHours) ?? 0;
            var hSup50 = data.Overtimes?
                .Where(o => o.RateMultiplier >= 1.40m && o.RateMultiplier < 1.75m)
                .Sum(o => o.DurationInHours) ?? 0;
            var hSup100 = data.Overtimes?
                .Where(o => o.RateMultiplier >= 1.75m)
                .Sum(o => o.DurationInHours) ?? 0;
            
            // ========== PRIMES IMPOSABLES : LISTE DYNAMIQUE ==========
            // Les VRAIES primes sont UNIQUEMENT dans les PackageItems
            // Les SalaryComponents sont des COMPOSANTS du salaire de base (pas des primes)
            
            var primesImposables = new List<object>();
            
            // Éléments du package imposables (IsTaxable = true) → Ce sont les PRIMES
            var packageItemsImposables = data.PackageItems?.Where(p => p.IsTaxable).ToList() ?? new();
            var packageItemsNonImposables = data.PackageItems?.Where(p => !p.IsTaxable).ToList() ?? new();
            
            Console.WriteLine($"📊 Analyse des primes :");
            
            // ⚠️ SalaryComponents = composants du salaire de base (pas des primes)
            Console.WriteLine($"   📋 SalaryComponents (salaire de base) : {data.SalaryComponents?.Count ?? 0} - NON envoyés comme primes");
            if (data.SalaryComponents != null && data.SalaryComponents.Any())
            {
                foreach (var comp in data.SalaryComponents)
                {
                    Console.WriteLine($"      - {comp.ComponentType} : {comp.Amount} MAD (composant du salaire de base)");
                }
            }
            
            // ✅ PackageItems imposables = VRAIES PRIMES
            Console.WriteLine($"   ✅ Primes imposables (PackageItems avec IsTaxable=true) : {packageItemsImposables.Count}");
            foreach (var item in packageItemsImposables)
            {
                Console.WriteLine($"      - {item.Label} : {item.DefaultValue} MAD");
                primesImposables.Add(new { label = item.Label, montant = item.DefaultValue });
            }
            
            Console.WriteLine($"   ⚪ Indemnités non imposables (PackageItems avec IsTaxable=false) : {packageItemsNonImposables.Count}");
            foreach (var item in packageItemsNonImposables)
            {
                Console.WriteLine($"      - {item.Label} : {item.DefaultValue} MAD");
            }
            
            Console.WriteLine($"💰 Total primes imposables envoyées au LLM : {primesImposables.Count}");
            
            // Plus besoin d'agrégation ! Le DSL gère ça avec SUM(primes_imposables[*].montant)
            
            // ========== INDEMNITÉS NON IMPOSABLES ==========
            // Extraire des PackageItems les éléments non imposables
            var niTransport = data.PackageItems?
                .FirstOrDefault(p => !p.IsTaxable && (p.Label.Contains("Transport", StringComparison.OrdinalIgnoreCase) || p.Type == "Transport"))
                ?.DefaultValue ?? 0;
            
            var niPanier = data.PackageItems?
                .FirstOrDefault(p => !p.IsTaxable && p.Label.Contains("Panier", StringComparison.OrdinalIgnoreCase))
                ?.DefaultValue ?? 0;
            
            var niRepresentation = data.PackageItems?
                .FirstOrDefault(p => !p.IsTaxable && p.Label.Contains("Représentation", StringComparison.OrdinalIgnoreCase))
                ?.DefaultValue ?? 0;
            
            // Autres NI non catégorisées
            var autresNi = data.PackageItems?
                .Where(p => !p.IsTaxable 
                    && !p.Label.Contains("Transport", StringComparison.OrdinalIgnoreCase)
                    && !p.Label.Contains("Panier", StringComparison.OrdinalIgnoreCase)
                    && !p.Label.Contains("Représentation", StringComparison.OrdinalIgnoreCase))
                .Sum(p => p.DefaultValue) ?? 0;
            
            // ========== RÉGIME CIMR ==========
            string regimeCimr = "AUCUN";
            if (data.CimrEmployeeRate.HasValue && data.CimrEmployeeRate > 0)
            {
                regimeCimr = "AL_KAMIL"; // Par défaut, peut être affiné
            }
            
            // ========== MUTUELLE ==========
            decimal mutuelleSalariale = 0;
            if (data.HasPrivateInsurance && data.PrivateInsuranceRate.HasValue)
            {
                mutuelleSalariale = data.BaseSalary * data.PrivateInsuranceRate.Value / 100;
            }
            
            // Construire l'objet DSL
            return new
            {
                // Identité
                nom = data.FullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "",
                prenom = data.FullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "",
                matricule = $"EMP-{data.CinNumber}",
                cin = data.CinNumber,
                cnss_numero = data.CnssNumber,
                fonction = data.JobPosition,
                contrat = MapContractType(data.ContractType),
                
                // Dates
                date_embauche = data.ContractStartDate,
                mois_paie = $"{data.PayYear:D4}-{data.PayMonth:D2}",
                
                // Famille
                situation_fam = data.NumberOfChildren + (data.HasSpouse ? 1 : 0),
                
                // Salaire et présence
                salaire_base_26j = data.BaseSalary,
                jours_travailles = joursTravailles,
                jours_feries = 0, // À améliorer : détecter les jours fériés dans le mois
                jours_conge = (int)(data.Leaves?.Sum(l => l.DaysCount) ?? 0),
                heures_mois = 191,
                
                // Heures supplémentaires par catégorie
                h_sup_25pct = hSup25,
                h_sup_50pct = hSup50,
                h_sup_100pct = hSup100,
                
                // PRIMES IMPOSABLES : Liste dynamique (nouvelle approche DSL v3.1)
                primes_imposables = primesImposables,
                
                // CIMR
                regime_cimr = regimeCimr,
                cimr_taux_salarial = (data.CimrEmployeeRate ?? 0) / 100,
                cimr_taux_patronal = (data.CimrCompanyRate ?? 0) / 100,
                
                // Mutuelle
                mutuelle_salariale = mutuelleSalariale,
                mutuelle_patronale = 0, // Non géré actuellement
                
                // Indemnités non imposables
                ni_transport = niTransport,
                ni_kilometrique = 0,
                ni_tournee = 0,
                ni_representation = niRepresentation,
                ni_panier = niPanier,
                ni_caisse = 0,
                ni_salissure = 0,
                ni_lait = 0,
                ni_outillage = 0,
                ni_aide_medicale = 0,
                ni_gratif_sociale = 0,
                ni_autres = autresNi,
                
                // Avances et prêts (non gérés actuellement)
                avance_salaire = 0,
                interet_pret_logement = 0,
                
                // Métadonnées pour le debug
                _metadata = new
                {
                    total_primes = primesImposables.Count,
                    dsl_version = "3.1",
                    feature = "Liste dynamique primes_imposables",
                    source = "EmployeePayrollDto -> DSL Adapter"
                }
            };
        }
        
        /// <summary>
        /// Mappe le type de contrat vers les codes DSL
        /// </summary>
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
            
            return "PP"; // Par défaut
        }

        /// <summary>
        /// Nettoie la réponse de Claude pour extraire uniquement le JSON valide.
        /// Enlève le markdown (```json, ```), les backticks, et les textes parasites.
        /// </summary>
        private static string CleanJsonResponse(string response)
        {
            // Enlever les blocs markdown ```json ... ```
            if (response.StartsWith("```"))
            {
                // Trouver la première ligne (qui contient ```json ou juste ```)
                var firstNewLine = response.IndexOf('\n');
                if (firstNewLine > 0)
                {
                    response = response.Substring(firstNewLine + 1);
                }
                
                // Enlever les ``` de fin
                if (response.EndsWith("```"))
                {
                    response = response.Substring(0, response.LastIndexOf("```"));
                }
            }

            // Enlever les backticks simples au début/fin
            response = response.Trim('`').Trim();

            // Trouver le premier { et le dernier }
            var firstBrace = response.IndexOf('{');
            var lastBrace = response.LastIndexOf('}');

            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                response = response.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            return response.Trim();
        }
    }
}
