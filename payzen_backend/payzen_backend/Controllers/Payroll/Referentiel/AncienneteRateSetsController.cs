using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.DTOs.Payroll.Referentiel;
using payzen_backend.Extensions;
using payzen_backend.Models.Payroll.Referentiel;

namespace payzen_backend.Controllers.Payroll.Referentiel
{
    /// <summary>
    /// API Controller for Ancienneté (Seniority) Rate Sets
    ///
    /// Design:
    /// - BACKOFFICE: Creates/manages legal default rates (CompanyId = null, IsLegalDefault = true)
    /// - FRONTOFFICE: Companies can customize rates (Copy-on-Write pattern)
    /// - VERSIONING: Changes create new versions, old versions are archived (EffectiveTo set)
    /// - HISTORICAL: Payroll can query rates as of any date for recalculations
    /// </summary>
    [Route("api/payroll/anciennete-rate-sets")]
    [ApiController]
    [Authorize]
    public class AncienneteRateSetsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AncienneteRateSetsController(AppDbContext db)
        {
            _db = db;
        }

        #region Backoffice Endpoints (Legal Default Management)

        /// <summary>
        /// Get all legal default rate sets (backoffice view)
        /// GET /api/payroll/anciennete-rate-sets?includeInactive=false&asOfDate=2025-01-29
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<AncienneteRateSetDto>>> GetAll(
            [FromQuery] bool includeInactive = false,
            [FromQuery] DateOnly? asOfDate = null)
        {
            var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var query = _db.AncienneteRateSets
                .AsNoTracking()
                .Include(s => s.Rates)
                .Where(s => s.CompanyId == null && s.IsLegalDefault) // Only legal defaults
                .AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(s =>
                    s.DeletedAt == null &&
                    s.EffectiveFrom <= checkDate &&
                    (s.EffectiveTo == null || s.EffectiveTo >= checkDate));
            }
            else
            {
                query = query.Where(s => s.DeletedAt == null);
            }

            var items = await query
                .OrderByDescending(s => s.EffectiveFrom)
                .Select(s => MapToDto(s))
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// Get a rate set by ID
        /// GET /api/payroll/anciennete-rate-sets/1
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<AncienneteRateSetDto>> GetById(int id)
        {
            var rateSet = await _db.AncienneteRateSets
                .AsNoTracking()
                .Include(s => s.Rates)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (rateSet == null)
                return NotFound(new { Message = "Ancienneté rate set not found" });

            return Ok(MapToDto(rateSet));
        }

        /// <summary>
        /// Get the current active legal default rate set
        /// GET /api/payroll/anciennete-rate-sets/current?asOfDate=2025-01-29
        /// </summary>
        [HttpGet("current")]
        [Produces("application/json")]
        public async Task<ActionResult<AncienneteRateSetDto>> GetCurrent([FromQuery] DateOnly? asOfDate = null)
        {
            var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            var rateSet = await _db.AncienneteRateSets
                .AsNoTracking()
                .Include(s => s.Rates)
                .Where(s =>
                    s.CompanyId == null &&
                    s.IsLegalDefault &&
                    s.DeletedAt == null &&
                    s.EffectiveFrom <= checkDate &&
                    (s.EffectiveTo == null || s.EffectiveTo >= checkDate))
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (rateSet == null)
                return NotFound(new { Message = "No active ancienneté rate set found for the specified date" });

            return Ok(MapToDto(rateSet));
        }

