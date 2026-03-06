using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;

namespace payzen_backend.Services.Llm
{
    public class ClaudeSimulationService : IClaudeSimulationService
    {
        private readonly AnthropicClient _client;
        private readonly ILogger<ClaudeSimulationService> _logger;

        public ClaudeSimulationService(
            IConfiguration config,
            ILogger<ClaudeSimulationService> logger)
        {
            var apikey = config["Anthropic:ApiKey"] ?? 
                throw new InvalidOperationException("Anthropic:ApiKey non configuré");
            _client = new AnthropicClient() { ApiKey = apikey };
            _logger = logger;
        }

        /// <summary>
        /// Envoie une requête à l'API d'Anthropic pour simuler des éléments de paie selon les règles fournies
        /// par le DSL
        /// </summary> 
        public async Task<string> SimulationSalaryAsync(
            string regleContent,
            string instruction,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Démarrage de la simulation de paie avec Claude");

                // Construction du prompt système avec les règles DSL
                var systemPrompt = BuildSystemPrompt(regleContent);

                // Construction du prompt utilisateur
                var userPrompt = BuildUserPrompt(instruction);

                // Création de la requête vers l'API Claude
                var parameters = new MessageCreateParams
                {
                    Model = "claude-haiku-4-5-20251001",
                    MaxTokens = 8192, // Augmenté pour supporter 3 scénarios complets
                    System = new MessageCreateParamsSystem(
                        new[]
                        {
                            new TextBlockParam
                            {
                                Text = systemPrompt,
                                CacheControl = new CacheControlEphemeral() // Active le caching pour optimiser les coûts
                            }
                        }
                    ),
                    Messages =
                    [
                        new()
                        {
                            Role = Role.User,
                            Content = userPrompt
                        }
                    ]
                };

                _logger.LogDebug("Envoi de la requête à l'API Anthropic");

                // Appel à l'API Claude
                var response = await _client.Messages.Create(parameters, cancellationToken);

                if (response?.Content == null || response.Content.Count == 0)
                {
                    _logger.LogWarning("Réponse vide reçue de l'API Claude");
                    return "Aucune réponse générée par le modèle.";
                }

                // Extraction du texte de la réponse
                TextBlock? textBlock = null;
                foreach (var contentBlock in response.Content)
                {
                    if (contentBlock.TryPickText(out var tb) && tb != null)
                    {
                        textBlock = tb;
                        break;
                    }
                }

                var responseText = textBlock?.Text 
                    ?? throw new InvalidOperationException("Réponse Claude sans contenu texte.");

                _logger.LogDebug("Longueur de la réponse : {Length} caractères", responseText.Length);

                // Validation du format JSON
                try
                {
                    // Nettoyer la réponse (supprimer les backticks markdown si présents)
                    var cleanedResponse = CleanJsonResponse(responseText);
                    
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
                        
                        _logger.LogWarning("⚠️ Demande utilisateur non claire - Le LLM demande des précisions : {Error}", 
                            detailedMessage);
                        
                        // ArgumentException sera transformé en BadRequest par le contrôleur
                        throw new ArgumentException(detailedMessage);
                    }
                    
                    // Vérifier que la réponse contient bien les scénarios attendus
                    if (!root.TryGetProperty("scenarios", out var scenariosProp))
                    {
                        _logger.LogWarning("⚠️ Réponse JSON valide mais sans champ 'scenarios' : {Response}", cleanedResponse);
                        
                        throw new InvalidOperationException(
                            $"Le LLM a retourné un JSON valide mais il manque le champ 'scenarios'.\n\n" +
                            $"Réponse reçue : {cleanedResponse.Substring(0, Math.Min(300, cleanedResponse.Length))}...");
                    }
                    
                    _logger.LogInformation("✅ Simulation de paie terminée avec succès - JSON valide avec {Count} scénarios", 
                        scenariosProp.GetArrayLength());
                    return cleanedResponse;
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "❌ ERREUR JSON INVALIDE - Réponse brute du LLM :\n{Response}", responseText);
                    
                    // Vérifier si le JSON est simplement incomplet (coupé par MaxTokens)
                    var errorMsg = $"Le LLM a retourné un JSON invalide. Erreur de parsing : {jsonEx.Message}\n\n" +
                        $"Position de l'erreur : Ligne {jsonEx.LineNumber}, Colonne {jsonEx.BytePositionInLine}\n\n";
                    
                    if (jsonEx.Message.Contains("end of data") || jsonEx.Message.Contains("incomplete"))
                    {
                        errorMsg += $"⚠️ Le JSON semble incomplet (probablement coupé par la limite de tokens).\n" +
                            $"Longueur de la réponse : {responseText.Length} caractères\n\n";
                    }
                    
                    errorMsg += $"Réponse brute reçue (premiers 800 caractères) :\n{responseText.Substring(0, Math.Min(800, responseText.Length))}...";
                    
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
                return response;

            // Supprimer les blocs markdown code (```json ... ``` ou ``` ... ```)
            var cleaned = response.Trim();
            
            if (cleaned.StartsWith("```"))
            {
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
            }
            
            return cleaned;
        }

