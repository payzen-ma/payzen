using payzen_backend.Models.Leave;
using payzen_backend.Models.Leave.Dtos;
using payzen_backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using payzen_backend.Data;
using payzen_backend.Services;
using payzen_backend.Models.Common.LeaveStatus;

namespace payzen_backend.Controllers.Leave
{
    [Route("api/leave-types")]
    [ApiController]
    //[Authorize]
    public class LeaveTypeController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly LeaveEventLogService _leaveEventLogService;

        public LeaveTypeController(AppDbContext db, LeaveEventLogService leaveEventLogService)
        {
            _db = db;
            _leaveEventLogService = leaveEventLogService;
        }

        /// <summary>
        /// Récupére tous les types de congés
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeaveTypeReadDto>>> GetAll([FromQuery] int? companyId)
        {
            var query = _db.LeaveTypes
                .AsNoTracking()
                .Where(lt => lt.DeletedAt == null);

            if (companyId.HasValue)
            {
                query = query.Where(lt =>
                    lt.Scope == LeaveScope.Global || lt.CompanyId == companyId.Value
                );
            }
            else
            {
                query = query.Where(lt => lt.Scope == LeaveScope.Global);
            }

            var leaveTypes = await query
                .Include(lt => lt.Company)
                .OrderBy(lt => lt.LeaveCode)
                .Select(lt => new LeaveTypeReadDto
                {
                    Id = lt.Id,
                    LeaveCode = lt.LeaveCode,
                    LeaveName = lt.LeaveNameFr,
                    LeaveDescription = lt.LeaveDescription,
                    Scope = lt.Scope,
                    CompanyId = lt.CompanyId,
                    CompanyName = lt.Company != null ? lt.Company.CompanyName : string.Empty,
                    IsActive = lt.IsActive,
                    CreatedAt = lt.CreatedAt
                })
                .ToListAsync();

            return Ok(leaveTypes);
        }


