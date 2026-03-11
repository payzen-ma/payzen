using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.DTOs.Payroll.Referentiel;
using payzen_backend.Models.Payroll.Referentiel;
using System.Security.Claims;

namespace payzen_backend.Controllers.v1.Payroll.Referentiel
{
    [ApiController]
        [Route("api/v{version:apiVersion}/business-sectors")]
        [ApiVersion("1.0")]
    [Authorize]
    public class BusinessSectorsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BusinessSectorsController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Get all business sectors
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BusinessSectorDto>>> GetAll(
            [FromQuery] bool includeInactive = false)
        {
            var query = _context.BusinessSectors.AsQueryable();

            if (includeInactive)
            {
                query = query.IgnoreQueryFilters();
            }

            var sectors = await query
                .OrderBy(bs => bs.SortOrder)
                .ThenBy(bs => bs.Name)
                .Select(bs => new BusinessSectorDto
                {
                    Id = bs.Id,
                    Code = bs.Code,
                    Name = bs.Name,
                    IsStandard = bs.IsStandard,
                    SortOrder = bs.SortOrder,
                    IsActive = bs.DeletedAt == null
                })
                .ToListAsync();

            return Ok(sectors);
        }

        /// <summary>
        /// Get single business sector by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<BusinessSectorDto>> GetById(int id)
        {
            var sector = await _context.BusinessSectors
                .Where(bs => bs.Id == id)
                .Select(bs => new BusinessSectorDto
                {
                    Id = bs.Id,
                    Code = bs.Code,
                    Name = bs.Name,
                    IsStandard = bs.IsStandard,
                    SortOrder = bs.SortOrder,
                    IsActive = bs.DeletedAt == null
                })
                .FirstOrDefaultAsync();

            if (sector == null)
            {
                return NotFound(new { Message = "Secteur d'activité introuvable" });
            }

            return Ok(sector);
        }

        /// <summary>
        /// Create new custom business sector
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BusinessSectorDto>> Create(
            [FromBody] CreateBusinessSectorDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check for duplicate code (case-insensitive)
            var codeExists = await _context.BusinessSectors
                .Where(bs => bs.DeletedAt == null)
                .AnyAsync(bs => bs.Code.ToLower() == dto.Code.ToLower());

            if (codeExists)
            {
                return BadRequest(new { Message = "Un secteur avec ce code existe déjà" });
            }

            var sector = new BusinessSector
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                IsStandard = false, // Custom sectors are never standard
                SortOrder = dto.SortOrder,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = GetCurrentUserId()
            };

            _context.BusinessSectors.Add(sector);
            await _context.SaveChangesAsync();

            var result = new BusinessSectorDto
            {
                Id = sector.Id,
                Code = sector.Code,
                Name = sector.Name,
                IsStandard = sector.IsStandard,
                SortOrder = sector.SortOrder,
                IsActive = true
            };

            return CreatedAtAction(nameof(GetById), new { id = sector.Id }, result);
        }

        /// <summary>
        /// Update business sector
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<BusinessSectorDto>> Update(
            int id,
            [FromBody] UpdateBusinessSectorDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sector = await _context.BusinessSectors
                .FirstOrDefaultAsync(bs => bs.Id == id && bs.DeletedAt == null);

            if (sector == null)
            {
                return NotFound(new { Message = "Secteur d'activité introuvable" });
            }

            // For standard sectors, only allow Name and SortOrder updates
            if (sector.IsStandard && sector.Code != dto.Code)
            {
                return BadRequest(new { Message = "Le code d'un secteur standard ne peut pas être modifié" });
            }

            // Check for duplicate code (excluding current sector)
            var codeExists = await _context.BusinessSectors
                .Where(bs => bs.DeletedAt == null && bs.Id != id)
                .AnyAsync(bs => bs.Code.ToLower() == dto.Code.ToLower());

            if (codeExists)
            {
                return BadRequest(new { Message = "Un autre secteur avec ce code existe déjà" });
            }

            sector.Code = dto.Code.Trim();
            sector.Name = dto.Name.Trim();
            sector.SortOrder = dto.SortOrder;
            sector.ModifiedAt = DateTimeOffset.UtcNow;
            sector.ModifiedBy = GetCurrentUserId();

            await _context.SaveChangesAsync();

            var result = new BusinessSectorDto
            {
                Id = sector.Id,
                Code = sector.Code,
                Name = sector.Name,
                IsStandard = sector.IsStandard,
                SortOrder = sector.SortOrder,
                IsActive = true
            };

            return Ok(result);
        }

        /// <summary>
        /// Soft delete business sector
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var sector = await _context.BusinessSectors
                .FirstOrDefaultAsync(bs => bs.Id == id && bs.DeletedAt == null);

            if (sector == null)
            {
                return NotFound(new { Message = "Secteur d'activité introuvable" });
            }

            // Prevent deletion of standard sectors
            if (sector.IsStandard)
            {
                return BadRequest(new { Message = "Les secteurs standards CNSS ne peuvent pas être supprimés" });
            }

            // Soft delete
            sector.DeletedAt = DateTimeOffset.UtcNow;
            sector.DeletedBy = GetCurrentUserId();

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