        /// <summary>
        /// Create a new legal default rate set (Backoffice only)
        /// POST /api/payroll/anciennete-rate-sets
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<AncienneteRateSetDto>> Create([FromBody] CreateAncienneteRateSetDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var name = dto.Name?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
                return BadRequest(new { Message = "Name is required" });

            if (dto.Rates.Count == 0)
                return BadRequest(new { Message = "At least one rate tier is required" });

            // Check for overlapping periods with existing legal defaults
            var overlap = await _db.AncienneteRateSets
                .AsNoTracking()
                .AnyAsync(s =>
                    s.CompanyId == null &&
                    s.IsLegalDefault &&
                    s.DeletedAt == null &&
                    s.EffectiveFrom <= (dto.EffectiveTo ?? DateOnly.MaxValue) &&
                    (s.EffectiveTo == null || s.EffectiveTo >= dto.EffectiveFrom));

            if (overlap)
                return Conflict(new { Message = "A legal default rate set already exists for this period. Use update to create a new version." });

            var userId = User.GetUserId();
            var code = GenerateCode(name);

            var entity = new AncienneteRateSet
            {
                Code = code,
                Name = name,
                CompanyId = null, // Legal default
                ClonedFromId = null,
                IsLegalDefault = true,
                Source = dto.Source?.Trim(),
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId,
                Rates = dto.Rates.Select((r, index) => new AncienneteRate
                {
                    MinYears = r.MinYears,
                    MaxYears = r.MaxYears,
                    Rate = r.Rate,
                    SortOrder = index
                }).ToList()
            };

            _db.AncienneteRateSets.Add(entity);
            await _db.SaveChangesAsync();

            var created = await _db.AncienneteRateSets
                .AsNoTracking()
                .Include(s => s.Rates)
                .FirstAsync(s => s.Id == entity.Id);

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapToDto(created));
        }

        /// <summary>
        /// Update rates with automatic versioning (IMMUTABLE pattern)
        /// Archives current grid and creates new one with today's date
        /// PUT /api/payroll/anciennete-rate-sets/{id}/update-rates
        /// </summary>
        [HttpPut("{id}/update-rates")]
        [Produces("application/json")]
        public async Task<ActionResult<AncienneteRateSetDto>> UpdateWithAutoVersioning(int id, [FromBody] UpdateAncienneteRatesDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentEntity = await _db.AncienneteRateSets
                .Include(s => s.Rates)
                .FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

            if (currentEntity == null)
                return NotFound(new { Message = "Ancienneté rate set not found" });

            if (dto.Rates.Count == 0)
                return BadRequest(new { Message = "At least one rate tier is required" });

            var userId = User.GetUserId();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // IMMUTABLE: Archive the current version by setting EffectiveTo
            currentEntity.EffectiveTo = today.AddDays(-1);
            currentEntity.ModifiedAt = DateTimeOffset.UtcNow;
            currentEntity.ModifiedBy = userId;

            // Create NEW version with updated rates
            var newEntity = new AncienneteRateSet
            {
                Code = currentEntity.Code,
                Name = dto.Name?.Trim() ?? currentEntity.Name,
                CompanyId = currentEntity.CompanyId,
                ClonedFromId = currentEntity.ClonedFromId,
                IsLegalDefault = currentEntity.IsLegalDefault,
                Source = currentEntity.Source,
                EffectiveFrom = today,
                EffectiveTo = null, // Open-ended
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId,
                Rates = dto.Rates.Select((r, index) => new AncienneteRate
                {
                    MinYears = r.MinYears,
                    MaxYears = r.MaxYears,
                    Rate = r.Rate,
                    SortOrder = index
                }).ToList()
            };

            _db.AncienneteRateSets.Add(newEntity);
            await _db.SaveChangesAsync();

            var created = await _db.AncienneteRateSets
                .AsNoTracking()
                .Include(s => s.Rates)
                .FirstAsync(s => s.Id == newEntity.Id);

            return Ok(MapToDto(created));
        }

        /// <summary>
        /// Soft-delete a rate set
        /// DELETE /api/payroll/anciennete-rate-sets/1
        /// </summary>
        [HttpDelete("{id}")]
        [Produces("application/json")]
        public async Task<IActionResult> Delete(int id, [FromQuery] bool confirm = false)
        {
            var entity = await _db.AncienneteRateSets
                .Include(s => s.Rates)
                .FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

            if (entity == null)
                return NotFound(new { Message = "Ancienneté rate set not found" });

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var isActive = entity.EffectiveFrom <= today &&
                (entity.EffectiveTo == null || entity.EffectiveTo >= today);

            if (isActive && !confirm)
            {
                return Ok(new
                {
                    RequiresConfirmation = true,
                    Message = "Cette grille d'ancienneté est actuellement active. Êtes-vous sûr de vouloir la supprimer ?",
                    IsActive = true
                });
            }

            entity.DeletedAt = DateTimeOffset.UtcNow;
            entity.DeletedBy = User.GetUserId();

            await _db.SaveChangesAsync();
            return NoContent();
        }

        #endregion

        #region Frontoffice Endpoints (Company Rate Customization)

        /// <summary>
        /// Get rates for a specific company (falls back to legal default if no custom rates)
        /// GET /api/payroll/anciennete-rate-sets/company/{companyId}?asOfDate=2025-01-29
        /// </summary>
        [HttpGet("company/{companyId}")]
        [Produces("application/json")]
        public async Task<ActionResult<AncienneteRateSetDto>> GetForCompany(int companyId, [FromQuery] DateOnly? asOfDate = null)
        {
            var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            // First, try to find company-specific rates
            var companyRateSet = await _db.AncienneteRateSets
                .AsNoTracking()
                .Include(s => s.Rates)
                .Where(s =>
                    s.CompanyId == companyId &&
                    s.DeletedAt == null &&
                    s.EffectiveFrom <= checkDate &&
                    (s.EffectiveTo == null || s.EffectiveTo >= checkDate))
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (companyRateSet != null)
                return Ok(MapToDto(companyRateSet));

            // Fall back to legal default
            var legalDefault = await _db.AncienneteRateSets
                .AsNoTracking()
                .Include(s => s.Rates)
                .Where(s =>
                    s.CompanyId == null &&
                    s.IsLegalDefault &&
                    s.DeletedAt == null &&
                    s.EffectiveFrom <= checkDate &&
                    (s.EffectiveTo == null || s.EffectiveTo >= checkDate))
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (legalDefault == null)
                return NotFound(new { Message = "No rate set found for the specified date" });

            return Ok(MapToDto(legalDefault));
        }

        /// <summary>
        /// Customize rates for a company (Copy-on-Write pattern)
        /// If company has no custom rates, clones from legal default
        /// If company already has custom rates, creates a new version
        /// POST /api/payroll/anciennete-rate-sets/company/{companyId}/customize
        /// </summary>
        [HttpPost("company/{companyId}/customize")]
        [Produces("application/json")]
        public async Task<ActionResult<AncienneteRateSetDto>> CustomizeForCompany(int companyId, [FromBody] CustomizeCompanyRatesDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.Rates.Count == 0)
                return BadRequest(new { Message = "At least one rate tier is required" });

            // Validate rates against legal minimum
            var validationResult = await ValidateAgainstLegalMinimum(dto.Rates);
            if (!validationResult.IsValid)
                return BadRequest(new { Message = "Rates violate legal minimum", Violations = validationResult.Violations });

            var userId = User.GetUserId();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Check if company already has custom rates
            var existingCompanyRates = await _db.AncienneteRateSets
                .Include(s => s.Rates)
                .Where(s =>
                    s.CompanyId == companyId &&
                    s.DeletedAt == null &&
                    (s.EffectiveTo == null || s.EffectiveTo >= today))
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (existingCompanyRates != null)
            {
                // Archive existing company rates
                existingCompanyRates.EffectiveTo = today.AddDays(-1);
                existingCompanyRates.ModifiedAt = DateTimeOffset.UtcNow;
                existingCompanyRates.ModifiedBy = userId;
            }

            // Get the current legal default (for cloning reference)
            var legalDefault = await _db.AncienneteRateSets
                .AsNoTracking()
                .Where(s =>
                    s.CompanyId == null &&
                    s.IsLegalDefault &&
                    s.DeletedAt == null &&
                    s.EffectiveFrom <= today &&
                    (s.EffectiveTo == null || s.EffectiveTo >= today))
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefaultAsync();

            // Create new company-specific rate set
            var newEntity = new AncienneteRateSet
            {
                Code = $"COMPANY_{companyId}",
                Name = $"Grille personnalisée",
                CompanyId = companyId,
                ClonedFromId = legalDefault?.Id,
                IsLegalDefault = false,
                Source = "Company customization",
                EffectiveFrom = today,
                EffectiveTo = null,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId,
                Rates = dto.Rates.Select((r, index) => new AncienneteRate
                {
                    MinYears = r.MinYears,
                    MaxYears = r.MaxYears,
                    Rate = r.Rate,
                    SortOrder = index
                }).ToList()
            };

            _db.AncienneteRateSets.Add(newEntity);
            await _db.SaveChangesAsync();

            var created = await _db.AncienneteRateSets
                .AsNoTracking()
                .Include(s => s.Rates)
                .FirstAsync(s => s.Id == newEntity.Id);

            return Ok(MapToDto(created));
        }

        /// <summary>
        /// Validate rates against legal minimum
        /// POST /api/payroll/anciennete-rate-sets/validate
        /// </summary>
        [HttpPost("validate")]
        [Produces("application/json")]
        public async Task<ActionResult<RateValidationResultDto>> ValidateRates([FromBody] List<CreateAncienneteRateDto> rates)
        {
            var result = await ValidateAgainstLegalMinimum(rates);
            return Ok(result);
        }

        #endregion

        #region Calculation Endpoints

        /// <summary>
        /// Calculate the ancienneté rate for given years
        /// GET /api/payroll/anciennete-rate-sets/{id}/calculate?years=7
        /// </summary>
        [HttpGet("{id}/calculate")]
        [Produces("application/json")]
        public async Task<ActionResult<object>> CalculateRate(int id, [FromQuery] int years)
        {
            var rateSet = await _db.AncienneteRateSets
                .AsNoTracking()
                .Include(s => s.Rates)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (rateSet == null)
                return NotFound(new { Message = "Ancienneté rate set not found" });

            var rate = rateSet.GetRateForYears(years);

            return Ok(new
            {
                rateSetId = rateSet.Id,
                rateSetName = rateSet.Name,
                years = years,
                rate = rate,
                ratePercentage = rate * 100
            });
        }

        #endregion

        #region Private Helpers

        private async Task<RateValidationResultDto> ValidateAgainstLegalMinimum(List<CreateAncienneteRateDto> rates)
        {
            var result = new RateValidationResultDto { IsValid = true, Violations = new List<string>() };

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var legalDefault = await _db.AncienneteRateSets
                .AsNoTracking()
                .Include(s => s.Rates)
                .Where(s =>
                    s.CompanyId == null &&
                    s.IsLegalDefault &&
                    s.DeletedAt == null &&
                    s.EffectiveFrom <= today &&
                    (s.EffectiveTo == null || s.EffectiveTo >= today))
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (legalDefault == null)
            {
                // No legal default configured, allow any rates
                return result;
            }

            foreach (var rate in rates)
            {
                var legalRate = legalDefault.Rates
                    .FirstOrDefault(r => r.MinYears == rate.MinYears);

                if (legalRate != null && rate.Rate < legalRate.Rate)
                {
                    result.IsValid = false;
                    result.Violations.Add(
                        $"Le taux pour {rate.MinYears}+ ans ({rate.Rate * 100:F1}%) " +
                        $"est inférieur au minimum légal ({legalRate.Rate * 100:F1}%)");
                }
            }

            return result;
        }

        private static string GenerateCode(string name)
        {
            var code = name.ToUpperInvariant()
                .Replace(" ", "_")
                .Replace("'", "")
                .Replace("-", "_");

            code = System.Text.Encoding.ASCII.GetString(
                System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(code));

            code = System.Text.RegularExpressions.Regex.Replace(code, @"[^A-Z0-9_]", "");
            return code.Length > 50 ? code[..50] : code;
        }

        private static AncienneteRateSetDto MapToDto(AncienneteRateSet entity)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var isActive = entity.DeletedAt == null &&
                entity.EffectiveFrom <= today &&
                (entity.EffectiveTo == null || entity.EffectiveTo >= today);

            return new AncienneteRateSetDto
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                IsLegalDefault = entity.IsLegalDefault,
                Source = entity.Source,
                EffectiveFrom = entity.EffectiveFrom,
                EffectiveTo = entity.EffectiveTo,
                IsActive = isActive,
                CompanyId = entity.CompanyId,
                ClonedFromId = entity.ClonedFromId,
                Rates = entity.Rates
                    .OrderBy(r => r.MinYears)
                    .Select(r => new AncienneteRateDto
                    {
                        Id = r.Id,
                        MinYears = r.MinYears,
                        MaxYears = r.MaxYears,
                        Rate = r.Rate
                    })
                    .ToList()
            };
        }

        #endregion
    }
}
