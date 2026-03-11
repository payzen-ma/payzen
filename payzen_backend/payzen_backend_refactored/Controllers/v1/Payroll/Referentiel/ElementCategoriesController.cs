using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.DTOs.Payroll.Referentiel;

namespace payzen_backend.Controllers.v1.Payroll.Referentiel
{
    /// <summary>
    /// API Controller for Element Categories (IND_PRO, IND_SOCIAL, PRIME_SPEC, AVANTAGE) - Read-only
    /// </summary>
    [Route("api/v{version:apiVersion}/payroll/element-categories")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class ElementCategoriesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ElementCategoriesController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Get all element categories
        /// GET /api/payroll/element-categories?includeInactive=false
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<ElementCategoryDto>>> GetAll([FromQuery] bool includeInactive = false)
        {
            var query = _db.ElementCategories.AsNoTracking().Where(c => c.DeletedAt == null);

            if (!includeInactive)
                query = query.Where(c => c.IsActive);

            var items = await query
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(c => new ElementCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    SortOrder = c.SortOrder,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// Create a new element category
        /// POST /api/payroll/element-categories
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<ElementCategoryDto>> Create([FromBody] CreateElementCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { Message = "Name is required" });

            // Check for duplicate name
            var exists = await _db.ElementCategories
                .AnyAsync(c => c.Name == dto.Name.Trim() && c.DeletedAt == null);
            if (exists)
                return Conflict(new { Message = $"A category named '{dto.Name.Trim()}' already exists" });

            var maxSort = await _db.ElementCategories
                .Where(c => c.DeletedAt == null)
                .MaxAsync(c => (int?)c.SortOrder) ?? 0;

            var category = new payzen_backend.Models.Payroll.Referentiel.ElementCategory
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                SortOrder = maxSort + 1,
                CreatedBy = 1
            };

            _db.ElementCategories.Add(category);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, new ElementCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                SortOrder = category.SortOrder
            });
        }

        /// <summary>
        /// Get a category by ID
        /// GET /api/payroll/element-categories/1
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<ElementCategoryDto>> GetById(int id)
        {
            var category = await _db.ElementCategories
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new ElementCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    SortOrder = c.SortOrder,
                    IsActive = c.IsActive
                })
                .FirstOrDefaultAsync();

            if (category == null)
                return NotFound(new { Message = "Category not found" });

            return Ok(category);
        }
    }
}
