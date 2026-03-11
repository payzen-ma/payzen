using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.DTOs.Payroll.Referentiel;
using payzen_backend.Extensions;
using payzen_backend.Models.Payroll.Referentiel;

namespace payzen_backend.Controllers.v1.Payroll.Referentiel
{
    /// <summary>
    /// API Controller for Legal Parameters (SMIG, SMAG, etc.)
    /// </summary>
    [Route("api/v{version:apiVersion}/payroll/legal-parameters")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class LegalParametersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LegalParametersController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Get all legal parameters
        /// GET /api/payroll/legal-parameters?includeInactive=false&asOfDate=2025-01-29
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<LegalParameterDto>>> GetAll(
            [FromQuery] bool includeInactive = false,
            [FromQuery] DateOnly? asOfDate = null)
        {
            var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var query = _db.LegalParameters.AsNoTracking().AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(p =>
                    p.DeletedAt == null &&
                    p.EffectiveFrom <= checkDate &&
                    (p.EffectiveTo == null || p.EffectiveTo >= checkDate));
            }

            var items = await query
                .OrderBy(p => p.Label)
                .ThenByDescending(p => p.EffectiveFrom)
                .Select(p => new LegalParameterDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Label,
                    Description = p.Source,
                    Source = p.Source,
                    Value = p.Value,
                    Unit = p.Unit,
                    EffectiveFrom = p.EffectiveFrom,
                    EffectiveTo = p.EffectiveTo,
                    IsActive = p.DeletedAt == null
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// Check if a legal parameter is used in active rule formulas and by which elements.
        /// GET /api/payroll/legal-parameters/1/usage
        /// Must be declared before GetById so that ".../1/usage" matches here and not as id=1.
        /// </summary>
        [HttpGet("{id}/usage")]
        [Produces("application/json")]
        public async Task<ActionResult<object>> GetUsage(int id)
        {
            var exists = await _db.LegalParameters.AnyAsync(p => p.Id == id);
            if (!exists)
                return NotFound(new { Message = "Legal parameter not found" });

            var usedByElements = await _db.RuleFormulas
                .Where(f => f.ParameterId == id && f.Rule.DeletedAt == null)
                .Select(f => new { ElementId = f.Rule.Element.Id, ElementName = f.Rule.Element.Name })
                .Distinct()
                .ToListAsync();

            return Ok(new
            {
                Used = usedByElements.Count > 0,
                UsedByElements = usedByElements
            });
        }

        /// <summary>
        /// Check freshness of legal parameters (identify parameters not updated in the last 6 months).
        /// Critical parameters like SMIG should be updated when legal values change.
        /// GET /api/payroll/legal-parameters/freshness-check
        /// </summary>
        [HttpGet("freshness-check")]
        [Produces("application/json")]
        public async Task<ActionResult<object>> CheckFreshness()
        {
            var sixMonthsAgo = DateTimeOffset.UtcNow.AddMonths(-6);

            // Find parameters that are currently active (no end date) and haven't been modified in 6+ months
            var staleParameters = await _db.LegalParameters
                .AsNoTracking()
                .Where(p =>
                    p.DeletedAt == null &&
                    p.EffectiveTo == null && // Still active (no end date)
                    (p.ModifiedAt == null ? p.CreatedAt : p.ModifiedAt) < sixMonthsAgo)
                .Select(p => new
                {
                    p.Id,
                    p.Code,
                    Name = p.Label,
                    p.Value,
                    p.Unit,
                    LastUpdated = p.ModifiedAt ?? p.CreatedAt,
                    p.EffectiveFrom
                })
                .OrderBy(p => p.LastUpdated)
                .ToListAsync();

            // Check specifically for SMIG/SMAG which are critical (check both Code and Name)
            var criticalStale = staleParameters
                .Where(p => p.Code.ToLower().Contains("smig") || p.Code.ToLower().Contains("smag")
                    || p.Name.ToLower().Contains("smig") || p.Name.ToLower().Contains("smag"))
                .ToList();

            return Ok(new
            {
                HasStaleParameters = staleParameters.Count > 0,
                HasCriticalStale = criticalStale.Count > 0,
                StaleParameters = staleParameters,
                CriticalStale = criticalStale,
                CheckedAt = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Get a legal parameter by ID
        /// GET /api/payroll/legal-parameters/1
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<LegalParameterDto>> GetById(int id)
        {
            var parameter = await _db.LegalParameters
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new LegalParameterDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Label,
                    Description = p.Source,
                    Source = p.Source,
                    Value = p.Value,
                    Unit = p.Unit,
                    EffectiveFrom = p.EffectiveFrom,
                    EffectiveTo = p.EffectiveTo,
                    IsActive = p.DeletedAt == null
                })
                .FirstOrDefaultAsync();

            if (parameter == null)
                return NotFound(new { Message = "Legal parameter not found" });

            return Ok(parameter);
        }

        /// <summary>
        /// Get a legal parameter by name (latest active version)
        /// GET /api/payroll/legal-parameters/by-name/SMIG%20Horaire?asOfDate=2025-01-29
        /// </summary>
        [HttpGet("by-name/{name}")]
        [Produces("application/json")]
        public async Task<ActionResult<LegalParameterDto>> GetByName(string name, [FromQuery] DateOnly? asOfDate = null)
        {
            var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var parameter = await _db.LegalParameters
                .AsNoTracking()
                .Where(p =>
                    (p.Code.ToLower() == name.ToLower() || p.Label.ToLower() == name.ToLower()) &&
                    p.DeletedAt == null &&
                    p.EffectiveFrom <= checkDate &&
                    (p.EffectiveTo == null || p.EffectiveTo >= checkDate))
                .OrderByDescending(p => p.EffectiveFrom)
                .Select(p => new LegalParameterDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Label,
                    Description = p.Source,
                    Source = p.Source,
                    Value = p.Value,
                    Unit = p.Unit,
                    EffectiveFrom = p.EffectiveFrom,
                    EffectiveTo = p.EffectiveTo,
                    IsActive = p.DeletedAt == null
                })
                .FirstOrDefaultAsync();

            if (parameter == null)
                return NotFound(new { Message = "Legal parameter not found" });

            return Ok(parameter);
        }

        /// <summary>
        /// Get all versions of a legal parameter by name
        /// GET /api/payroll/legal-parameters/by-name/SMIG%20Horaire/history
        /// </summary>
        [HttpGet("by-name/{name}/history")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<LegalParameterDto>>> GetHistory(string name)
        {
            var parameters = await _db.LegalParameters
                .AsNoTracking()
                .Where(p => p.Code.ToLower() == name.ToLower() || p.Label.ToLower() == name.ToLower())
                .OrderByDescending(p => p.EffectiveFrom)
                .Select(p => new LegalParameterDto
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Label,
                    Description = p.Source,
                    Source = p.Source,
                    Value = p.Value,
                    Unit = p.Unit,
                    EffectiveFrom = p.EffectiveFrom,
                    EffectiveTo = p.EffectiveTo,
                    IsActive = p.DeletedAt == null
                })
                .ToListAsync();

            return Ok(parameters);
        }

        /// <summary>
        /// Create a new legal parameter
        /// POST /api/payroll/legal-parameters
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<LegalParameterDto>> Create([FromBody] CreateLegalParameterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var label = dto.Name?.Trim() ?? "";
            if (string.IsNullOrEmpty(label))
                return BadRequest(new { Message = "Name is required" });

            // Auto-generate Code from Name if not provided
            var code = !string.IsNullOrWhiteSpace(dto.Code)
                ? dto.Code.Trim().ToUpperInvariant().Replace(" ", "_")
                : label.ToUpperInvariant().Replace(" ", "_");

            // Check for overlapping periods (same Code, excluding soft-deleted)
            var overlap = await _db.LegalParameters
                .AsNoTracking()
                .AnyAsync(p =>
                    p.DeletedAt == null &&
                    p.Code.ToLower() == code.ToLower() &&
                    p.EffectiveFrom <= (dto.EffectiveTo ?? DateOnly.MaxValue) &&
                    (p.EffectiveTo == null || p.EffectiveTo >= dto.EffectiveFrom));

            if (overlap)
                return Conflict(new { Message = "A legal parameter with this code already exists for the specified period" });

            var userId = User.GetUserId();

            // Validate unit/value consistency
            var unitError = ValidateUnitAndValue(code, dto.Value, dto.Unit);
            if (unitError != null)
                return BadRequest(new { Message = unitError });

            var entity = new LegalParameter
            {
                Code = code,
                Label = label,
                Source = (dto.Description ?? dto.Source)?.Trim(),
                Value = dto.Value,
                Unit = dto.Unit.Trim(),
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.LegalParameters.Add(entity);
            await _db.SaveChangesAsync();

            var result = new LegalParameterDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Label,
                Description = entity.Source,
                Source = entity.Source,
                Value = entity.Value,
                Unit = entity.Unit,
                EffectiveFrom = entity.EffectiveFrom,
                EffectiveTo = entity.EffectiveTo,
                IsActive = entity.IsActive(null)
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
        }

        /// <summary>
        /// Update a legal parameter
        /// PUT /api/payroll/legal-parameters/1
        /// </summary>
        [HttpPut("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<LegalParameterDto>> Update(int id, [FromBody] CreateLegalParameterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = await _db.LegalParameters.FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null)
                return NotFound(new { Message = "Legal parameter not found" });

            var label = dto.Name?.Trim() ?? "";
            if (string.IsNullOrEmpty(label))
                return BadRequest(new { Message = "Name is required" });

            // Check for overlapping periods (same Code, excluding current record and soft-deleted)
            var overlap = await _db.LegalParameters
                .AsNoTracking()
                .AnyAsync(p =>
                    p.DeletedAt == null &&
                    p.Id != id &&
                    p.Code.ToLower() == entity.Code.ToLower() &&
                    p.EffectiveFrom <= (dto.EffectiveTo ?? DateOnly.MaxValue) &&
                    (p.EffectiveTo == null || p.EffectiveTo >= dto.EffectiveFrom));

            if (overlap)
                return Conflict(new { Message = "Another legal parameter with this code exists for the specified period" });

            // Validate unit/value consistency
            var unitError = ValidateUnitAndValue(entity.Code, dto.Value, dto.Unit);
            if (unitError != null)
                return BadRequest(new { Message = unitError });

            // Code is immutable — only update Label and other fields
            entity.Label = label;
            entity.Source = (dto.Description ?? dto.Source)?.Trim();
            entity.Value = dto.Value;
            entity.Unit = dto.Unit.Trim();
            entity.EffectiveFrom = dto.EffectiveFrom;
            entity.EffectiveTo = dto.EffectiveTo;
            entity.ModifiedAt = DateTimeOffset.UtcNow;
            entity.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            var result = new LegalParameterDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Label,
                Description = entity.Source,
                Source = entity.Source,
                Value = entity.Value,
                Unit = entity.Unit,
                EffectiveFrom = entity.EffectiveFrom,
                EffectiveTo = entity.EffectiveTo,
                IsActive = entity.IsActive(null)
            };

            return Ok(result);
        }

        /// <summary>
        /// Soft-delete a legal parameter
        /// DELETE /api/payroll/legal-parameters/1
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.LegalParameters.FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null)
                return NotFound(new { Message = "Legal parameter not found" });

            // Safety: refuse delete if still used (e.g. direct API call or race)
            var usedByElements = await _db.RuleFormulas
                .Where(f => f.ParameterId == id && f.Rule.DeletedAt == null)
                .Select(f => new { ElementId = f.Rule.Element.Id, ElementName = f.Rule.Element.Name })
                .Distinct()
                .ToListAsync();
            if (usedByElements.Count > 0)
            {
                return BadRequest(new
                {
                    Message = "Cannot delete this parameter as it is used in active rule formulas",
                    UsedByElements = usedByElements
                });
            }

            entity.DeletedAt = DateTimeOffset.UtcNow;
            entity.DeletedBy = User.GetUserId();

            await _db.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Validate that the value is consistent with the declared unit.
        /// Returns an error message or null if valid.
        /// </summary>
        private static string? ValidateUnitAndValue(string code, decimal value, string unit)
        {
            var unitLower = unit.Trim().ToLowerInvariant();

            if (unitLower == "ratio" || unitLower == "taux")
            {
                if (value < 0 || value > 1)
                    return $"Pour l'unité '{unit}', la valeur doit être entre 0 et 1 (ex: 0.0448 pour 4.48%). Valeur reçue: {value}";
            }
            else if (unitLower == "%")
            {
                if (value < 0 || value > 100)
                    return $"Pour l'unité '%', la valeur doit être entre 0 et 100. Valeur reçue: {value}";
            }
            else if (unitLower.Contains("mad") || unitLower.Contains("dh"))
            {
                if (value < 0)
                    return $"Pour l'unité '{unit}', la valeur doit être positive. Valeur reçue: {value}";
            }

            return null;
        }
    }
}
