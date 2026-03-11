using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.DTOs.Payroll.Referentiel;

namespace payzen_backend.Controllers.v1.Payroll.Referentiel
{
    /// <summary>
    /// API Controller for Authorities (CNSS, IR, AMO, CIMR) - Read-only
    /// </summary>
    [Route("api/v{version:apiVersion}/payroll/authorities")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class AuthoritiesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AuthoritiesController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Get all authorities
        /// GET /api/payroll/authorities?includeInactive=false
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<AuthorityDto>>> GetAll([FromQuery] bool includeInactive = false)
        {
            var query = _db.Authorities.AsNoTracking().Where(a => a.DeletedAt == null);

            if (!includeInactive)
                query = query.Where(a => a.IsActive);

            var items = await query
                .OrderBy(a => a.SortOrder)
                .ThenBy(a => a.Name)
                .Select(a => new AuthorityDto
                {
                    Id = a.Id,
                    Code = a.Code,
                    Name = a.Name,
                    Description = a.Description,
                    IsActive = a.IsActive
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// Get an authority by ID
        /// GET /api/payroll/authorities/1
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<AuthorityDto>> GetById(int id)
        {
            var authority = await _db.Authorities
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new AuthorityDto
                {
                    Id = a.Id,
                    Code = a.Code,
                    Name = a.Name,
                    Description = a.Description,
                    IsActive = a.IsActive
                })
                .FirstOrDefaultAsync();

            if (authority == null)
                return NotFound(new { Message = "Authority not found" });

            return Ok(authority);
        }

        /// <summary>
        /// Get an authority by code
        /// GET /api/payroll/authorities/by-code/CNSS
        /// </summary>
        [HttpGet("by-code/{code}")]
        [Produces("application/json")]
        public async Task<ActionResult<AuthorityDto>> GetByCode(string code)
        {
            var authority = await _db.Authorities
                .AsNoTracking()
                .Where(a => a.Code.ToLower() == code.ToLower())
                .Select(a => new AuthorityDto
                {
                    Id = a.Id,
                    Code = a.Code,
                    Name = a.Name,
                    Description = a.Description,
                    IsActive = a.IsActive
                })
                .FirstOrDefaultAsync();

            if (authority == null)
                return NotFound(new { Message = "Authority not found" });

            return Ok(authority);
        }
    }
}