        /// <summary>
        /// Récupère un type de congé par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveTypeReadDto>> GetById(int id)
        {
            var leaveType = await _db.LeaveTypes
                .AsNoTracking()
                .Where(lt => lt.Id == id && lt.DeletedAt == null)
                .Include(lt => lt.Company)
                .Select(lt => new LeaveTypeReadDto
                {
                    Id = lt.Id,
                    LeaveCode = lt.LeaveCode,
                    LeaveName = lt.LeaveNameFr,
                    LeaveDescription = lt.LeaveDescription,
                    Scope = lt.Scope,
                    CompanyId = lt.CompanyId,
                    CompanyName = lt.Company != null ? lt.Company.CompanyName : string.Empty,
                    IsActive = lt.IsActive,
                    CreatedAt = lt.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (leaveType == null)
                return NotFound(new { Message = "Type de congé non trouvé" });

            return Ok(leaveType);
        }

        /// <summary>
        /// Crée un nouveau type de congé
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LeaveTypeReadDto>> Create([FromBody] LeaveTypeCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validation métier: Si Scope = Company, CompanyId obligatoire
            if (dto.Scope == LeaveScope.Company && dto.CompanyId == null)
            {
                return BadRequest(new { Message = "CompanyId est obligatoire pour les types de congés d'entreprise (Scope=Company)" });
            }

            // Validation métier: Si Scope = Global, CompanyId doit être null
            if (dto.Scope == LeaveScope.Global && dto.CompanyId != null)
            {
                return BadRequest(new { Message = "CompanyId doit être null pour les types de congés globaux (Scope=Global)" });
            }

            // Vérifier l'unicité du LeaveCode par scope
            bool codeExists;
            if (dto.Scope == LeaveScope.Global)
            {
                codeExists = await _db.LeaveTypes
                    .AnyAsync(lt => lt.LeaveCode == dto.LeaveCode && lt.Scope == LeaveScope.Global && lt.DeletedAt == null);
            }
            else
            {
                codeExists = await _db.LeaveTypes
                    .AnyAsync(lt => lt.LeaveCode == dto.LeaveCode && lt.CompanyId == dto.CompanyId && lt.DeletedAt == null);
            }

            if (codeExists)
            {
                return Conflict(new { Message = $"Un type de congé avec le code '{dto.LeaveCode}' existe déjà dans ce contexte" });
            }

            var userId = User.GetUserId();

            var leaveType = new LeaveType
            {
                LeaveCode = dto.LeaveCode.Trim(),
                LeaveNameFr = dto.LeaveName.Trim(),
                LeaveNameEn = dto.LeaveName.Trim(), // Par défaut, même nom
                LeaveNameAr = dto.LeaveName.Trim(), // Par défaut, même nom
                LeaveDescription = dto.LeaveDescription.Trim(),
                Scope = dto.Scope,
                CompanyId = dto.CompanyId,
                IsActive = dto.IsActive,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.LeaveTypes.Add(leaveType);
            await _db.SaveChangesAsync();

            // Logger l'événement
            var companyId = dto.CompanyId ?? 0; // Si Global, mettre 0
            await _leaveEventLogService.LogSimpleEventAsync(
                companyId,
                LeaveEventLogService.EventNames.LeaveTypeCreated,
                null,
                $"Type de congé '{leaveType.LeaveCode}' créé",
                userId
            );

            var readDto = new LeaveTypeReadDto
            {
                Id = leaveType.Id,
                LeaveCode = leaveType.LeaveCode,
                LeaveName = leaveType.LeaveNameFr,
                LeaveDescription = leaveType.LeaveDescription,
                Scope = leaveType.Scope,
                CompanyId = leaveType.CompanyId,
                CompanyName = string.Empty,
                IsActive = leaveType.IsActive,
                CreatedAt = leaveType.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = leaveType.Id }, readDto);
        }

        /// <summary>
        /// Met à jour un type de congé
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<LeaveTypeReadDto>> Update(int id, [FromBody] LeaveTypePatchDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var leaveType = await _db.LeaveTypes
                .Include(lt => lt.Company)
                .FirstOrDefaultAsync(lt => lt.Id == id && lt.DeletedAt == null);

            if (leaveType == null)
                return NotFound(new { Message = "Type de congé non trouvé" });

            var userId = User.GetUserId();
            var changes = new List<string>();

            // Mise à jour du LeaveCode avec vérification d'unicité
            if (!string.IsNullOrWhiteSpace(dto.LeaveCode) && dto.LeaveCode != leaveType.LeaveCode)
            {
                bool codeExists;
                if (leaveType.Scope == LeaveScope.Global)
                {
                    codeExists = await _db.LeaveTypes
                        .AnyAsync(lt => lt.Id != id && lt.LeaveCode == dto.LeaveCode && lt.Scope == LeaveScope.Global && lt.DeletedAt == null);
                }
                else
                {
                    codeExists = await _db.LeaveTypes
                        .AnyAsync(lt => lt.Id != id && lt.LeaveCode == dto.LeaveCode && lt.CompanyId == leaveType.CompanyId && lt.DeletedAt == null);
                }

                if (codeExists)
                {
                    return Conflict(new { Message = $"Un type de congé avec le code '{dto.LeaveCode}' existe déjà" });
                }

                changes.Add($"LeaveCode: '{leaveType.LeaveCode}' → '{dto.LeaveCode}'");
                leaveType.LeaveCode = dto.LeaveCode.Trim();
            }

            // Mise à jour du nom
            if (!string.IsNullOrWhiteSpace(dto.LeaveName) && dto.LeaveName != leaveType.LeaveNameFr)
            {
                changes.Add($"LeaveName: '{leaveType.LeaveNameFr}' → '{dto.LeaveName}'");
                leaveType.LeaveNameFr = dto.LeaveName.Trim();
                leaveType.LeaveNameEn = dto.LeaveName.Trim();
                leaveType.LeaveNameAr = dto.LeaveName.Trim();
            }

            // Mise à jour de la description
            if (!string.IsNullOrWhiteSpace(dto.LeaveDescription) && dto.LeaveDescription != leaveType.LeaveDescription)
            {
                changes.Add($"Description modifiée");
                leaveType.LeaveDescription = dto.LeaveDescription.Trim();
            }

            // Mise à jour de l'état actif
            if (dto.IsActive.HasValue && dto.IsActive != leaveType.IsActive)
            {
                changes.Add($"IsActive: {leaveType.IsActive} → {dto.IsActive}");
                leaveType.IsActive = dto.IsActive.Value;
            }

            if (changes.Any())
            {
                leaveType.UpdatedAt = DateTimeOffset.UtcNow;
                leaveType.UpdatedBy = userId;
                await _db.SaveChangesAsync();

                // Logger l'événement
                var companyId = leaveType.CompanyId ?? 0;
                await _leaveEventLogService.LogSimpleEventAsync(
                    companyId,
                    LeaveEventLogService.EventNames.LeaveTypeUpdated,
                    null,
                    string.Join(", ", changes),
                    userId
                );
            }

            var readDto = new LeaveTypeReadDto
            {
                Id = leaveType.Id,
                LeaveCode = leaveType.LeaveCode,
                LeaveName = leaveType.LeaveNameFr,
                LeaveDescription = leaveType.LeaveDescription,
                Scope = leaveType.Scope,
                CompanyId = leaveType.CompanyId,
                CompanyName = leaveType.Company?.CompanyName ?? string.Empty,
                IsActive = leaveType.IsActive,
                CreatedAt = leaveType.CreatedAt
            };

            return Ok(readDto);
        }
            
        /// <summary>
            /// Supprime un type de congé (soft delete)
            /// </summary>
            [HttpDelete("{id}")]
       public async Task<IActionResult> Delete(int id)
            {
                var leaveType = await _db.LeaveTypes
                    .FirstOrDefaultAsync(lt => lt.Id == id && lt.DeletedAt == null);

                if (leaveType == null)
                    return NotFound(new { Message = "Type de congé non trouvé" });

                // Vérifier les dépendances: LeaveRequest
                var hasLeaveRequests = await _db.LeaveRequests
                    .AnyAsync(lr => lr.LeaveTypeId == id && lr.DeletedAt == null);

                if (hasLeaveRequests)
                {
                    return BadRequest(new { Message = "Impossible de supprimer ce type de congé car il est utilisé dans des demandes de congés" });
                }

                // Vérifier les dépendances: LeaveBalance
                var hasLeaveBalances = await _db.LeaveBalances
                    .AnyAsync(lb => lb.LeaveTypeId == id && lb.DeletedAt == null);

                if (hasLeaveBalances)
                {
                    return BadRequest(new { Message = "Impossible de supprimer ce type de congé car il est utilisé dans des soldes de congés" });
                }

                var userId = User.GetUserId();
                leaveType.DeletedAt = DateTimeOffset.UtcNow;
                leaveType.DeletedBy = userId;

                await _db.SaveChangesAsync();

                // Logger l'événement
                var companyId = leaveType.CompanyId ?? 0;
                await _leaveEventLogService.LogSimpleEventAsync(
                    companyId,
                    LeaveEventLogService.EventNames.LeaveTypeDeleted,
                    $"Type de congé '{leaveType.LeaveCode}' supprimé",
                    null,
                    userId
                );

                return NoContent();
            }
        }
    }