using System.Text.Json;
using System.Net.Http.Headers;

namespace payzen_backend.Services.Llm
{
    public class ClaudeSimulationService : IClaudeSimulationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ClaudeSimulationService> _logger;
        private readonly string _apiKey;
        private const string GEMINI_MODEL = "gemini-2.5-flash-lite";
        private const string GEMINI_API_BASE = "https://generativelanguage.googleapis.com/v1beta/models";

        public ClaudeSimulationService(
            IConfiguration config,
            ILogger<ClaudeSimulationService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _apiKey = config["Google:ApiKey"] ?? 
                throw new InvalidOperationException("Google:ApiKey non configuré dans appsettings.json");
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _logger = logger;
        }

        /// <summary>
        /// Envoie une requête à l'API Google Gemini pour simuler des éléments de paie selon les règles fournies
        /// par le DSL
        /// </summary> 
        public async Task<string> SimulationSalaryAsync(
            string regleContent,
            string instruction,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Démarrage de la simulation de paie avec Gemini");

                // Construction du prompt système avec les règles DSL
                var systemPrompt = BuildSystemPrompt(regleContent);

                // Construction du prompt utilisateur
                var userPrompt = BuildUserPrompt(instruction);

                // Combinaison des prompts pour Gemini
                var fullPrompt = $"{systemPrompt}\n\n{userPrompt}";

                // Préparation de la requête pour Gemini API
                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = fullPrompt }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 8192,
                        responseMimeType = "application/json"
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                _logger.LogDebug("Envoi de la requête à l'API Gemini");
                _logger.LogInformation("?? Paramètres de la requête - Model: {Model}, MaxTokens: {MaxTokens}", GEMINI_MODEL, 8192);

