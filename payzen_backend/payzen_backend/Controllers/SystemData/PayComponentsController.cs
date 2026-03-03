using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Payroll;
using payzen_backend.Models.Payroll.Dtos;

namespace payzen_backend.Controllers.SystemData
{
    /// <summary>
    /// Controller for managing global pay component catalog (backoffice)
    /// Pay components define regulatory rules for Moroccan payroll compliance
    /// </summary>
    [Route("api/pay-components")]
    [ApiController]
    [Authorize]
    public class PayComponentsController : ControllerBase
    {
        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "base_salary",
            "allowance",
            "bonus",
            "benefit_in_kind",
            "social_charge"
        };

        private readonly AppDbContext _db;

        public PayComponentsController(AppDbContext db) => _db = db;

        /// <summary>
        /// Get all pay components (optionally filtered by type or active status)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PayComponentReadDto>>> GetAll(
            [FromQuery] string? type = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool? isRegulated = null)
        {
            var query = _db.PayComponents
                .AsNoTracking()
                .Where(pc => pc.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(type))
            {
                var normalizedType = type.Trim().ToLowerInvariant();
                if (AllowedTypes.Contains(normalizedType))
                    query = query.Where(pc => pc.Type == normalizedType);
            }

            if (isActive.HasValue)
                query = query.Where(pc => pc.IsActive == isActive.Value);

            if (isRegulated.HasValue)
                query = query.Where(pc => pc.IsRegulated == isRegulated.Value);

            var components = await query
                .OrderBy(pc => pc.SortOrder)
                .ThenBy(pc => pc.Code)
                .ThenByDescending(pc => pc.Version)
                .ToListAsync();

            var result = components.Select(MapToReadDto);

            return Ok(result);
        }

        /// <summary>
        /// Get active components valid at a specific date
        /// </summary>
        [HttpGet("effective")]
        public async Task<ActionResult<IEnumerable<PayComponentReadDto>>> GetEffective([FromQuery] DateTime? date = null)
        {
            var effectiveDate = date ?? DateTime.UtcNow;

            var components = await _db.PayComponents
                .AsNoTracking()
                .Where(pc => pc.DeletedAt == null 
                    && pc.IsActive 
                    && pc.ValidFrom <= effectiveDate
                    && (pc.ValidTo == null || pc.ValidTo >= effectiveDate))
                .OrderBy(pc => pc.SortOrder)
                .ThenBy(pc => pc.Code)
                .ToListAsync();

            var result = components.Select(MapToReadDto);

            return Ok(result);
        }

        /// <summary>
        /// Get a pay component by id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PayComponentReadDto>> GetById(int id)
        {
            var component = await _db.PayComponents
                .AsNoTracking()
                .FirstOrDefaultAsync(pc => pc.Id == id && pc.DeletedAt == null);

            if (component == null)
                return NotFound(new { Message = "Pay component not found" });

            return Ok(MapToReadDto(component));
        }

        /// <summary>
        /// Get a pay component by code (returns the latest active version)
        /// </summary>
        [HttpGet("code/{code}")]
        public async Task<ActionResult<PayComponentReadDto>> GetByCode(string code)
        {
            var component = await _db.PayComponents
                .AsNoTracking()
                .Where(pc => pc.Code == code.ToUpperInvariant() && pc.DeletedAt == null && pc.IsActive)
                .OrderByDescending(pc => pc.Version)
                .FirstOrDefaultAsync();

            if (component == null)
                return NotFound(new { Message = "Pay component not found" });

            return Ok(MapToReadDto(component));
        }

        /// <summary>
        /// Create a new pay component
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PayComponentReadDto>> Create([FromBody] PayComponentWriteDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Invalid data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            var code = dto.Code.Trim().ToUpperInvariant();
            var type = dto.Type.Trim().ToLowerInvariant();

            if (!AllowedTypes.Contains(type))
                return BadRequest(new { Message = "Type must be one of: base_salary, allowance, bonus, benefit_in_kind, social_charge" });

            // Check if code already exists
            var codeExists = await _db.PayComponents
                .AnyAsync(pc => pc.Code == code && pc.DeletedAt == null);

            if (codeExists)
                return Conflict(new { Message = "A pay component with this code already exists" });

            var component = new PayComponent
            {
                Code = code,
                NameFr = dto.NameFr.Trim(),
                NameAr = dto.NameAr?.Trim(),
                NameEn = dto.NameEn?.Trim(),
                Type = type,
                IsTaxable = dto.IsTaxable,
                IsSocial = dto.IsSocial,
                IsCIMR = dto.IsCIMR,
                ExemptionLimit = dto.ExemptionLimit,
                ExemptionRule = dto.ExemptionRule?.Trim(),
                DefaultAmount = dto.DefaultAmount,
                Version = 1,
                ValidFrom = dto.ValidFrom ?? DateTime.UtcNow,
                ValidTo = dto.ValidTo,
                IsRegulated = dto.IsRegulated,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder ?? 0,
                CreatedAt = now,
                CreatedBy = userId
            };

            _db.PayComponents.Add(component);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = component.Id }, MapToReadDto(component));
        }

        /// <summary>
        /// Update a pay component (creates a new version if regulated)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PayComponentReadDto>> Update(int id, [FromBody] PayComponentWriteDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Invalid data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var component = await _db.PayComponents
                .FirstOrDefaultAsync(pc => pc.Id == id && pc.DeletedAt == null);

            if (component == null)
                return NotFound(new { Message = "Pay component not found" });

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            var type = dto.Type.Trim().ToLowerInvariant();

            if (!AllowedTypes.Contains(type))
                return BadRequest(new { Message = "Type must be one of: base_salary, allowance, bonus, benefit_in_kind, social_charge" });

            // If component is used in packages, we need to check if we should create a new version
            var isUsedInPackages = await _db.SalaryPackageItems
                .AnyAsync(spi => spi.PayComponentId == id && spi.DeletedAt == null);

            if (isUsedInPackages && component.IsRegulated)
            {
                // Create a new version instead of updating
                return await CreateNewVersion(id, dto);
            }

            // Update existing component (only for non-regulated or unused components)
            component.NameFr = dto.NameFr.Trim();
            component.NameAr = dto.NameAr?.Trim();
            component.NameEn = dto.NameEn?.Trim();
            component.Type = type;
            component.IsTaxable = dto.IsTaxable;
            component.IsSocial = dto.IsSocial;
            component.IsCIMR = dto.IsCIMR;
            component.ExemptionLimit = dto.ExemptionLimit;
            component.ExemptionRule = dto.ExemptionRule?.Trim();
            component.DefaultAmount = dto.DefaultAmount;
            component.ValidFrom = dto.ValidFrom ?? component.ValidFrom;
            component.ValidTo = dto.ValidTo;
            component.IsRegulated = dto.IsRegulated;
            component.IsActive = dto.IsActive;
            component.SortOrder = dto.SortOrder ?? component.SortOrder;
            component.ModifiedAt = now;
            component.ModifiedBy = userId;

            await _db.SaveChangesAsync();

            return Ok(MapToReadDto(component));
        }

        /// <summary>
        /// Create a new version of an existing component (for regulatory changes)
        /// </summary>
        [HttpPost("{id}/new-version")]
        public async Task<ActionResult<PayComponentReadDto>> CreateNewVersion(int id, [FromBody] PayComponentWriteDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Invalid data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var sourceComponent = await _db.PayComponents
                .FirstOrDefaultAsync(pc => pc.Id == id && pc.DeletedAt == null);

            if (sourceComponent == null)
                return NotFound(new { Message = "Pay component not found" });

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            var type = dto.Type.Trim().ToLowerInvariant();

            if (!AllowedTypes.Contains(type))
                return BadRequest(new { Message = "Type must be one of: base_salary, allowance, bonus, benefit_in_kind, social_charge" });

            // Get the next version number
            var maxVersion = await _db.PayComponents
                .Where(pc => pc.Code == sourceComponent.Code && pc.DeletedAt == null)
                .MaxAsync(pc => pc.Version);

            // Mark the old version as ended
            sourceComponent.ValidTo = dto.ValidFrom ?? DateTime.UtcNow;
            sourceComponent.IsActive = false;
            sourceComponent.ModifiedAt = now;
            sourceComponent.ModifiedBy = userId;

            // Create new version
            var newVersion = new PayComponent
            {
                Code = sourceComponent.Code, // Keep the same code
                NameFr = dto.NameFr.Trim(),
                NameAr = dto.NameAr?.Trim(),
                NameEn = dto.NameEn?.Trim(),
                Type = type,
                IsTaxable = dto.IsTaxable,
                IsSocial = dto.IsSocial,
                IsCIMR = dto.IsCIMR,
                ExemptionLimit = dto.ExemptionLimit,
                ExemptionRule = dto.ExemptionRule?.Trim(),
                DefaultAmount = dto.DefaultAmount,
                Version = maxVersion + 1,
                ValidFrom = dto.ValidFrom ?? DateTime.UtcNow,
                ValidTo = dto.ValidTo,
                IsRegulated = dto.IsRegulated,
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder ?? sourceComponent.SortOrder,
                CreatedAt = now,
                CreatedBy = userId
            };

            _db.PayComponents.Add(newVersion);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = newVersion.Id }, MapToReadDto(newVersion));
        }

        /// <summary>
        /// Soft delete a pay component
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var component = await _db.PayComponents
                .FirstOrDefaultAsync(pc => pc.Id == id && pc.DeletedAt == null);

            if (component == null)
                return NotFound(new { Message = "Pay component not found" });

            // Check if component is used in packages
            var isUsedInPackages = await _db.SalaryPackageItems
                .AnyAsync(spi => spi.PayComponentId == id && spi.DeletedAt == null);

            if (isUsedInPackages)
                return BadRequest(new { Message = "Cannot delete a pay component that is used in salary packages. Deactivate it instead." });

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            component.DeletedAt = now;
            component.DeletedBy = userId;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Deactivate a pay component (soft deprecation)
        /// </summary>
        [HttpPost("{id}/deactivate")]
        public async Task<ActionResult<PayComponentReadDto>> Deactivate(int id)
        {
            var component = await _db.PayComponents
                .FirstOrDefaultAsync(pc => pc.Id == id && pc.DeletedAt == null);

            if (component == null)
                return NotFound(new { Message = "Pay component not found" });

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            component.IsActive = false;
            component.ValidTo = DateTime.UtcNow;
            component.ModifiedAt = now;
            component.ModifiedBy = userId;

            await _db.SaveChangesAsync();

            return Ok(MapToReadDto(component));
        }

        private static PayComponentReadDto MapToReadDto(PayComponent component)
        {
            var updatedAt = (component.ModifiedAt ?? component.CreatedAt).DateTime;

            return new PayComponentReadDto
            {
                Id = component.Id,
                Code = component.Code,
                NameFr = component.NameFr,
                NameAr = component.NameAr,
                NameEn = component.NameEn,
                Type = component.Type,
                IsTaxable = component.IsTaxable,
                IsSocial = component.IsSocial,
                IsCIMR = component.IsCIMR,
                ExemptionLimit = component.ExemptionLimit,
                ExemptionRule = component.ExemptionRule,
                DefaultAmount = component.DefaultAmount,
                Version = component.Version,
                ValidFrom = component.ValidFrom,
                ValidTo = component.ValidTo,
                IsRegulated = component.IsRegulated,
                IsActive = component.IsActive,
                SortOrder = component.SortOrder,
                CreatedAt = component.CreatedAt.DateTime,
                UpdatedAt = updatedAt
            };
        }
    }
}
