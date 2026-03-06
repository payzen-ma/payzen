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
                _logger.LogInformation("🤖 Nouvelle demande de simulation de paie reçue");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Requête de simulation invalide");
                    return BadRequest(ModelState);
                }

                var result = await _claudeService.SimulationSalaryAsync(
                    request.RegleContent,
                    request.Instruction,
                    cancellationToken);

                _logger.LogInformation("✅ Simulation de paie réussie");

                return Ok(new SimulationResponse
                {
                    Success = true,
                    Result = result,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ Demande utilisateur non claire : {Message}", ex.Message);
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
                _logger.LogInformation("🚀 Nouvelle demande de simulation rapide de paie");

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Requête de simulation rapide invalide");
                    return BadRequest(ModelState);
                }

                // Lecture du fichier de règles DSL compact
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

                var result = await _claudeService.SimulationSalaryAsync(
                    regleContent,
                    request.Instruction,
                    cancellationToken);

                _logger.LogInformation("✅ Simulation rapide de paie réussie");

                return Ok(new SimulationResponse
                {
                    Success = true,
                    Result = result,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ Demande utilisateur non claire : {Message}", ex.Message);
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
        /// Simule des compositions de salaire avec streaming (Server-Sent Events)
        /// POST: api/claudesimulation/simulate-stream
        /// </summary>
        /// <param name="request">Requête contenant l'instruction utilisateur</param>
        /// <param name="cancellationToken">Jeton d'annulation</param>
        /// <returns>Stream de texte (Server-Sent Events)</returns>
        [HttpPost("simulate-stream")]
        public async Task SimulateSalaryStream(
            [FromBody] QuickSimulationRequest request,
            CancellationToken cancellationToken)
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            try
            {
                _logger.LogInformation("🚀 Démarrage simulation streaming");

                if (!ModelState.IsValid)
                {
                    await Response.WriteAsync($"data: {{\"error\": \"Requête invalide\"}}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                    return;
                }

                // Lecture du fichier de règles DSL
                var rulesFilePath = Path.Combine(_environment.ContentRootPath, "rules", "regles_paie_compact.txt");

                if (!System.IO.File.Exists(rulesFilePath))
                {
                    await Response.WriteAsync($"data: {{\"error\": \"Fichier de règles introuvable\"}}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                    return;
                }

                var regleContent = await System.IO.File.ReadAllTextAsync(rulesFilePath, cancellationToken);

                // Stream de la réponse
                await foreach (var chunk in _claudeService.SimulationSalaryStreamAsync(
                    regleContent,
                    request.Instruction,
                    cancellationToken))
                {
                    // Format Server-Sent Events
                    var data = $"data: {JsonSerializer.Serialize(new { chunk })}\n\n";
                    await Response.WriteAsync(data, cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }

                // Signal de fin
                await Response.WriteAsync("data: {\"done\": true}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                _logger.LogInformation("✅ Streaming terminé");
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("⚠️ Erreur utilisateur : {Message}", ex.Message);
                var errorData = $"data: {{\"error\": {JsonSerializer.Serialize(ex.Message)}}}\n\n";
                await Response.WriteAsync(errorData, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur streaming");
                var errorData = $"data: {{\"error\": \"Erreur lors du streaming\"}}\n\n";
                await Response.WriteAsync(errorData, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
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
        public string? Result { get; set; }

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