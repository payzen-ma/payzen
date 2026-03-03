using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.DTOs.Payroll.Referentiel;

namespace payzen_backend.Controllers.Payroll.Referentiel
{
    /// <summary>
    /// API Controller for Eligibility Criteria (ALL, CADRES_SUP, PDG_DG, etc.) - Read-only
    /// </summary>
    [Route("api/payroll/eligibility-criteria")]
    [ApiController]
    [Authorize]
    public class EligibilityCriteriaController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EligibilityCriteriaController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Get all eligibility criteria
        /// GET /api/payroll/eligibility-criteria?includeInactive=false
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<EligibilityCriteriaDto>>> GetAll([FromQuery] bool includeInactive = false)
        {
            var query = _db.EligibilityCriteria.AsNoTracking().AsQueryable();

            if (!includeInactive)
                query = query.Where(e => e.DeletedAt == null);

            var items = await query
                .OrderBy(e => e.Code)
                .Select(e => new EligibilityCriteriaDto
                {
                    Id = e.Id,
                    Code = e.Code,
                    Name = e.Name,
                    Description = e.Description,
                    
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// Get an eligibility criterion by ID
        /// GET /api/payroll/eligibility-criteria/1
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<EligibilityCriteriaDto>> GetById(int id)
        {
            var criteria = await _db.EligibilityCriteria
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new EligibilityCriteriaDto
                {
                    Id = e.Id,
                    Code = e.Code,
                    Name = e.Name,
                    Description = e.Description,
                    
                })
                .FirstOrDefaultAsync();

            if (criteria == null)
                return NotFound(new { Message = "Eligibility criterion not found" });

            return Ok(criteria);
        }
    }
}
