using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Company;
using payzen_backend.Models.Company.Dtos;

namespace payzen_backend.Controllers.SystemData
{
    [Route("api/contract-types")]
    [ApiController]
    [Authorize]
    public class ContractTypesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ContractTypesController(AppDbContext db) => _db = db;

        /// <summary>
        /// Récupère tous les types de contrat actifs
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContractTypeReadDto>>> GetAll()
        {
            var contractTypes = await _db.ContractTypes
                .AsNoTracking()
                .Where(ct => ct.DeletedAt == null)
                .Include(ct => ct.Company)
                .Include(ct => ct.LegalContractType)
                .Include(ct => ct.StateEmploymentProgram)
                .OrderBy(ct => ct.ContractTypeName)
                .ToListAsync();

            var result = contractTypes.Select(ct => new ContractTypeReadDto
            {
                Id = ct.Id,
                ContractTypeName = ct.ContractTypeName,
                CompanyId = ct.CompanyId,
                CompanyName = ct.Company?.CompanyName,
                LegalContractTypeId = ct.LegalContractTypeId,
                StateEmploymentProgramId = ct.StateEmploymentProgramId,
                LegalContractTypeName = ct.LegalContractType?.Name,
                StateEmploymentProgramName = ct.StateEmploymentProgram?.Name,
                CreatedAt = ct.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// Récupère un type de contrat par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ContractTypeReadDto>> GetById(int id)
        {
            var contractType = await _db.ContractTypes
                .AsNoTracking()
                .Where(ct => ct.DeletedAt == null)
                .Include(ct => ct.Company)
                .Include(ct => ct.LegalContractType)
                .Include(ct => ct.StateEmploymentProgram)
                .FirstOrDefaultAsync(ct => ct.Id == id);

            if (contractType == null)
                return NotFound(new { Message = "Type de contrat non trouvé" });

            var result = new ContractTypeReadDto
            {
                Id = contractType.Id,
                ContractTypeName = contractType.ContractTypeName,
                CompanyId = contractType.CompanyId,
                CompanyName = contractType.Company?.CompanyName,
                LegalContractTypeId = contractType.LegalContractTypeId,
                StateEmploymentProgramId = contractType.StateEmploymentProgramId,
                LegalContractTypeName = contractType.LegalContractType?.Name,
                StateEmploymentProgramName = contractType.StateEmploymentProgram?.Name,
                CreatedAt = contractType.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Récupère tous les types de contrat d'une société
        /// </summary>
        [HttpGet("by-company/{companyId}")]
        public async Task<ActionResult<IEnumerable<ContractTypeReadDto>>> GetByCompany(int companyId)
        {
            var companyExists = await _db.Companies
                .AnyAsync(c => c.Id == companyId && c.DeletedAt == null);

            if (!companyExists)
                return NotFound(new { Message = "Société non trouvée" });

            var contractTypes = await _db.ContractTypes
                .AsNoTracking()
                .Where(ct => ct.CompanyId == companyId && ct.DeletedAt == null)
                .Include(ct => ct.LegalContractType)
                .Include(ct => ct.StateEmploymentProgram)
                .OrderBy(ct => ct.ContractTypeName)
                .ToListAsync();

            var result = contractTypes.Select(ct => new ContractTypeReadDto
            {
                Id = ct.Id,
                ContractTypeName = ct.ContractTypeName,
                CompanyId = ct.CompanyId,
                LegalContractTypeId = ct.LegalContractTypeId,
                StateEmploymentProgramId = ct.StateEmploymentProgramId,
                LegalContractTypeName = ct.LegalContractType?.Name,
                StateEmploymentProgramName = ct.StateEmploymentProgram?.Name,
                CreatedAt = ct.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// Crée un nouveau type de contrat
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ContractTypeReadDto>> Create([FromBody] ContractTypeCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            // Vérifier que la société existe
            var companyExists = await _db.Companies
                .AnyAsync(c => c.Id == dto.CompanyId && c.DeletedAt == null);

            if (!companyExists)
                return NotFound(new { Message = "Société non trouvée" });

            // Vérifier que le nom du type de contrat n'existe pas déjà pour cette société
            var nameExists = await _db.ContractTypes
                .AnyAsync(ct => ct.ContractTypeName == dto.ContractTypeName && 
                               ct.CompanyId == dto.CompanyId && 
                               ct.DeletedAt == null);

            if (nameExists)
                return Conflict(new { Message = "Un type de contrat avec ce nom existe déjà pour cette société" });

            var contractType = new ContractType
            {
                ContractTypeName = dto.ContractTypeName,
                CompanyId = dto.CompanyId,
                LegalContractTypeId = dto.LegalContractTypeId,
                StateEmploymentProgramId = dto.StateEmploymentProgramId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.ContractTypes.Add(contractType);
            await _db.SaveChangesAsync();

            // Récupérer le type de contrat créé avec ses relations
            var createdContractType = await _db.ContractTypes
                .AsNoTracking()
                .Include(ct => ct.Company)
                .Include(ct => ct.LegalContractType)
                .Include(ct => ct.StateEmploymentProgram)
                .FirstAsync(ct => ct.Id == contractType.Id);

            var readDto = new ContractTypeReadDto
            {
                Id = createdContractType.Id,
                ContractTypeName = createdContractType.ContractTypeName,
                CompanyId = createdContractType.CompanyId,
                CompanyName = createdContractType.Company?.CompanyName,
                LegalContractTypeId = createdContractType.LegalContractTypeId,
                StateEmploymentProgramId = createdContractType.StateEmploymentProgramId,
                LegalContractTypeName = createdContractType.LegalContractType?.Name,
                StateEmploymentProgramName = createdContractType.StateEmploymentProgram?.Name,
                CreatedAt = createdContractType.CreatedAt.DateTime
            };

            return CreatedAtAction(nameof(GetById), new { id = contractType.Id }, readDto);
        }

        /// <summary>
        /// Met à jour un type de contrat
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ContractTypeReadDto>> Update(int id, [FromBody] ContractTypeUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            var contractType = await _db.ContractTypes
                .Where(ct => ct.Id == id && ct.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (contractType == null)
                return NotFound(new { Message = "Type de contrat non trouvé" });

            // Mettre à jour le nom si fourni
            if (dto.ContractTypeName != null && dto.ContractTypeName != contractType.ContractTypeName)
            {
                // Vérifier que le nouveau nom n'existe pas déjà pour cette société
                var nameExists = await _db.ContractTypes
                    .AnyAsync(ct => ct.ContractTypeName == dto.ContractTypeName && 
                                   ct.CompanyId == contractType.CompanyId && 
                                   ct.Id != id && 
                                   ct.DeletedAt == null);

                if (nameExists)
                    return Conflict(new { Message = "Un type de contrat avec ce nom existe déjà pour cette société" });

                contractType.ContractTypeName = dto.ContractTypeName;
            }

            // Mise a jour le LegalContractTypeId et StateEmploymentProgramId si fournis
            if (dto.LegalContractTypeId.HasValue)
                contractType.LegalContractTypeId = dto.LegalContractTypeId;

            if (dto.StateEmploymentProgramId.HasValue)
                contractType.StateEmploymentProgramId = dto.StateEmploymentProgramId;

            contractType.UpdatedAt = DateTimeOffset.UtcNow;
            contractType.UpdatedBy = userId;

            await _db.SaveChangesAsync();

            // Récupérer le type de contrat mis à jour avec ses relations
            var updatedContractType = await _db.ContractTypes
                .AsNoTracking()
                .Include(ct => ct.Company)
                .FirstAsync(ct => ct.Id == id);

            var readDto = new ContractTypeReadDto
            {
                Id = updatedContractType.Id,
                ContractTypeName = updatedContractType.ContractTypeName,
                CompanyId = updatedContractType.CompanyId,
                CompanyName = updatedContractType.Company?.CompanyName,
                LegalContractTypeId = updatedContractType.LegalContractTypeId,
                StateEmploymentProgramId = updatedContractType.StateEmploymentProgramId,
                LegalContractTypeName = updatedContractType.LegalContractType?.Name,
                StateEmploymentProgramName = updatedContractType.StateEmploymentProgram?.Name,
                CreatedAt = updatedContractType.CreatedAt.DateTime
            };

            return Ok(readDto);
        }

        /// <summary>
        /// Supprime un type de contrat (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();

            var contractType = await _db.ContractTypes
                .Where(ct => ct.Id == id && ct.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (contractType == null)
                return NotFound(new { Message = "Type de contrat non trouvé" });

            // Vérifier si le type de contrat est utilisé dans des contrats d'employés
            var hasEmployeeContracts = await _db.EmployeeContracts
                .AnyAsync(ec => ec.ContractTypeId == id && ec.DeletedAt == null);

            if (hasEmployeeContracts)
                return BadRequest(new { Message = "Impossible de supprimer ce type de contrat car il est utilisé dans des contrats d'employés" });

            // Soft delete
            contractType.DeletedAt = DateTimeOffset.UtcNow;
            contractType.DeletedBy = userId;

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
