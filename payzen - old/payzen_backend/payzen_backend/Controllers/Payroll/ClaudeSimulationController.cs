using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using payzen_backend.Services.Llm;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace payzen_backend.Controllers.Payroll
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClaudeSimulationController : ControllerBase
    {
        private readonly IClaudeSimulationService _claudeService;
        private readonly ILogger<ClaudeSimulationController> _logger;
        private readonly IWebHostEnvironment _environment;

        public ClaudeSimulationController(
            IClaudeSimulationService claudeService,
            ILogger<ClaudeSimulationController> logger,
            IWebHostEnvironment environment)
        {
            _claudeService = claudeService;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Simule des compositions de salaire selon les règles de paie et l'instruction fournie
        /// POST: api/claudesimulation/simulate
        /// </summary>
        /// <param name="request">Requête contenant les règles DSL et l'instruction utilisateur</param>
        /// <param name="cancellationToken">Jeton d'annulation</param>
        /// <returns>Scénarios de paie proposés par Claude</returns>
        [HttpPost("simulate")]
        [ProducesResponseType(typeof(SimulationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SimulateSalary(
            [FromBody] SimulationRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("?? Nouvelle demande de simulation de paie reçue");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Requête de simulation invalide");
                    return BadRequest(ModelState);
                }

                var result = await _claudeService.SimulationSalaryAsync(
                    request.RegleContent,
                    request.Instruction,
                    cancellationToken);

                _logger.LogInformation("? Simulation de paie réussie");

                // Désérialiser le JSON pour éviter le double encodage
                var jsonResult = JsonSerializer.Deserialize<object>(result);
                _logger.LogInformation("?? [AVANT normalisation] JSON désérialisé: {Json}", JsonSerializer.Serialize(jsonResult, new JsonSerializerOptions { WriteIndented = false }));
                
                // Convertir les clés camelCase en snake_case pour compatibilité frontend
                var normalizedResult = NormalizeCamelCaseToSnakeCase(jsonResult);
                _logger.LogInformation("? [APRÈS normalisation] JSON normalisé: {Json}", JsonSerializer.Serialize(normalizedResult, new JsonSerializerOptions { WriteIndented = false }));

                return Ok(new SimulationResponse
                {
                    Success = true,
                    Result = normalizedResult,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("?? Demande utilisateur non claire : {Message}", ex.Message);
                return BadRequest(new SimulationResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Erreur métier lors de la simulation");
                return StatusCode(500, new SimulationResponse
                {
                    Success = false,
                    ErrorMessage = $"Erreur de simulation : {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erreur de parsing JSON");
                return StatusCode(500, new SimulationResponse
                {
                    Success = false,
                    ErrorMessage = $"Le LLM a retourné un JSON invalide : {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la simulation de paie");
                
                var detailedMessage = _environment.IsDevelopment() 
                    ? $"Erreur : {ex.Message}\n\nType : {ex.GetType().Name}"
                    : "Une erreur inattendue s'est produite.";
                
                return StatusCode(500, new SimulationResponse
                {
                    Success = false,
                    ErrorMessage = detailedMessage,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Simule des compositions de salaire avec les règles du fichier DSL compact
        /// POST: api/claudesimulation/simulate-quick
        /// </summary>
        /// <param name="request">Requête contenant uniquement l'instruction utilisateur</param>
        /// <param name="cancellationToken">Jeton d'annulation</param>
        /// <returns>Scénarios de paie proposés par Claude avec règles DSL du système</returns>
        [HttpPost("simulate-quick")]
        [ProducesResponseType(typeof(SimulationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SimulateSalaryQuick(
            [FromBody] QuickSimulationRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("?? Nouvelle demande de simulation rapide de paie");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Requête de simulation rapide invalide");
                    return BadRequest(ModelState);
                }

                // Lecture du fichier de règles DSL compact
                var rulesFilePath = Path.Combine(_environment.ContentRootPath, "rules", "regle_simulateur.md");

                if (!System.IO.File.Exists(rulesFilePath))
                {
                    _logger.LogError("Fichier de règles DSL introuvable : {Path}", rulesFilePath);
                    return StatusCode(500, new SimulationResponse
                    {
                        Success = false,
                        ErrorMessage = "Le fichier de règles de paie est introuvable sur le serveur.",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var regleContent = await System.IO.File.ReadAllTextAsync(rulesFilePath, cancellationToken);

                var result = await _claudeService.SimulationSalaryAsync(
                    regleContent,
                    request.Instruction,
                    cancellationToken);

                _logger.LogInformation("? Simulation rapide de paie réussie");

                // Désérialiser le JSON pour éviter le double encodage
                var jsonResult = JsonSerializer.Deserialize<object>(result);
                _logger.LogInformation("?? [AVANT normalisation] JSON désérialisé: {Json}", JsonSerializer.Serialize(jsonResult, new JsonSerializerOptions { WriteIndented = false }));
                
                // Convertir les clés camelCase en snake_case pour compatibilité frontend
                var normalizedResult = NormalizeCamelCaseToSnakeCase(jsonResult);
                _logger.LogInformation("? [APRÈS normalisation] JSON normalisé: {Json}", JsonSerializer.Serialize(normalizedResult, new JsonSerializerOptions { WriteIndented = false }));

                return Ok(new SimulationResponse
                {
                    Success = true,
                    Result = normalizedResult,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("?? Demande utilisateur non claire : {Message}", ex.Message);
                return BadRequest(new SimulationResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Erreur opérationnelle lors de la simulation");
                return StatusCode(500, new SimulationResponse
                {
                    Success = false,
                    ErrorMessage = $"Erreur de simulation : {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erreur de parsing JSON");
                return StatusCode(500, new SimulationResponse
                {
                    Success = false,
                    ErrorMessage = $"Le LLM a retourné un JSON invalide : {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la simulation rapide de paie");
                
                // En développement, retourner plus de détails
                var detailedMessage = _environment.IsDevelopment() 
                    ? $"Erreur : {ex.Message}\n\nType : {ex.GetType().Name}\n\nStack : {ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0))}"
                    : "Une erreur inattendue s'est produite lors de la simulation.";
                
                return StatusCode(500, new SimulationResponse
                {
                    Success = false,
                    ErrorMessage = detailedMessage,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Simule des compositions de salaire avec HTTP (anciennement streaming)
        /// POST: api/claudesimulation/simulate-stream
        /// </summary>
        /// <param name="request">Requête contenant l'instruction utilisateur</param>
        /// <param name="cancellationToken">Jeton d'annulation</param>
        /// <returns>Réponse HTTP standard avec les scénarios de paie</returns>
        [HttpPost("simulate-stream")]
        [ProducesResponseType(typeof(SimulationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SimulateSalaryStream(
            [FromBody] QuickSimulationRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("?? Démarrage simulation HTTP");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Requête de simulation invalide");
                    return BadRequest(ModelState);
                }

                // Lecture du fichier de règles DSL
                var rulesFilePath = Path.Combine(_environment.ContentRootPath, "rules", "regles_paie_compact.txt");

                if (!System.IO.File.Exists(rulesFilePath))
                {
                    _logger.LogError("Fichier de règles DSL introuvable : {Path}", rulesFilePath);
                    return StatusCode(500, new SimulationResponse
                    {
                        Success = false,
                        ErrorMessage = "Le fichier de règles de paie est introuvable sur le serveur.",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var regleContent = await System.IO.File.ReadAllTextAsync(rulesFilePath, cancellationToken);

                // Appel HTTP standard
                var result = await _claudeService.SimulationSalaryStreamAsync(
                    regleContent,
                    request.Instruction,
                    cancellationToken);

                _logger.LogInformation("? Simulation HTTP terminée");

                // Désérialiser le JSON pour éviter le double encodage
                var jsonResult = JsonSerializer.Deserialize<object>(result);
                _logger.LogInformation("?? [AVANT normalisation] JSON désérialisé: {Json}", JsonSerializer.Serialize(jsonResult, new JsonSerializerOptions { WriteIndented = false }));
                
                // Convertir les clés camelCase en snake_case pour compatibilité frontend
                var normalizedResult = NormalizeCamelCaseToSnakeCase(jsonResult);
                _logger.LogInformation("? [APRÈS normalisation] JSON normalisé: {Json}", JsonSerializer.Serialize(normalizedResult, new JsonSerializerOptions { WriteIndented = false }));

                return Ok(new SimulationResponse
                {
                    Success = true,
                    Result = normalizedResult,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("?? Demande utilisateur non claire : {Message}", ex.Message);
                return BadRequest(new SimulationResponse
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Erreur opérationnelle lors de la simulation");
                return StatusCode(500, new SimulationResponse
                {
                    Success = false,
                    ErrorMessage = $"Erreur de simulation : {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erreur de parsing JSON");
                return StatusCode(500, new SimulationResponse
                {
                    Success = false,
                    ErrorMessage = $"Le LLM a retourné un JSON invalide : {ex.Message}",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur HTTP simulation");
                
                var detailedMessage = _environment.IsDevelopment() 
                    ? $"Erreur : {ex.Message}\n\nType : {ex.GetType().Name}"
                    : "Une erreur inattendue s'est produite.";
                
                return StatusCode(500, new SimulationResponse
                {
                    Success = false,
                    ErrorMessage = detailedMessage,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Récupère le contenu du fichier de règles DSL compact
        /// GET: api/claudesimulation/rules
        /// </summary>
        /// <returns>Contenu du fichier de règles DSL</returns>
        [HttpGet("rules")]
        [ProducesResponseType(typeof(RulesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPayrollRules(CancellationToken cancellationToken)
        {
            try
            {
                var rulesFilePath = Path.Combine(_environment.ContentRootPath, "rules", "regles_paie_compact.txt");

                if (!System.IO.File.Exists(rulesFilePath))
                {
                    _logger.LogWarning("Fichier de règles DSL introuvable");
                    return NotFound(new { message = "Le fichier de règles de paie est introuvable." });
                }

                var content = await System.IO.File.ReadAllTextAsync(rulesFilePath, cancellationToken);

                return Ok(new RulesResponse
                {
                    Success = true,
                    Content = content,
                    FilePath = "rules/regles_paie_compact.txt",
                    LastModified = System.IO.File.GetLastWriteTimeUtc(rulesFilePath)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture du fichier de règles");
                return StatusCode(500, new { message = "Erreur lors de la lecture du fichier de règles." });
            }
        }

        /// <summary>
        /// Convertit récursivement les clés d'un objet JSON de camelCase vers snake_case
        /// pour compatibilité avec le frontend Angular
        /// </summary>
        private object? NormalizeCamelCaseToSnakeCase(object? obj)
        {
            if (obj == null)
                return null;

            if (obj is JsonElement jsonElement)
            {
                return ConvertJsonElement(jsonElement);
            }

            return obj;
        }

        private object? ConvertJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var property in element.EnumerateObject())
                    {
                        var snakeCaseKey = ToSnakeCase(property.Name);
                        
                        // Mapping spécifique pour Gemini -> Frontend
                        var finalKey = snakeCaseKey switch
                        {
                            "total_retenues_salariales" => "total_retenues",
                            "cout_employeur_total" => "cout_employeur",
                            _ => snakeCaseKey
                        };
                        
                        dict[finalKey] = ConvertJsonElement(property.Value);
                    }
                    return dict;

                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(ConvertJsonElement(item));
                    }
                    return list;

                case JsonValueKind.String:
                    return element.GetString();

                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    return element.GetDouble();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                    return null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Convertit une chaîne camelCase en snake_case
        /// Exemples: brutImposable -> brut_imposable, salaireNet -> salaire_net
        /// </summary>
        private string ToSnakeCase(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            var result = new System.Text.StringBuilder();
            result.Append(char.ToLower(str[0]));

            for (int i = 1; i < str.Length; i++)
            {
                if (char.IsUpper(str[i]))
                {
                    result.Append('_');
                    result.Append(char.ToLower(str[i]));
                }
                else
                {
                    result.Append(str[i]);
                }
            }

            return result.ToString();
        }
    }

    #region DTOs

    /// <summary>
    /// Requête de simulation de paie avec règles personnalisées
    /// </summary>
    public class SimulationRequest
    {
        /// <summary>
        /// Contenu des règles DSL de calcul de paie
        /// </summary>
        [Required(ErrorMessage = "Le contenu des règles est requis")]
        [MinLength(10, ErrorMessage = "Le contenu des règles doit contenir au moins 10 caractères")]
        public required string RegleContent { get; set; }

        /// <summary>
        /// Instruction de l'utilisateur (ex: "Je veux un net de 10000 DH")
        /// </summary>
        [Required(ErrorMessage = "L'instruction est requise")]
        [MinLength(5, ErrorMessage = "L'instruction doit contenir au moins 5 caractères")]
        [MaxLength(2000, ErrorMessage = "L'instruction ne peut pas dépasser 2000 caractères")]
        public required string Instruction { get; set; }
    }

    /// <summary>
    /// Requête de simulation rapide avec règles par défaut
    /// </summary>
    public class QuickSimulationRequest
    {
        /// <summary>
        /// Instruction de l'utilisateur
        /// </summary>
        [Required(ErrorMessage = "L'instruction est requise")]
        [MinLength(5, ErrorMessage = "L'instruction doit contenir au moins 5 caractères")]
        [MaxLength(2000, ErrorMessage = "L'instruction ne peut pas dépasser 2000 caractères")]
        public required string Instruction { get; set; }
    }

    /// <summary>
    /// Réponse de simulation de paie
    /// </summary>
    public class SimulationResponse
    {
        /// <summary>
        /// Indique si la simulation a réussi
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Résultat de la simulation (scénarios proposés par Claude)
        /// </summary>
        public object? Result { get; set; }

        /// <summary>
        /// Message d'erreur en cas d'échec
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Horodatage de la réponse
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Réponse contenant les règles de paie DSL
    /// </summary>
    public class RulesResponse
    {
        /// <summary>
        /// Indique si la récupération a réussi
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Contenu du fichier de règles DSL
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Chemin du fichier
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Date de dernière modification du fichier
        /// </summary>
        public DateTime? LastModified { get; set; }
    }

    #endregion
}