                // Appel à l'API Gemini
                var url = $"{GEMINI_API_BASE}/{GEMINI_MODEL}:generateContent?key={_apiKey}";
                var httpResponse = await _httpClient.PostAsync(url, content, cancellationToken);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Erreur API Gemini: {StatusCode} - {Error}", httpResponse.StatusCode, errorContent);
                    throw new InvalidOperationException($"Erreur API Gemini: {httpResponse.StatusCode}");
                }

                var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                
                // Parser la réponse Gemini
                using var geminiResponse = JsonDocument.Parse(responseContent);
                var responseText = geminiResponse.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? throw new InvalidOperationException("Réponse Gemini sans contenu texte.");

                _logger.LogInformation("?? Réponse brute reçue - Longueur: {Length} caractères", responseText.Length);
                _logger.LogDebug("?? Réponse brute complète:\n{Response}", responseText);

                // Nettoyer la réponse (supprimer les backticks markdown si présents)
                var cleanedResponse = CleanJsonResponse(responseText);
                
                _logger.LogInformation("?? JSON nettoyé - Longueur: {Length} caractères", cleanedResponse.Length);
                _logger.LogDebug("?? JSON nettoyé:\n{CleanedResponse}", cleanedResponse);

                // Validation du format JSON
                try
                {
                    // Tenter de parser pour valider le JSON
                    using var jsonDoc = JsonDocument.Parse(cleanedResponse);
                    var root = jsonDoc.RootElement;
                    
                    // Vérifier si le LLM a retourné une erreur au lieu des scénarios
                    if (root.TryGetProperty("error", out var errorProp))
                    {
                        var errorTitle = errorProp.GetString() ?? "Demande invalide";
                        var detailedMessage = errorTitle;
                        
                        if (root.TryGetProperty("message", out var msgProp))
                        {
                            var msg = msgProp.GetString();
                            if (!string.IsNullOrEmpty(msg))
                                detailedMessage = msg;
                        }
                        
                        if (root.TryGetProperty("instructions", out var instrProp))
                        {
                            var instr = instrProp.GetString();
                            if (!string.IsNullOrEmpty(instr))
                                detailedMessage += "\n\n" + instr;
                        }
                        
                        // Ajouter les exemples si disponibles
                        if (root.TryGetProperty("exemples_valides", out var exemplesProp) && exemplesProp.ValueKind == JsonValueKind.Array)
                        {
                            detailedMessage += "\n\nExemples valides :";
                            foreach (var exemple in exemplesProp.EnumerateArray())
                            {
                                detailedMessage += "\n• " + exemple.GetString();
                            }
                        }
                        
                        _logger.LogWarning("?? Demande utilisateur non claire - Le LLM demande des précisions : {Error}", 
                            detailedMessage);
                        
                        // ArgumentException sera transformé en BadRequest par le contrôleur
                        throw new ArgumentException(detailedMessage);
                    }
                    
                    // Vérifier que la réponse contient bien les scénarios attendus
                    if (!root.TryGetProperty("scenarios", out var scenariosProp))
                    {
                        _logger.LogWarning("?? Réponse JSON valide mais sans champ 'scenarios' : {Response}", cleanedResponse);
                        
                        throw new InvalidOperationException(
                            $"Le LLM a retourné un JSON valide mais il manque le champ 'scenarios'.\n\n" +
                            $"Réponse reçue : {cleanedResponse.Substring(0, Math.Min(300, cleanedResponse.Length))}...");
                    }
                    
                    _logger.LogInformation("? Simulation de paie terminée avec succès - JSON valide avec {Count} scénarios", 
                        scenariosProp.GetArrayLength());
                    return cleanedResponse;
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "? ERREUR JSON INVALIDE");
                    _logger.LogError("?? Position erreur: Ligne {Line}, Colonne {Column}", jsonEx.LineNumber, jsonEx.BytePositionInLine);
                    _logger.LogError("?? Premiers 1000 caractères du JSON:\n{JsonStart}", cleanedResponse.Substring(0, Math.Min(1000, cleanedResponse.Length)));
                    _logger.LogError("?? Derniers 500 caractères du JSON:\n{JsonEnd}", cleanedResponse.Length > 500 ? cleanedResponse.Substring(cleanedResponse.Length - 500) : cleanedResponse);
                    
                    // Vérifier si le JSON est simplement incomplet (coupé par MaxTokens)
                    var errorMsg = $"Le LLM a retourné un JSON invalide. Erreur de parsing : {jsonEx.Message}\n\n" +
                        $"Position de l'erreur : Ligne {jsonEx.LineNumber}, Colonne {jsonEx.BytePositionInLine}\n\n";
                    
                    if (jsonEx.Message.Contains("end of data") || jsonEx.Message.Contains("incomplete"))
                    {
                        errorMsg += $"?? Le JSON semble incomplet (probablement coupé par la limite de tokens).\n" +
                            $"Longueur de la réponse : {responseText.Length} caractères\n\n";
                    }
                    
                    errorMsg += $"Consultez les logs pour voir le JSON complet.";
                    
                    throw new InvalidOperationException(errorMsg, jsonEx);
                }
            }
            catch (ArgumentException)
            {
                // Demande utilisateur non claire - propager l'exception sans la wrapper
                throw;
            }
            catch (JsonException)
            {
                // Déjà géré ci-dessus, mais on le relance sans wrapper
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la simulation de paie avec Claude");
                throw new InvalidOperationException(
                    "Erreur lors de la simulation de paie. Veuillez réessayer.", ex);
            }
        }

        /// <summary>
        /// Nettoie la réponse JSON en supprimant les backticks markdown et espaces superflus
        /// </summary>
        private string CleanJsonResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogWarning("?? Réponse vide ou null passée à CleanJsonResponse");
                return response;
            }

            _logger.LogDebug("?? Nettoyage JSON - Longueur initiale: {Length}", response.Length);
            
            // Supprimer les blocs markdown code (```json ... ``` ou ``` ... ```)
            var cleaned = response.Trim();
            
            if (cleaned.StartsWith("```"))
            {
                _logger.LogDebug("?? Détection de blocs markdown - Suppression des backticks");
                
                // Trouver la fin du premier ```
                var firstLineEnd = cleaned.IndexOf('\n');
                if (firstLineEnd > 0)
                {
                    cleaned = cleaned.Substring(firstLineEnd + 1);
                }
                
                // Supprimer les ``` de fin
                if (cleaned.EndsWith("```"))
                {
                    cleaned = cleaned.Substring(0, cleaned.Length - 3);
                }
                
                cleaned = cleaned.Trim();
                _logger.LogDebug("?? Après suppression markdown - Longueur: {Length}", cleaned.Length);
            }
            
            // Vérifications supplémentaires
            if (!cleaned.StartsWith("{") && !cleaned.StartsWith("["))
            {
                _logger.LogWarning("?? Le JSON nettoyé ne commence pas par {{ ou [ : {Start}", 
                    cleaned.Substring(0, Math.Min(50, cleaned.Length)));
            }
            
            if (!cleaned.EndsWith("}") && !cleaned.EndsWith("]"))
            {
                _logger.LogWarning("?? Le JSON nettoyé ne se termine pas par }} ou ] : {End}", 
                    cleaned.Length > 50 ? cleaned.Substring(cleaned.Length - 50) : cleaned);
            }
            
            return cleaned;
        }

        /// <summary>
        /// Construit le prompt système contenant les règles de calcul de paie
        /// </summary>
        public static string BuildSystemPrompt(string regleContent)
        {
            return $@"Tu es un expert-comptable spécialisé en droit social marocain et en optimisation de la rémunération salariale.

Tu dois STRICTEMENT appliquer les règles définies dans le fichier DSL PayZen v3.1 ci-dessous.
Ne jamais inventer de taux ou de règles. Tout est dans le DSL.

<payzen_dsl>
;;; ============================================================
;;; PAYZEN DSL — Règles de Paie Marocaine
;;; Version   : 3.1  (primes imposables — liste dynamique)
;;; Juridiction: Maroc (MA)
;;; Devise     : MAD (Dirham marocain)
;;; Sources    : CNSS Décret 2.25.266 (2025) · CGI Art.59
;;;              Loi Finances 2023 · Code du Travail Marocain
;;; ============================================================

;;; RÈGLE D'OR N°1 — ORDRE DES DÉDUCTIONS AVANT IR :
;;;   RNI = Brut Imposable
;;;         - CNSS_salarial  (RG + AMO)
;;;         - CIMR_salarial
;;;         - Mutuelle_salariale
;;;         - Frais_Professionnels  (% calculé sur brut)
;;;         - Intérêt_prêt_logement

;;; RÈGLE D'OR N°2 — FRAIS PROFESSIONNELS :
;;;   Le TAUX FP (25% ou 35%) s'applique sur le BRUT IMPOSABLE
;;;   PAS sur (brut - cnss).
;;;   montant_fp = MIN(brut × taux, 2916.67)
;;;   Si brut=9900 ? 9900×25%=2475 < 2916.67 ? fp=2475 (?2916.67)

;;; RÈGLE D'OR N°3 — NE PAS CONFONDRE :
;;;   base_fp        = salaire_brut_imposable (pour le taux)
;;;   revenu_net_imp = brut - cnss - cimr - mutuelle - fp

;;; RÈGLE D'OR N°4 — VÉRIFIER VIA CHECKPOINT :
;;;   Après chaque module, vérifier la cohérence des chiffres.
;;;   SELF_CHECK MODULE[09] : RNI doit être < (brut - fp) si cnss > 0

@CONSTANTS {{
  PLAFOND_CNSS_MENSUEL        : 6000.00
  CNSS_RG_SALARIAL            : 0.0448
  CNSS_RG_PATRONAL            : 0.0898
  CNSS_AMO_SALARIAL           : 0.0226
  CNSS_AMO_PATRONAL           : 0.0226
  CNSS_AMO_PARTICIPATION_PAT  : 0.0185
  CNSS_ALLOC_FAM_PAT          : 0.0640
  CNSS_FP_PAT                 : 0.0160
  PLAFOND_NI_TRANSPORT        : 500.00
  PLAFOND_NI_TRANSPORT_HU     : 750.00
  PLAFOND_NI_TOURNEE          : 1500.00
  PLAFOND_NI_REPRESENTATION   : 0.10
  PLAFOND_NI_PANIER_JOUR      : 34.20
  PLAFOND_NI_CAISSE_DGI       : 190.00
  PLAFOND_NI_LAIT_DGI         : 150.00
  PLAFOND_NI_OUTILLAGE_DGI    : 100.00
  PLAFOND_NI_SALISSURE_DGI    : 210.00
  PLAFOND_NI_GRATIF_DGI       : 2500.00
  IR_DEDUCTION_FAMILLE        : 30.00
}}

MODULE[01] anciennete {{
  WHEN anciennete_annees < 2    THEN taux_anciennete = 0.00
  WHEN anciennete_annees < 5    THEN taux_anciennete = 0.05
  WHEN anciennete_annees < 12   THEN taux_anciennete = 0.10
  WHEN anciennete_annees < 20   THEN taux_anciennete = 0.15
  WHEN anciennete_annees >= 20  THEN taux_anciennete = 0.20
  prime_anciennete = ROUND(salaire_base × taux_anciennete, 2)
}}

MODULE[05] salaire_brut_imposable {{
  total_primes_imposables = SUM(primes_imposables[*].montant)
  salaire_brut_imposable  = salaire_base
                          + prime_anciennete
                          + total_hsupp
                          + total_primes_imposables
                          + total_ni_excedent_imposable
}}

MODULE[06] cnss {{
  base_cnss_rg     = MIN(salaire_brut_imposable, 6000.00)
  cnss_rg_sal      = ROUND(base_cnss_rg × 0.0448, 2)
  cnss_amo_sal     = ROUND(salaire_brut_imposable × 0.0226, 2)
  total_cnss_sal   = cnss_rg_sal + cnss_amo_sal

  cnss_rg_pat           = ROUND(base_cnss_rg × 0.0898, 2)
  cnss_alloc_fam_pat    = ROUND(salaire_brut_imposable × 0.0640, 2)
  cnss_fp_pat           = ROUND(salaire_brut_imposable × 0.0160, 2)
  cnss_amo_pat          = ROUND(salaire_brut_imposable × 0.0226, 2)
  cnss_particip_amo_pat = ROUND(salaire_brut_imposable × 0.0185, 2)
  total_cnss_pat = cnss_rg_pat + cnss_alloc_fam_pat + cnss_fp_pat
                 + cnss_amo_pat + cnss_particip_amo_pat
}}

MODULE[07] cimr {{
  WHEN regime = AUCUN        : cimr_sal = 0 ; cimr_pat = 0
  WHEN regime = AL_KAMIL     : base = salaire_brut_imposable
  WHEN regime = AL_MOUNASSIB : base = MAX(0, salaire_brut_imposable - 6000)
  cimr_sal = ROUND(base × taux_salarial, 2)
  cimr_pat = ROUND(base × taux_patronal, 2)
}}

MODULE[08] frais_professionnels {{
  ;; BASE FP = brut_imposable COMPLET — jamais brut - cnss
  WHEN salaire_brut_imposable <= 6500 : taux_fp = 0.35 ; plafond_fp = 2916.67
  WHEN salaire_brut_imposable >  6500 : taux_fp = 0.25 ; plafond_fp = 2916.67
  montant_fp = MIN(ROUND(salaire_brut_imposable × taux_fp, 2), plafond_fp)
}}

MODULE[09] base_ir {{
  RNI = salaire_brut_imposable
      - total_cnss_sal
      - cimr_sal
      - mutuelle_salariale
      - montant_fp
      - interet_pret_logement
  RNI = MAX(0, RNI)
  SELF_CHECK: ASSERT RNI < (salaire_brut_imposable - montant_fp) si total_cnss_sal > 0
}}

MODULE[10] ir {{
  ;; Barème mensuel 2026
  WHEN RNI <= 3333.33  : taux_ir = 0.00  ; ded_bareme =    0.00
  WHEN RNI <= 5000.00  : taux_ir = 0.10  ; ded_bareme =  333.33
  WHEN RNI <= 6666.67  : taux_ir = 0.20  ; ded_bareme =  833.33
  WHEN RNI <= 8333.33  : taux_ir = 0.30  ; ded_bareme = 1500.00
  WHEN RNI <= 15000.00 : taux_ir = 0.34  ; ded_bareme = 1833.33
  WHEN RNI >  15000.00 : taux_ir = 0.37  ; ded_bareme = 2283.33
  ir_brut         = ROUND(RNI × taux_ir, 2)
  ded_famille     = nb_personnes_charge × 30.00
  ir_final        = MAX(0, ROUND(ir_brut - ded_bareme - ded_famille, 2))
}}

MODULE[11] net_a_payer {{
  total_retenues = total_cnss_sal + cimr_sal + mutuelle_salariale + ir_final
  salaire_net    = salaire_brut_imposable - total_retenues + total_ni_exonere
}}

MODULE[12] cout_employeur {{
  total_charges_pat = total_cnss_pat + cimr_pat + mutuelle_patronale
  cout_employeur    = salaire_brut_imposable + total_charges_pat + total_ni_exonere
}}

@ANTIPATTERNS {{
  ? fp_base = brut - cnss        ? ? fp_base = brut_imposable
  ? montant_fp = 2916.67 forfait ? ? MIN(brut × taux, 2916.67)
  ? RNI = brut - fp seul         ? ? RNI = brut - cnss - cimr - mutuelle - fp
  ? cnss_rg = brut × 4.48%       ? ? MIN(brut, 6000) × 4.48%
}}

@EXAMPLE_REFERENCE {{
  ;; Cas test — à utiliser pour auto-vérification avant de répondre
  salaire_base = 9000 MAD, ancienneté = 5 ans, 26j, 0 charge, sans CIMR, sans NI
  ? brut_imposable  = 9900.00
  ? total_cnss_sal  = 492.54   (268.80 + 223.74)
  ? montant_fp      = 2475.00  (9900 × 25% = 2475 < 2916.67 ? NON plafonné)
  ? RNI             = 6932.46  (9900 - 492.54 - 2475)
  ? IR              = 579.74
  ? salaire_net     = 8827.72
  ? cout_employeur  = 11637.69
}}
</payzen_dsl>

INSTRUCTIONS DE SORTIE :
- Réponds UNIQUEMENT avec un objet JSON valide.
- Aucun texte avant, aucun texte après.
- Aucune balise markdown, aucun ```json.
- Tous les montants sont en MAD, arrondis à 2 décimales.
- Avant de répondre, vérifie chaque formule avec l'@EXAMPLE_REFERENCE comme référence croisée.";
        }


        /// <summary>
        /// Construit le prompt utilisateur avec l'instruction de simulation
        /// </summary>
        private string BuildUserPrompt(string instruction)
        {
            return $@"Voici ma demande de simulation de paie :

{instruction}

IMPORTANT : 
- Si la demande est claire et contient un montant de salaire net souhaité : Propose EXACTEMENT 3 FORMULES DIFFÉRENTES avec des stratégies distinctes
- Si la demande n'est PAS CLAIRE ou ne contient PAS de montant net : Retourne un JSON avec un champ ""error"" expliquant ce qui manque
- Le salaire net à payer doit correspondre à ma demande avec plus ou moins 5% d'écart maximum
- Réponds UNIQUEMENT avec du JSON valide (pas de texte avant/après, pas de markdown)
- ?? CRITIQUE : Le JSON DOIT être COMPLET avec TOUS les 3 scénarios et toutes les accolades fermées
- Utilise la structure JSON spécifiée dans les instructions système pour les scénarios
- Pour les erreurs, utilise ce format : {{""error"": ""titre"", ""message"": ""détails"", ""instructions"": ""aide""}}
- Tous les montants doivent être arrondis à 2 décimales
- Inclus tous les éléments de paie : base, primes, déductions (CNSS, AMO, IR, etc.)
- ?????? IMPÉRATIF NOMS DE PROPRIÉTÉS : Utilise EXACTEMENT snake_case (avec underscores) :
  * ""brut_imposable"" (PAS brutImposable)
  * ""total_retenues"" (PAS totalRetenues)
  * ""cout_employeur"" (PAS coutEmployeur)
  * ""salaire_net"" (PAS salaireNet)
  * ""calcul_steps"" (PAS calculSteps)
- Chaque scénario DOIT contenir tous ces champs numériques
- Calcule le coût total employeur avec les charges patronales
- Liste les avantages et inconvénients de chaque formule
- Si tu approches de la limite de tokens, simplifie les descriptions mais GARDE LE JSON VALIDE ET COMPLET

???? VÉRIFICATION FINALE OBLIGATOIRE avant de répondre :
Pour CHAQUE scénario, vérifie que :
  salaire_net = brut_imposable - total_retenues + somme_des_indemnités_ni

Si cette équation n'est pas respectée, CORRIGE le salaire_net avant d'envoyer la réponse.";
        }

        /// <summary>
        /// Simule des compositions de salaire avec HTTP (non-streaming)
        /// </summary>
        public async Task<string> SimulationSalaryStreamAsync(
            string regleContent,
            string instruction,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("?? Démarrage de la simulation de paie avec Gemini HTTP");

            // Construction des prompts
            var systemPrompt = BuildSystemPrompt(regleContent);
            var userPrompt = BuildUserPrompt(instruction);

            // Combinaison des prompts pour Gemini
            var fullPrompt = $"{systemPrompt}\n\n{userPrompt}";

            // Préparation de la requête pour Gemini API
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = fullPrompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 8192,
                    responseMimeType = "application/json"
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            _logger.LogDebug("Envoi de la requête HTTP à l'API Gemini");
            _logger.LogInformation("?? Paramètres de la requête - Model: {Model}, MaxTokens: {MaxTokens}", GEMINI_MODEL, 8192);

            // Appel HTTP standard à l'API Gemini
            var url = $"{GEMINI_API_BASE}/{GEMINI_MODEL}:generateContent?key={_apiKey}";
            var httpResponse = await _httpClient.PostAsync(url, content, cancellationToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Erreur API Gemini: {StatusCode} - {Error}", httpResponse.StatusCode, errorContent);
                throw new InvalidOperationException($"Erreur API Gemini: {httpResponse.StatusCode}");
            }

            var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            
            // Parser la réponse Gemini
            using var geminiResponse = JsonDocument.Parse(responseContent);
            var responseText = geminiResponse.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? throw new InvalidOperationException("Réponse Gemini sans contenu texte.");

            _logger.LogInformation("?? Réponse brute reçue - Longueur: {Length} caractères", responseText.Length);
            _logger.LogDebug("?? Réponse brute complète:\n{Response}", responseText);

            // Nettoyer la réponse JSON
            var cleanedResponse = CleanJsonResponse(responseText);
            _logger.LogInformation("?? JSON nettoyé - Longueur: {Length} caractères", cleanedResponse.Length);
            _logger.LogDebug("?? JSON nettoyé:\n{CleanedResponse}", cleanedResponse);

            // Validation du format JSON
            try
            {
                using var jsonDoc = JsonDocument.Parse(cleanedResponse);
                _logger.LogInformation("? JSON valide parsé avec succès");
                return cleanedResponse;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "? ERREUR JSON INVALIDE");
                _logger.LogError("?? Position erreur: Ligne {Line}, Colonne {Column}", jsonEx.LineNumber, jsonEx.BytePositionInLine);
                _logger.LogError("?? Premiers 1000 caractères du JSON:\n{JsonStart}", cleanedResponse.Substring(0, Math.Min(1000, cleanedResponse.Length)));
                _logger.LogError("?? Derniers 500 caractères du JSON:\n{JsonEnd}", cleanedResponse.Length > 500 ? cleanedResponse.Substring(cleanedResponse.Length - 500) : cleanedResponse);
                
                throw new InvalidOperationException(
                    $"JSON invalide retourné par l'API. Erreur: {jsonEx.Message}\n" +
                    $"Position: Ligne {jsonEx.LineNumber}, Colonne {jsonEx.BytePositionInLine}\n" +
                    $"Consultez les logs pour voir le JSON complet.",
                    jsonEx);
            }
        }
    }
}