        /// <summary>
        /// Construit le prompt système contenant les règles de calcul de paie
        /// </summary>
        private string BuildSystemPrompt(string regleContent)
        {
            return $@"Tu es un expert en paie marocaine et en simulation de salaires. 
Tu as accès aux règles de calcul suivantes définies dans notre système :

{regleContent}

INSTRUCTIONS CRITIQUES :
1. Analyse la demande de l'utilisateur (généralement un salaire net souhaité)
2. Le demandeur est toujours un RH qui veux optimiser la paie pour l'entreprise, ne donne pas un montant superiour a la demande de plus de 5%
3. Propose EXACTEMENT 3 FORMULES DIFFÉRENTES pour atteindre cet objectif
4. Chaque formule doit utiliser une stratégie distincte :
   - Formule 1 : Approche équilibrée (mix salaire base + indemnités standard)
   - Formule 2 : Salaire de base maximal (moins d'indemnités)
   - Formule 3 : Optimisation fiscale (plus d'indemnités exonérées)
5. Calcule précisément tous les éléments : brut, cotisations, IR, net
6. Explique les avantages et inconvénients de chaque formule
7. Assure-toi que les calculs respectent la législation marocaine définie dans les règles de calcul Payzen DSL
8. Assume que l'employé a une ancienneté de 0 (nouveau employé) sauf si spécifié dans la demande
9. La prime de représentation est Uniquement pour employee gérent.
10. Si la demande contient : Merci, Thank you, Thanks sans une demande valable, retourn JSON avec Ravi de vous service ou un message du genre

FORMAT DE RÉPONSE OBLIGATOIRE - JSON UNIQUEMENT :
⚠️ CRITIQUE : Réponds UNIQUEMENT avec un objet JSON COMPLET et VALIDE.
- PAS de markdown (pas de ```json)
- PAS de texte avant ou après le JSON
- Le JSON DOIT être COMPLET avec les 3 scénarios
- Vérifie que toutes les accolades {{}} et crochets [] sont bien fermés
- Si tu arrives à la limite de tokens, réduis les descriptions mais garde le JSON valide

Structure JSON attendue (DOIT inclure EXACTEMENT 3 scénarios) :

{{
  ""scenarios"": [
    {{
      ""titre"": ""Approche équilibrée"",
      ""description"": ""Mix salaire base et indemnités standard"",
      ""elements"": [
        {{ ""nom"": ""Salaire de base"", ""type"": ""base"", ""montant"": 8000.00 }},
        {{ ""nom"": ""Prime de transport"", ""type"": ""prime"", ""montant"": 500.00 }},
        {{ ""nom"": ""CNSS salariale"", ""type"": ""deduction"", ""montant"": -340.00 }},
        {{ ""nom"": ""AMO"", ""type"": ""deduction"", ""montant"": -170.00 }},
        {{ ""nom"": ""IR"", ""type"": ""deduction"", ""montant"": -650.00 }}
      ],
      ""brut_imposable"": 8500.00,
      ""total_retenues"": 1160.00,
      ""cout_employeur"": 10200.00,
      ""salaire_net"": 7340.00,
      ""calcul_steps"": [
        {{ ""label"": ""Salaire brut"", ""value"": ""8 500.00 DH"" }},
        {{ ""label"": ""CNSS (4%)"", ""value"": ""− 340.00 DH"" }},
        {{ ""label"": ""AMO (2%)"", ""value"": ""− 170.00 DH"" }},
        {{ ""label"": ""IR"", ""value"": ""− 650.00 DH"" }},
        {{ ""label"": ""Salaire net"", ""value"": ""7 340.00 DH"" }}
      ],
      ""avantages"": [""Équilibre entre coûts et avantages"", ""Structure classique""],
      ""inconvenients"": [""Optimisation fiscale limitée""]
    }}
  ]
}}

TYPES d'éléments autorisés : 'base', 'prime', 'deduction', 'avantage', 'ni' (non imposable)";
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
- Le salaire net à payer doit correspondre à ma demande
- Réponds UNIQUEMENT avec du JSON valide (pas de texte avant/après, pas de markdown)
- ⚠️ CRITIQUE : Le JSON DOIT être COMPLET avec TOUS les 3 scénarios et toutes les accolades fermées
- Utilise la structure JSON spécifiée dans les instructions système pour les scénarios
- Pour les erreurs, utilise ce format : {{""error"": ""titre"", ""message"": ""détails"", ""instructions"": ""aide""}}
- Tous les montants doivent être arrondis à 2 décimales
- Inclus tous les éléments de paie : base, primes, déductions (CNSS, AMO, IR, etc.)
- Calcule le coût total employeur avec les charges patronales
- Liste les avantages et inconvénients de chaque formule
- Si tu approches de la limite de tokens, simplifie les descriptions mais GARDE LE JSON VALIDE ET COMPLET";
        }

        /// <summary>
        /// Simule des compositions de salaire avec streaming de la réponse
        /// </summary>
        public async IAsyncEnumerable<string> SimulationSalaryStreamAsync(
            string regleContent,
            string instruction,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("🚀 Démarrage de la simulation de paie avec streaming");

            // Construction des prompts
            var systemPrompt = BuildSystemPrompt(regleContent);
            var userPrompt = BuildUserPrompt(instruction);

            // Création de la requête vers l'API Claude avec streaming
            var parameters = new MessageCreateParams
            {
                Model = "claude-haiku-4-5-20251001",
                MaxTokens = 8192,
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
                Messages =
                [
                    new MessageParam
                    {
                        Role = Role.User,
                        Content = userPrompt
                    }
                ]
            };

            _logger.LogDebug("Envoi de la requête streaming à l'API Anthropic");

            // Stream de la réponse avec CreateStreaming
            var stream = _client.Messages.CreateStreaming(parameters, cancellationToken);

            await foreach (var streamEvent in stream)
            {
                if (streamEvent.TryPickContentBlockDelta(out var deltaEvent)
                    && deltaEvent.Delta.TryPickText(out var textDelta))
                {
                    // Envoyer chaque morceau de texte au fur et à mesure
                    yield return textDelta.Text;
                }
            }

            _logger.LogInformation("✅ Streaming de simulation terminé");
        }
    }
}
