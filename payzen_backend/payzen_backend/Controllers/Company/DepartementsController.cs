using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Authorization;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Company;
using payzen_backend.Models.Company.Dtos;
using payzen_backend.Services;

namespace payzen_backend.Controllers.Company
{
    [Route("api/departements")]
    [ApiController]
    [Authorize]
    public class DepartementsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly CompanyEventLogService _companyEventLogService;
        public DepartementsController(AppDbContext db, CompanyEventLogService companyEventLogService)
        {
            _db = db;
            _companyEventLogService = companyEventLogService;
        }

        /// <summary>
        /// R�cup�re tous les d�partements actifs
        /// </summary>
        [HttpGet]
        //[HasPermission("READ_DEPARTEMENTS")]
        public async Task<ActionResult<IEnumerable<DepartementReadDto>>> ReadAll()
        {
            var departements = await _db.Departement
                .AsNoTracking()
                .Where(d => d.DeletedAt == null)
                .Include(d => d.Company)
                .OrderBy(d => d.DepartementName)
                .ToListAsync();

            var result = departements.Select(d => new DepartementReadDto
            {
                Id = d.Id,
                DepartementName = d.DepartementName,
                CompanyId = d.CompanyId,
                CompanyName = d.Company?.CompanyName ?? "",
                CreatedAt = d.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re un d�partement par ID
        /// </summary>
        [HttpGet("{id}")]
        //[HasPermission("VIEW_DEPARTEMENTS")]
        public async Task<ActionResult<DepartementReadDto>> ReadById(int id)
        {
            var departement = await _db.Departement
                .AsNoTracking()
                .Where(d => d.DeletedAt == null)
                .Include(d => d.Company)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (departement == null)
                return NotFound(new { Message = "D�partement non trouv�" });

            var result = new DepartementReadDto
            {
                Id = departement.Id,
                DepartementName = departement.DepartementName,
                CompanyId = departement.CompanyId,
                CompanyName = departement.Company?.CompanyName ?? "",
                CreatedAt = departement.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re tous les d�partements d'une soci�t�
        /// </summary>
        [HttpGet("company/{companyId}")]
        //[HasPermission("READ_DEPARTEMENTS")]
        public async Task<ActionResult<IEnumerable<DepartementReadDto>>> GetByCompanyId(int companyId)
        {
            var companyExists = await _db.Companies.AnyAsync(c => c.Id == companyId && c.DeletedAt == null);
            if (!companyExists)
                return NotFound(new { Message = "Soci�t� non trouv�e" });

            var departements = await _db.Departement
                .AsNoTracking()
                .Where(d => d.CompanyId == companyId && d.DeletedAt == null)
                .Include(d => d.Company)
                .OrderBy(d => d.DepartementName)
                .ToListAsync();

            var result = departements.Select(d => new DepartementReadDto
            {
                Id = d.Id,
                DepartementName = d.DepartementName,
                CompanyId = d.CompanyId,
                CompanyName = d.Company?.CompanyName ?? "",
                CreatedAt = d.CreatedAt.DateTime
            })
            .DistinctBy(d => d.Id)
            .ToList();

            return Ok(result);
        }

        /// <summary>
        /// Cr�e un nouveau d�partement
        /// </summary>
        [HttpPost]
        //[HasPermission("CREATE_DEPARTEMENTS")]
        public async Task<ActionResult<DepartementReadDto>> Create([FromBody] DepartementCreateDto departementDto)
        {
            // Validation du mod�le
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            // V�rifier que la company existe
            var companyExists = await _db.Companies
                .AsNoTracking()
                .AnyAsync(c => c.Id == departementDto.CompanyId && c.DeletedAt == null);

            if (!companyExists)
            {
                return BadRequest(new { Message = "La soci�t� sp�cifi�e n'existe pas" });
            }

            // V�rifier qu'un d�partement avec le m�me nom n'existe pas d�j� pour cette soci�t�
            var departementExists = await _db.Departement
                .AsNoTracking()
                .AnyAsync(d => d.CompanyId == departementDto.CompanyId
                            && d.DepartementName == departementDto.DepartementName
                            && d.DeletedAt == null);

            if (departementExists)
            {
                return Conflict(new { Message = "Un d�partement avec ce nom existe d�j� dans cette soci�t�" });
            }

            try
            {
                var departement = new Departement
                {
                    DepartementName = departementDto.DepartementName,
                    CompanyId = departementDto.CompanyId,
                    CreatedBy = User.GetUserId(),
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _db.Departement.Add(departement);
                await _db.SaveChangesAsync();

                // Log : cr�ation du d�partement (relation -> link)
                await _companyEventLogService.LogRelationEventAsync(
                    departement.CompanyId,
                    "Departement_Created",
                    null,
                    null,
                    departement.Id,
                    departement.DepartementName,
                    User.GetUserId()
                );

                var CompanyName = await _db.Companies
                    .AsNoTracking()
                    .Where(c => c.Id == departement.CompanyId)
                    .Select(c => c.CompanyName)
                    .FirstOrDefaultAsync() ?? "";

                var result = new DepartementReadDto
                {
                    Id = departement.Id,
                    DepartementName = departement.DepartementName,
                    CompanyId = departement.CompanyId,
                    CompanyName = CompanyName,
                    CreatedAt = departement.CreatedAt.DateTime
                };

                return CreatedAtAction(nameof(ReadById), new { id = departement.Id }, result);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Message = "Erreur lors de la cr�ation du d�partement", Details = ex.Message });
            }
        }

        /// <summary>
        /// Met � jour un d�partement
        /// </summary>
        [HttpPut("{id}")]
        //[HasPermission("UPDATE_DEPARTEMENTS")]
        public async Task<ActionResult<DepartementReadDto>> Update(int id, [FromBody] DepartementUpdateDto departementDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var userId = User.GetUserId();

            // R�cup�rer le d�partement � modifier
            var departement = await _db.Departement
                .FirstOrDefaultAsync(d => d.Id == id && d.DeletedAt == null);

            if (departement == null)
                return NotFound(new { Message = "D�partement non trouv�" });

            // Conserver les anciennes valeurs pour les logs
            var oldName = departement.DepartementName;
            var oldCompanyId = departement.CompanyId;

            // Mettre � jour le nom si fourni
            if (departementDto.DepartementName != null && departementDto.DepartementName != departement.DepartementName)
            {
                // V�rifier qu'un d�partement avec ce nom n'existe pas d�j� dans la m�me soci�t�
                var nameExists = await _db.Departement
                    .AsNoTracking()
                    .AnyAsync(d => d.CompanyId == departement.CompanyId
                                && d.DepartementName == departementDto.DepartementName
                                && d.Id != id
                                && d.DeletedAt == null);

                if (nameExists)
                {
                    return Conflict(new { Message = "Un d�partement avec ce nom existe d�j� dans cette soci�t�" });
                }

                departement.DepartementName = departementDto.DepartementName;
            }

            // Mettre � jour la soci�t� si fournie
            if (departementDto.CompanyId.HasValue && departementDto.CompanyId.Value != departement.CompanyId)
            {
                // V�rifier que la nouvelle soci�t� existe
                var companyExists = await _db.Companies
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == departementDto.CompanyId.Value && c.DeletedAt == null);

                if (!companyExists)
                {
                    return BadRequest(new { Message = "La soci�t� sp�cifi�e n'existe pas" });
                }

                // V�rifier si le d�partement a des employ�s
                var hasEmployees = await _db.Employees
                    .AnyAsync(e => e.DepartementId == id && e.DeletedAt == null);

                if (hasEmployees)
                {
                    return BadRequest(new { Message = "Impossible de changer la soci�t� car le d�partement contient des employ�s" });
                }

                // V�rifier qu'un d�partement avec le m�me nom n'existe pas dans la nouvelle soci�t�
                var nameExistsInNewCompany = await _db.Departement
                    .AsNoTracking()
                    .AnyAsync(d => d.CompanyId == departementDto.CompanyId.Value
                                && d.DepartementName == departement.DepartementName
                                && d.Id != id
                                && d.DeletedAt == null);

                if (nameExistsInNewCompany)
                {
                    return Conflict(new { Message = "Un d�partement avec ce nom existe d�j� dans la soci�t� cible" });
                }

                departement.CompanyId = departementDto.CompanyId.Value;
            }

            departement.UpdatedAt = DateTimeOffset.UtcNow;
            departement.UpdatedBy = userId;

            try
            {
                await _db.SaveChangesAsync();

                // Logs apr�s sauvegarde :
                // 1) Si le nom a chang� -> log simple (oldName -> newName)
                if (oldName != departement.DepartementName)
                {
                    await _companyEventLogService.LogEventAsync(
                        departement.CompanyId,
                        "DepartementName_Changed",
                        oldName,
                        null,
                        departement.DepartementName,
                        departement.Id,
                        userId
                    );
                }

                // 2) Si la soci�t� a chang� -> log_unlink sur l'ancienne soci�t� et log_link sur la nouvelle
                if (oldCompanyId != departement.CompanyId)
                {
                    // R�cup�rer les noms des soci�t�s pour plus de clart�
                    var oldCompanyName = await _db.Companies
                        .AsNoTracking()
                        .Where(c => c.Id == oldCompanyId)
                        .Select(c => c.CompanyName)
                        .FirstOrDefaultAsync();

                    var newCompanyName = await _db.Companies
                        .AsNoTracking()
                        .Where(c => c.Id == departement.CompanyId)
                        .Select(c => c.CompanyName)
                        .FirstOrDefaultAsync();

                    // Unlink sur l'ancienne soci�t�
                    await _companyEventLogService.LogRelationEventAsync(
                        oldCompanyId,
                        "Departement_Unlinked",
                        departement.Id,
                        departement.DepartementName,
                        null,
                        null,
                        userId
                    );

                    // Link sur la nouvelle soci�t�
                    await _companyEventLogService.LogRelationEventAsync(
                        departement.CompanyId,
                        "Departement_Linked",
                        null,
                        null,
                        departement.Id,
                        departement.DepartementName,
                        userId
                    );
                }

                // R�cup�rer le d�partement mis � jour avec ses relations
                var updatedDepartement = await _db.Departement
                    .AsNoTracking()
                    .Include(d => d.Company)
                    .FirstAsync(d => d.Id == id);

                var result = new DepartementReadDto
                {
                    Id = updatedDepartement.Id,
                    DepartementName = updatedDepartement.DepartementName,
                    CompanyId = updatedDepartement.CompanyId,
                    CompanyName = updatedDepartement.Company?.CompanyName ?? "",
                    CreatedAt = updatedDepartement.CreatedAt.DateTime,
                };

                return Ok(result);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Message = "Erreur lors de la mise � jour du d�partement", Details = ex.Message });
            }
        }

        /// <summary>
        /// Supprime un d�partement (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        //[HasPermission("DELETE_DEPARTEMENTS")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();

            var departement = await _db.Departement
                .FirstOrDefaultAsync(d => d.Id == id && d.DeletedAt == null);

            if (departement == null)
                return NotFound(new { Message = "D�partement non trouv�" });

            // V�rifier si le d�partement contient des employ�s actifs
            var hasEmployees = await _db.Employees
                .AnyAsync(e => e.DepartementId == id && e.DeletedAt == null);

            if (hasEmployees)
            {
                return BadRequest(new { Message = "Impossible de supprimer ce d�partement car il contient des employ�s actifs" });
            }

            try
            {
                // Soft delete
                departement.DeletedAt = DateTimeOffset.UtcNow;
                departement.DeletedBy = userId;

                await _db.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { Message = "Erreur lors de la suppression du d�partement", Details = ex.Message });
            }
        }
    }
}
