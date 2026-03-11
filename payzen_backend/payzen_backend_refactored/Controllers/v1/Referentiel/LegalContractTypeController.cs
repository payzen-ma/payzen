using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Referentiel;
using payzen_backend.Models.Referentiel.Dtos;

namespace payzen_backend.Controllers.v1.Referentiel
{
    [Route("api/v{version:apiVersion}/legal-contract-types")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class LegalContractTypeController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LegalContractTypeController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Récupère tous les types de contrats légaux actifs
        /// GET /api/legal-contract-types
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<LegalContractTypeReadDtos>>> GetAll()
        {
            var legalContractTypes = await _db.LegalContractTypes
                .AsNoTracking()
                .Where(l => l.DeletedAt == null)
                .OrderBy(l => l.Code)
                .Select(l => new LegalContractTypeReadDtos
                {
                    Id = l.Id,
                    Code = l.Code,
                    Name = l.Name
                })
                .ToListAsync();

            return Ok(legalContractTypes);
        }

        /// <summary>
        /// Récupère un type de contrat légal par ID
        /// GET /api/legal-contract-types/{id}
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<LegalContractTypeReadDtos>> GetById(int id)
        {
            var legalContractType = await _db.LegalContractTypes
                .AsNoTracking()
                .Where(l => l.Id == id && l.DeletedAt == null)
                .Select(l => new LegalContractTypeReadDtos
                {
                    Id = l.Id,
                    Code = l.Code,
                    Name = l.Name
                })
                .FirstOrDefaultAsync();

            if (legalContractType == null)
                return NotFound(new { Message = "Type de contrat légal non trouvé" });

            return Ok(legalContractType);
        }

        /// <summary>
        /// Crée un nouveau type de contrat légal
        /// POST /api/legal-contract-types
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<LegalContractTypeReadDtos>> Create([FromBody] LegalContractTypeCreateDtos dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            // Vérifier l'unicité du code
            if (await _db.LegalContractTypes.AnyAsync(l => l.Code == dto.Code && l.DeletedAt == null))
                return Conflict(new { Message = "Un type de contrat légal avec ce code existe déjà" });

            // Vérifier l'unicité du nom
            if (await _db.LegalContractTypes.AnyAsync(l => l.Name == dto.Name && l.DeletedAt == null))
                return Conflict(new { Message = "Un type de contrat légal avec ce nom existe déjà" });

            var legalContractType = new LegalContractType
            {
                Code = dto.Code.Trim().ToUpper(),
                Name = dto.Name.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.LegalContractTypes.Add(legalContractType);
            await _db.SaveChangesAsync();

            var result = new LegalContractTypeReadDtos
            {
                Id = legalContractType.Id,
                Code = legalContractType.Code,
                Name = legalContractType.Name
            };

            return CreatedAtAction(nameof(GetById), new { id = legalContractType.Id }, result);
        }

        /// <summary>
        /// Met à jour un type de contrat légal
        /// PUT /api/legal-contract-types/{id}
        /// </summary>
        [HttpPut("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<LegalContractTypeReadDtos>> Update(int id, [FromBody] LegalContractTypeUpdateDtos dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            var legalContractType = await _db.LegalContractTypes
                .FirstOrDefaultAsync(l => l.Id == id && l.DeletedAt == null);

            if (legalContractType == null)
                return NotFound(new { Message = "Type de contrat légal non trouvé" });

            bool hasChanges = false;

            // Mise à jour du code
            if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code.Trim().ToUpper() != legalContractType.Code)
            {
                var newCode = dto.Code.Trim().ToUpper();
                if (await _db.LegalContractTypes.AnyAsync(l => l.Code == newCode && l.Id != id && l.DeletedAt == null))
                    return Conflict(new { Message = "Un type de contrat légal avec ce code existe déjà" });

                legalContractType.Code = newCode;
                hasChanges = true;
            }

            // Mise à jour du nom
            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name.Trim() != legalContractType.Name)
            {
                var newName = dto.Name.Trim();
                if (await _db.LegalContractTypes.AnyAsync(l => l.Name == newName && l.Id != id && l.DeletedAt == null))
                    return Conflict(new { Message = "Un type de contrat légal avec ce nom existe déjà" });

                legalContractType.Name = newName;
                hasChanges = true;
            }

            if (hasChanges)
            {
                legalContractType.UpdatedAt = DateTimeOffset.UtcNow;
                legalContractType.UpdatedBy = userId;
                await _db.SaveChangesAsync();
            }

            var result = new LegalContractTypeReadDtos
            {
                Id = legalContractType.Id,
                Code = legalContractType.Code,
                Name = legalContractType.Name
            };

            return Ok(result);
        }

        /// <summary>
        /// Supprime un type de contrat légal (soft delete)
        /// DELETE /api/legal-contract-types/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();

            var legalContractType = await _db.LegalContractTypes
                .FirstOrDefaultAsync(l => l.Id == id && l.DeletedAt == null);

            if (legalContractType == null)
                return NotFound(new { Message = "Type de contrat légal non trouvé" });

            // Vérifier si le type est utilisé par des ContractTypes
            var isUsed = await _db.ContractTypes
                .AnyAsync(c => c.LegalContractTypeId == id && c.DeletedAt == null);

            if (isUsed)
                return BadRequest(new { Message = "Impossible de supprimer ce type de contrat légal car il est utilisé par des types de contrats" });

            legalContractType.DeletedAt = DateTimeOffset.UtcNow;
            legalContractType.DeletedBy = userId;

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
