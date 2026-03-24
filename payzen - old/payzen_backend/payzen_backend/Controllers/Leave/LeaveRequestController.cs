using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Common.LeaveStatus;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Leave;
using payzen_backend.Models.Leave.Dtos;
using payzen_backend.Services;
using payzen_backend.Services.Leave;

namespace payzen_backend.Controllers.Leave
{
    [Route("api/leave-requests")]
    [ApiController]
    [Authorize]
    public class LeaveRequestController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly LeaveEventLogService _leaveEventLogService;
        private readonly WorkingDaysCalculator _workingDaysCalculator;
        private readonly LeaveBalanceService _leaveBalanceService;

        public LeaveRequestController(
            AppDbContext db,
            LeaveEventLogService leaveEventLogService,
            WorkingDaysCalculator workingDaysCalculator,
            LeaveBalanceService leaveBalanceService)
        {
            _db = db;
            _leaveEventLogService = leaveEventLogService;
            _workingDaysCalculator = workingDaysCalculator;
            _leaveBalanceService = leaveBalanceService;
        }

        /// <summary>
        /// Crée une nouvelle version de solde (LeaveBalance) pour garder un historique.
        /// L'ancien solde est marqué comme supprimé (soft delete) et un nouveau enregistrement
        /// est inséré avec la même période (Year/Month) et un delta sur UsedDays.
        /// </summary>
        private void VersionLeaveBalance(LeaveBalance currentBalance, decimal usedDaysDelta, int userId)
        {
            // Marquer l'ancien enregistrement comme supprimé (historique)
            currentBalance.DeletedAt = DateTimeOffset.UtcNow;
            currentBalance.DeletedBy = userId;

            var now = DateTimeOffset.UtcNow;

            var newUsedDays = currentBalance.UsedDays + usedDaysDelta;
            var newClosingDays = currentBalance.OpeningDays
                               + currentBalance.AccruedDays
                               + currentBalance.CarryInDays
                               - newUsedDays
                               - currentBalance.CarryOutDays;

            var newBalance = new LeaveBalance
            {
                CompanyId = currentBalance.CompanyId,
                EmployeeId = currentBalance.EmployeeId,
                LeaveTypeId = currentBalance.LeaveTypeId,
                Year = currentBalance.Year,
                Month = currentBalance.Month,

                OpeningDays = currentBalance.OpeningDays,
                AccruedDays = currentBalance.AccruedDays,
                UsedDays = newUsedDays,
                CarryInDays = currentBalance.CarryInDays,
                CarryOutDays = currentBalance.CarryOutDays,
                ClosingDays = newClosingDays,

                CarryoverExpiresOn = currentBalance.CarryoverExpiresOn,
                LastRecalculatedAt = currentBalance.LastRecalculatedAt,

                CreatedAt = now,
                CreatedBy = userId,
                ModifiedAt = now,
                ModifiedBy = userId
            };

            _db.LeaveBalances.Add(newBalance);
        }

        /// <summary>
        /// Récupère toutes les demandes de congés avec filtres
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeaveRequestReadDto>>> GetAll(
            [FromQuery] int? companyId = null,
            [FromQuery] int? employeeId = null,
            [FromQuery] LeaveRequestStatus? status = null)
        {
            var query = _db.LeaveRequests
                .AsNoTracking()
                .Where(lr => lr.DeletedAt == null);

            if (companyId.HasValue)
            {
                query = query.Where(lr => lr.CompanyId == companyId.Value);
            }

            if (employeeId.HasValue)
            {
                query = query.Where(lr => lr.EmployeeId == employeeId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(lr => lr.Status == status.Value);
            }

            var requests = await query
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.LegalRule)
                .OrderByDescending(lr => lr.RequestedAt)
                .Select(lr => MapToReadDto(lr))
                .ToListAsync();

            return Ok(requests);
        }

        /// <summary>
        /// Récupère une demande par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveRequestReadDto>> GetById(int id)
        {
            var request = await _db.LeaveRequests
                .AsNoTracking()
                .Where(lr => lr.Id == id && lr.DeletedAt == null)
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.LegalRule)
                .FirstOrDefaultAsync();

            if (request == null)
                return NotFound(new { Message = "Demande de congé non trouvée" });

            return Ok(MapToReadDto(request));
        }

        /// <summary>
        /// Récupère les demandes d'un employé
        /// </summary>
        [HttpGet("employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<LeaveRequestReadDto>>> GetByEmployee(int employeeId)
        {
            var requests = await _db.LeaveRequests
                .AsNoTracking()
                .Where(lr => lr.EmployeeId == employeeId && lr.DeletedAt == null)
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.Policy)
                .Include(lr => lr.LegalRule)
                .OrderByDescending(lr => lr.RequestedAt)
                .Select(lr => MapToReadDto(lr))
                .ToListAsync();

            return Ok(requests);
        }

        /// <summary>
        /// Récupère les demandes en attente d'approbation
        /// </summary>
        [HttpGet("pending-approval")]
        public async Task<ActionResult<IEnumerable<LeaveRequestReadDto>>> GetPendingApproval([FromQuery] int? companyId = null)
        {
            var query = _db.LeaveRequests
                .AsNoTracking()
                .Where(lr => lr.Status == LeaveRequestStatus.Submitted && lr.DeletedAt == null);

            if (companyId.HasValue)
            {
                query = query.Where(lr => lr.CompanyId == companyId.Value);
            }

            var requests = await query
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.LegalRule)
                .Include(lr => lr.Employee)
                .OrderBy(lr => lr.SubmittedAt)
                .Select(lr => MapToReadDto(lr))
                .ToListAsync();

            return Ok(requests);
        }

        /// <summary>
        /// Crée une nouvelle demande de congé (statut Draft)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LeaveRequestReadDto>> Create([FromBody] LeaveRequestCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            // Vérifier si l'utilisateur est RH ou Admin pour approbation automatique
            var currentUserForRole = await _db.Users
                .AsNoTracking()
                .Include(u => u.UsersRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            bool isRhOrAdmin = currentUserForRole?.UsersRoles?.Any(ur =>
                ur.Role.Name.Equals("RH", StringComparison.OrdinalIgnoreCase) ||
                ur.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            ) ?? false;

            // Récupérer l'employé pour obtenir le CompanyId
            var employee = await _db.Employees
                .Include(e => e.Contracts)
                .FirstOrDefaultAsync(e => e.Id == userId && e.DeletedAt == null);

            if (employee == null)
            {
                return NotFound(new { Message = "Employé non trouvé" });
            }

            // Validation: StartDate < EndDate
            if (dto.StartDate >= dto.EndDate)
            {
                return BadRequest(new { Message = "La date de début doit être antérieure à la date de fin" });
            }

            // Vérifier que le LeaveType existe
            var leaveType = await _db.LeaveTypes
                .FirstOrDefaultAsync(lt => lt.Id == dto.LeaveTypeId && lt.DeletedAt == null);

            if (leaveType == null)
            {
                return NotFound(new { Message = "Type de congé non trouvé" });
            }

            // Vérifier le LeaveTypePlolicy
            var leavePolicy = await _db.LeaveTypePolicies
                .FirstOrDefaultAsync(lp => lp.LeaveTypeId == dto.LeaveTypeId && lp.CompanyId == employee.CompanyId && lp.DeletedAt == null);

            // Vérifier l'existence de la politique de congé
            if (leavePolicy == null)
            {
                return BadRequest(new { Message = "Aucune politique de congé définie pour ce type de congé dans l'entreprise" });
            }

            // Vérifier l'éligibilité 6 mois si requis
            if (leavePolicy.RequiresEligibility6Months)
            {
                var firstContract = employee.Contracts
                    .OrderBy(c => c.StartDate)
                    .FirstOrDefault(c => c.DeletedAt == null);

                if (firstContract == null)
                    return BadRequest(new { Message = "Aucun contrat actif trouvé pour l'employé" });

                var employmentDate = DateOnly.FromDateTime(firstContract.StartDate);

                var sixMonthsLater = employmentDate.AddMonths(6);

                if (DateOnly.FromDateTime(DateTime.UtcNow) < sixMonthsLater)
                {
                    return BadRequest(new { Message = "Vous devez avoir au moins 6 mois d'ancienneté pour ce type de congé" });
                }
            }

            // Calculer les jours calendaires
            var calendarDays = dto.EndDate.DayNumber - dto.StartDate.DayNumber + 1;

            // Calculer les jours ouvrables (simplifié - en pratique, utiliser le calendrier de travail)
            var workingDays = await _workingDaysCalculator.CalculateWorkingDaysAsync(employee.CompanyId, dto.StartDate, dto.EndDate);

            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                CompanyId = employee.CompanyId,
                LeaveTypeId = dto.LeaveTypeId,
                PolicyId = leavePolicy.Id,
                LegalRuleId = dto.LegalRuleId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = isRhOrAdmin ? LeaveRequestStatus.Approved : LeaveRequestStatus.Draft,
                SubmittedAt = isRhOrAdmin ? DateTimeOffset.UtcNow : null,
                DecisionAt = isRhOrAdmin ? DateTimeOffset.UtcNow : null,
                DecisionBy = isRhOrAdmin ? userId : null,
                DecisionComment = isRhOrAdmin ? "Approbation automatique (RH/Admin)" : null,
                RequestedAt = DateTimeOffset.UtcNow,
                CalendarDays = calendarDays,
                WorkingDaysDeducted = workingDays,
                EmployeeNote = dto.EmployeeNote?.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.LeaveRequests.Add(request);
            await _db.SaveChangesAsync();

            // Approbation automatique si RH/Admin : historique + déduction du solde
            if (isRhOrAdmin)
            {
                _db.LeaveRequestApprovalHistories.Add(new LeaveRequestApprovalHistory
                {
                    LeaveRequestId = request.Id,
                    Action = LeaveApprovalAction.Approved,
                    ActionAt = DateTimeOffset.UtcNow,
                    ActionBy = userId,
                    Comment = "Approbation automatique (RH/Admin)"
                });

                if (leavePolicy.RequiresBalance && !dto.LegalRuleId.HasValue)
                {
                    var balance = await _leaveBalanceService.GetOrCreateBalanceForMonthAsync(
                        request.CompanyId,
                        request.EmployeeId,
                        request.LeaveTypeId,
                        request.StartDate.Year,
                        request.StartDate.Month,
                        userId);

                    if (balance != null)
                    {
                        VersionLeaveBalance(balance, request.WorkingDaysDeducted, userId);
                    }
                }

                await _db.SaveChangesAsync();
            }

            // Logger l'événement
            await _leaveEventLogService.LogLeaveRequestEventAsync(
                employee.CompanyId,
                employee.Id,
                request.Id,
                LeaveEventLogService.EventNames.RequestCreated,
                null,
                $"Demande créée: {dto.StartDate} - {dto.EndDate}",
                userId
            );

            // Recharger pour avoir les relations
            var createdRequest = await _db.LeaveRequests
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.LegalRule)
                .FirstAsync(lr => lr.Id == request.Id);

            return CreatedAtAction(nameof(GetById), new { id = request.Id }, MapToReadDto(createdRequest));
        }

        /// <summary
        /// Crée une nouvelle demande de congé  par Rh/ Manager pour l'employee
        /// </summary>
        [HttpPost("create-for-employee/{employeeId}")]
        public async Task<ActionResult<LeaveRequestReadDto>> CreateForEmployee (int employeeId, [FromBody] LeaveRequestCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get the User authentifié (RH/Manager)
            var userId = User.GetUserId();

            // Récupérer l'employé pour obtenir le CompanyId
            var employee = await _db.Employees
                .Include(e => e.Contracts)
                .FirstOrDefaultAsync(e => e.Id == employeeId && e.DeletedAt == null);

            if (employee == null)
                return NotFound(new { Message = "Employé non trouvé" });

            // Verifier que l'utilisateur est l'employe ou son manager

            var currentUser = await _db.Users
                .AsNoTracking()
                .Include(u => u.UsersRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            bool isRH = currentUser?.UsersRoles?.Any(ur =>
                ur.Role.Name.Equals("RH", StringComparison.OrdinalIgnoreCase)
            ) ?? false;

            bool isAdminRole = currentUser?.UsersRoles?.Any(ur =>
                ur.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase)
            ) ?? false;

            bool isRhOrAdmin = isRH || isAdminRole;

            // Vérifier que l'utilisateur est le manager de l'employéID
            var isManager = await _db.Employees
                .AsNoTracking()
                .AnyAsync(e =>
                    e.Id == employeeId &&
                    e.ManagerId == userId
                );
            if (!isRhOrAdmin && !isManager)
                return BadRequest(new { Message = "Vous devez être RH, Admin ou Manager pour créer un congé pour cet utilisateur" });

            // Validation: StartDate < EndDate
            if (dto.StartDate >= dto.EndDate)
                return BadRequest(new { Message = "La date de début doit être antérieure à la date de fin" });

            // Vérifier que le LeaveType existe
            var leaveType = await _db.LeaveTypes
                .FirstOrDefaultAsync(lt => lt.Id == dto.LeaveTypeId && lt.DeletedAt == null);
            if (leaveType == null)
                return BadRequest (new { Message = "Type de congé non trouvé" });

            // Vérifier le LeaveTypePlolicy
            
            var leavePolicy = await _db.LeaveTypePolicies
                .FirstOrDefaultAsync(lp => lp.LeaveTypeId == dto.LeaveTypeId && lp.CompanyId == employee.CompanyId && lp.DeletedAt == null);
            if (leavePolicy == null)
                return BadRequest(new { Message = "Aucune politique de congé définie pour ce type de congé dans l'entreprise" });

            // Vérifier l'éligibilité 6 mois si requis
            if (leavePolicy.RequiresEligibility6Months)
            {
                var firstContract = employee.Contracts
                    .OrderBy(c => c.StartDate)
                    .FirstOrDefault(c => c.DeletedAt == null);
                if (firstContract == null)
                    return BadRequest(new { Message = "Aucun contrat actif trouvé pour l'employé" });
                var employmentDate = DateOnly.FromDateTime(firstContract.StartDate);
                var sixMonthsLater = employmentDate.AddMonths(6);
                if (DateOnly.FromDateTime(DateTime.UtcNow) < sixMonthsLater)
                    return BadRequest(new { Message = "L'employé doit avoir au moins 6 mois d'ancienneté pour ce type de congé" });
            }

            // Calculer les jours calendaires
            var calendarDays = dto.EndDate.DayNumber - dto.StartDate.DayNumber + 1;

            // Calculer les jours ouvrables (simplifié - en pratique, utiliser le calendrier de travail)

            var workingDays = await _workingDaysCalculator.CalculateWorkingDaysAsync(employee.CompanyId, dto.StartDate, dto.EndDate);

            var request = new LeaveRequest
            {
                EmployeeId = employee.Id,
                CompanyId = employee.CompanyId,
                LeaveTypeId = dto.LeaveTypeId,
                PolicyId = leavePolicy.Id,
                LegalRuleId = dto.LegalRuleId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = isRhOrAdmin ? LeaveRequestStatus.Approved : LeaveRequestStatus.Draft,
                SubmittedAt = isRhOrAdmin ? DateTimeOffset.UtcNow : null,
                DecisionAt = isRhOrAdmin ? DateTimeOffset.UtcNow : null,
                DecisionBy = isRhOrAdmin ? userId : null,
                DecisionComment = isRhOrAdmin ? "Approbation automatique (RH/Admin)" : null,
                RequestedAt = DateTimeOffset.UtcNow,
                CalendarDays = calendarDays,
                WorkingDaysDeducted = workingDays,
                EmployeeNote = dto.EmployeeNote?.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.LeaveRequests.Add(request);
            await _db.SaveChangesAsync();

            // Approbation automatique si RH/Admin : historique + déduction du solde
            if (isRhOrAdmin)
            {
                _db.LeaveRequestApprovalHistories.Add(new LeaveRequestApprovalHistory
                {
                    LeaveRequestId = request.Id,
                    Action = LeaveApprovalAction.Approved,
                    ActionAt = DateTimeOffset.UtcNow,
                    ActionBy = userId,
                    Comment = "Approbation automatique (RH/Admin)"
                });

                if (leavePolicy.RequiresBalance && !dto.LegalRuleId.HasValue)
                {
                    var balance = await _leaveBalanceService.GetOrCreateBalanceForMonthAsync(
                        request.CompanyId,
                        request.EmployeeId,
                        request.LeaveTypeId,
                        request.StartDate.Year,
                        request.StartDate.Month,
                        userId);

                    if (balance != null)
                    {
                        VersionLeaveBalance(balance, request.WorkingDaysDeducted, userId);
                    }
                }

                await _db.SaveChangesAsync();
            }

            // Logger l'événement
            await _leaveEventLogService.LogLeaveRequestEventAsync(
                employee.CompanyId,
                employee.Id,
                request.Id,
                LeaveEventLogService.EventNames.RequestCreated,
                null,
                $"Demande créée: {dto.StartDate} - {dto.EndDate}",
                userId
            );

            return Ok();
        }

        /// <summary>
        /// Met à jour une demande (seulement si Draft)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<LeaveRequestReadDto>> Update(int id, [FromBody] LeaveRequestPatchDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var request = await _db.LeaveRequests
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.LegalRule)
                .FirstOrDefaultAsync(lr => lr.Id == id && lr.DeletedAt == null);

            if (request == null)
                return NotFound(new { Message = "Demande de congé non trouvée" });

            // Vérifier que la demande est en Draft
            if (request.Status != LeaveRequestStatus.Draft)
            {
                return BadRequest(new { Message = "Seules les demandes en brouillon peuvent être modifiées" });
            }

            var userId = User.GetUserId();
            var changes = new List<string>();

            // Mise à jour des dates
            if (dto.StartDate.HasValue && dto.StartDate != request.StartDate)
            {
                changes.Add($"StartDate: {request.StartDate} → {dto.StartDate}");
                request.StartDate = dto.StartDate.Value;
            }

            if (dto.EndDate.HasValue && dto.EndDate != request.EndDate)
            {
                changes.Add($"EndDate: {request.EndDate} → {dto.EndDate}");
                request.EndDate = dto.EndDate.Value;
            }

            // Validation: StartDate < EndDate
            if (request.StartDate >= request.EndDate)
            {
                return BadRequest(new { Message = "La date de début doit être antérieure à la date de fin" });
            }

            // Recalculer les jours si les dates ont changé
            if (dto.StartDate.HasValue || dto.EndDate.HasValue)
            {
                request.CalendarDays = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;

                // Utiliser le companyId de la demande + les dates effectives
                request.WorkingDaysDeducted = await _workingDaysCalculator.CalculateWorkingDaysAsync(
                    request.CompanyId,
                    request.StartDate,
                    request.EndDate
                );
            }

            // Mise à jour des notes
            if (dto.EmployeeNote != null && dto.EmployeeNote != request.EmployeeNote)
            {
                changes.Add("EmployeeNote modifiée");
                request.EmployeeNote = dto.EmployeeNote.Trim();
            }

            if (changes.Any())
            {
                request.ModifiedAt = DateTimeOffset.UtcNow;
                request.ModifiedBy = userId;
                await _db.SaveChangesAsync();

                // Logger l'événement
                await _leaveEventLogService.LogLeaveRequestEventAsync(
                    request.CompanyId,
                    request.EmployeeId,
                    request.Id,
                    LeaveEventLogService.EventNames.RequestUpdated,
                    null,
                    string.Join(", ", changes),
                    userId
                );
            }

            return Ok(MapToReadDto(request));
        }

        /// <summary>
        /// Soumet une demande (Draft → Submitted)
        /// - Recompute/Check balance as-of request.StartDate si policy.RequiresBalance
        /// - Bloque les chevauchements avec demandes Submitted/Approved
        /// </summary>
        [HttpPost("{id}/submit")]
        public async Task<ActionResult<LeaveRequestReadDto>> Submit(int id, CancellationToken ct)
        {
            var request = await _db.LeaveRequests
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.LegalRule)
                .FirstOrDefaultAsync(lr => lr.Id == id && lr.DeletedAt == null, ct);

            if (request == null)
                return NotFound(new { Message = "Demande de congé non trouvée" });

            if (request.Status != LeaveRequestStatus.Draft)
                return BadRequest(new { Message = "Seules les demandes en brouillon peuvent être soumises" });

            // (Optionnel) Si tu veux empêcher une demande incohérente en DB
            if (request.StartDate > request.EndDate)
                return BadRequest(new { Message = "Période invalide (StartDate > EndDate)" });

            var userId = User.GetUserId();

            // 1) Check solde (GetOrCreate + Recalculate as-of StartDate) si policy.RequiresBalance
            // Les congés légaux (LegalRuleId renseigné : mariage, décès, naissance, etc.) ne consomment pas le solde → pas de vérification
            if (!request.LegalRuleId.HasValue)
            {
                var balanceCheck = await _leaveBalanceService.CheckSufficientBalanceForSubmitAsync(
                    companyId: request.CompanyId,
                    employeeId: request.EmployeeId,
                    leaveTypeId: request.LeaveTypeId,
                    requestStartDate: request.StartDate,
                    requestedWorkingDays: request.WorkingDaysDeducted,
                    userId: userId,
                    ct: ct);

                if (!balanceCheck.Success)
                    return BadRequest(new { Message = balanceCheck.ErrorMessage });
            }

            // 2) Vérifier chevauchements avec demandes déjà Submitted/Approved
            // (bloque double soumission en parallèle)
            var hasOverlap = await _db.LeaveRequests.AnyAsync(lr =>
                lr.Id != id &&
                lr.EmployeeId == request.EmployeeId &&
                lr.DeletedAt == null &&
                (lr.Status == LeaveRequestStatus.Submitted || lr.Status == LeaveRequestStatus.Approved) &&
                (lr.StartDate <= request.EndDate && lr.EndDate >= request.StartDate), ct);

            if (hasOverlap)
                return BadRequest(new { Message = "Cette période chevauche une demande déjà soumise ou approuvée" });

            // 3) Passer en Submitted + historique
            request.Status = LeaveRequestStatus.Submitted;
            request.SubmittedAt = DateTimeOffset.UtcNow;
            request.ModifiedAt = DateTimeOffset.UtcNow;
            request.ModifiedBy = userId;

            _db.LeaveRequestApprovalHistories.Add(new LeaveRequestApprovalHistory
            {
                LeaveRequestId = request.Id,
                Action = LeaveApprovalAction.Submitted,
                ActionAt = DateTimeOffset.UtcNow,
                ActionBy = userId
            });

            await _db.SaveChangesAsync(ct);

            // 4) Event log
            await _leaveEventLogService.LogLeaveRequestEventAsync(
                request.CompanyId,
                request.EmployeeId,
                request.Id,
                LeaveEventLogService.EventNames.RequestSubmitted,
                "Draft",
                "Submitted",
                userId);

            return Ok(MapToReadDto(request));
        }

        /// <summary>
        /// Approuve une demande (Submitted → Approved)
        /// </summary>
        [HttpPost("{id}/approve")]
        public async Task<ActionResult<LeaveRequestReadDto>> Approve(int id, [FromBody] ApprovalDto dto)
        {
            var request = await _db.LeaveRequests
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.LegalRule)
                .Include(lr => lr.Policy)
                .FirstOrDefaultAsync(lr => lr.Id == id && lr.DeletedAt == null);

            if (request == null)
                return NotFound(new { Message = "Demande de congé non trouvée" });

            if (request.Status != LeaveRequestStatus.Submitted)
            {
                return BadRequest(new { Message = "Seules les demandes soumises peuvent être approuvées" });
            }

            var userId = User.GetUserId();
            request.Status = LeaveRequestStatus.Approved;
            request.DecisionAt = DateTimeOffset.UtcNow;
            request.DecisionBy = userId;
            request.DecisionComment = dto.Comment?.Trim();
            request.ModifiedAt = DateTimeOffset.UtcNow;
            request.ModifiedBy = userId;

            // Ajouter à l'historique
            var history = new LeaveRequestApprovalHistory
            {
                LeaveRequestId = request.Id,
                Action = LeaveApprovalAction.Approved,
                ActionAt = DateTimeOffset.UtcNow,
                ActionBy = userId,
                Comment = dto.Comment
            };
            _db.LeaveRequestApprovalHistories.Add(history);

            // Déduire du solde du mois de début du congé (Year, Month) — pas pour les congés légaux (LegalRuleId renseigné)
            if (request.Policy != null && request.Policy.RequiresBalance && !request.LegalRuleId.HasValue)
            {
                var balance = await _leaveBalanceService.GetOrCreateBalanceForMonthAsync(
                    request.CompanyId,
                    request.EmployeeId,
                    request.LeaveTypeId,
                    request.StartDate.Year,
                    request.StartDate.Month,
                    userId);

                if (balance != null)
                {
                    // Créer une nouvelle version de solde pour garder l'historique.
                    VersionLeaveBalance(balance, request.WorkingDaysDeducted, userId);
                }
            }

            await _db.SaveChangesAsync();

            // Logger l'événement
            await _leaveEventLogService.LogLeaveRequestEventAsync(
                request.CompanyId,
                request.EmployeeId,
                request.Id,
                LeaveEventLogService.EventNames.RequestApproved,
                "Submitted",
                "Approved",
                userId
            );

            return Ok(MapToReadDto(request));
        }

        /// <summary>
        /// Rejette une demande (Submitted → Rejected)
        /// </summary>
        [HttpPost("{id}/reject")]
        public async Task<ActionResult<LeaveRequestReadDto>> Reject(int id, [FromBody] ApprovalDto dto)
        {
            var request = await _db.LeaveRequests
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.LegalRule)
                .FirstOrDefaultAsync(lr => lr.Id == id && lr.DeletedAt == null);

            if (request == null)
                return NotFound(new { Message = "Demande de congé non trouvée" });

            if (request.Status != LeaveRequestStatus.Submitted)
            {
                return BadRequest(new { Message = "Seules les demandes soumises peuvent être rejetées" });
            }

            var userId = User.GetUserId();
            request.Status = LeaveRequestStatus.Rejected;
            request.DecisionAt = DateTimeOffset.UtcNow;
            request.DecisionBy = userId;
            request.DecisionComment = dto.Comment?.Trim();
            request.ModifiedAt = DateTimeOffset.UtcNow;
            request.ModifiedBy = userId;

            // Ajouter à l'historique
            var history = new LeaveRequestApprovalHistory
            {
                LeaveRequestId = request.Id,
                Action = LeaveApprovalAction.Rejected,
                ActionAt = DateTimeOffset.UtcNow,
                ActionBy = userId,
                Comment = dto.Comment
            };
            _db.LeaveRequestApprovalHistories.Add(history);

            await _db.SaveChangesAsync();

            // Logger l'événement
            await _leaveEventLogService.LogLeaveRequestEventAsync(
                request.CompanyId,
                request.EmployeeId,
                request.Id,
                LeaveEventLogService.EventNames.RequestRejected,
                "Submitted",
                "Rejected",
                userId
            );

            return Ok(MapToReadDto(request));
        }

        /// <summary>
        /// Annule une demande (Approved / Submitted → Cancelled)
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<ActionResult<LeaveRequestReadDto>> Cancel(int id, [FromBody] ApprovalDto dto)
        {
            var request = await _db.LeaveRequests
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.LegalRule)
                .Include(lr => lr.Policy)
                .FirstOrDefaultAsync(lr => lr.Id == id && lr.DeletedAt == null);

            if (request == null)
                return NotFound(new { Message = "Demande de congé non trouvée" });

            if (request.Status != LeaveRequestStatus.Approved && request.Status != LeaveRequestStatus.Submitted)
            {
                return BadRequest(new { Message = "Seules les demandes approuvées ou soumise peuvent être annulées" });
            }

            var userId = User.GetUserId();
            request.Status = LeaveRequestStatus.Cancelled;
            request.ModifiedAt = DateTimeOffset.UtcNow;
            request.ModifiedBy = userId;

            // Ajouter à l'historique
            var history = new LeaveRequestApprovalHistory
            {
                LeaveRequestId = request.Id,
                Action = LeaveApprovalAction.Cancelled,
                ActionAt = DateTimeOffset.UtcNow,
                ActionBy = userId,
                Comment = dto.Comment
            };
            _db.LeaveRequestApprovalHistories.Add(history);

            // Rembourser le solde du mois de début du congé (Year, Month) — uniquement si on avait déduit (pas congé légal)
            if (request.Policy != null && request.Policy.RequiresBalance && !request.LegalRuleId.HasValue)
            {
                var balance = await _leaveBalanceService.GetOrCreateBalanceForMonthAsync(
                    request.CompanyId,
                    request.EmployeeId,
                    request.LeaveTypeId,
                    request.StartDate.Year,
                    request.StartDate.Month,
                    userId);

                if (balance != null)
                {
                    // Versionner le solde avec un delta négatif (remboursement).
                    VersionLeaveBalance(balance, -request.WorkingDaysDeducted, userId);
                }
            }

            await _db.SaveChangesAsync();

            // Logger l'événement
            await _leaveEventLogService.LogLeaveRequestEventAsync(
                request.CompanyId,
                request.EmployeeId,
                request.Id,
                LeaveEventLogService.EventNames.RequestCancelled,
                "Approved/Submitted",
                "Cancelled",
                userId
            );

            return Ok(MapToReadDto(request));
        }

        /// <summary>
        /// Renonce à une demande (Approved → Renounced)
        /// </summary>
        [HttpPost("{id}/renounce")]
        public async Task<ActionResult<LeaveRequestReadDto>> Renounce(int id)
        {
            var request = await _db.LeaveRequests
                .Include(lr => lr.LeaveType)
                .Include(lr => lr.LegalRule)
                .Include(lr => lr.Policy)
                .FirstOrDefaultAsync(lr => lr.Id == id && lr.DeletedAt == null);

            if (request == null)
                return NotFound(new { Message = "Demande de congé non trouvée" });

            if (request.Status != LeaveRequestStatus.Approved)
            {
                return BadRequest(new { Message = "Seules les demandes approuvées peuvent faire l'objet d'une renonciation" });
            }

            var userId = User.GetUserId();
            request.Status = LeaveRequestStatus.Renounced;
            request.IsRenounced = true;
            request.ModifiedAt = DateTimeOffset.UtcNow;
            request.ModifiedBy = userId;

            // Rembourser le solde du mois de début du congé (Year, Month) — uniquement si on avait déduit (pas congé légal)
            if (request.Policy != null && request.Policy.RequiresBalance && !request.LegalRuleId.HasValue)
            {
                var balance = await _leaveBalanceService.GetOrCreateBalanceForMonthAsync(
                    request.CompanyId,
                    request.EmployeeId,
                    request.LeaveTypeId,
                    request.StartDate.Year,
                    request.StartDate.Month,
                    userId);

                if (balance != null)
                {
                    // Versionner le solde avec un delta négatif (remboursement).
                    VersionLeaveBalance(balance, -request.WorkingDaysDeducted, userId);
                }
            }

            await _db.SaveChangesAsync();

            // Logger l'événement
            await _leaveEventLogService.LogLeaveRequestEventAsync(
                request.CompanyId,
                request.EmployeeId,
                request.Id,
                LeaveEventLogService.EventNames.RequestRenounced,
                "Approved",
                "Renounced",
                userId
            );

            return Ok(MapToReadDto(request));
        }

        /// <summary>
        /// Supprime une demande (soft delete, seulement si Draft)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _db.LeaveRequests
                .FirstOrDefaultAsync(lr => lr.Id == id && lr.DeletedAt == null);

            if (request == null)
                return NotFound(new { Message = "Demande de congé non trouvée" });

            if (request.Status != LeaveRequestStatus.Draft)
            {
                return BadRequest(new { Message = "Seules les demandes en brouillon peuvent être supprimées" });
            }

            var userId = User.GetUserId();
            request.DeletedAt = DateTimeOffset.UtcNow;
            request.DeletedBy = userId;

            await _db.SaveChangesAsync();

            // Logger l'événement
            await _leaveEventLogService.LogLeaveRequestEventAsync(
                request.CompanyId,
                request.EmployeeId,
                request.Id,
                LeaveEventLogService.EventNames.RequestDeleted,
                $"Demande ID {request.Id} supprimée",
                null,
                userId
            );

            return NoContent();
        }

        // Méthodes privées

        private static LeaveRequestReadDto MapToReadDto(LeaveRequest request)
        {
            return new LeaveRequestReadDto
            {
                Id = request.Id,
                EmployeeId = request.EmployeeId,
                CompanyId = request.CompanyId,
                LeaveTypeId = request.LeaveTypeId,
                LeaveTypeCode = request.LeaveType.LeaveCode,
                LeaveTypeName = request.LeaveType.LeaveNameFr,
                LegalRuleId = request.LegalRuleId,
                LegalCaseCode = request.LegalRule?.EventCaseCode,
                LegalCaseDescription = request.LegalRule?.Description,
                LegalDaysGranted = request.LegalRule?.DaysGranted,
                LegalArticle = request.LegalRule?.LegalArticle,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = request.Status,
                RequestedAt = request.RequestedAt,
                SubmittedAt = request.SubmittedAt,
                DecisionAt = request.DecisionAt,
                DecisionBy = request.DecisionBy,
                DecisionComment = request.DecisionComment,
                CalendarDays = request.CalendarDays,
                WorkingDaysDeducted = request.WorkingDaysDeducted,
                HasMinConsecutiveBlock = request.HasMinConsecutiveBlock,
                ComputationVersion = request.ComputationVersion,
                IsRenounced = request.IsRenounced,
                EmployeeNote = request.EmployeeNote,
                ManagerNote = request.ManagerNote,
                CreatedAt = request.CreatedAt
            };
        }
    }

    // DTO pour les actions d'approbation/rejet
    public class ApprovalDto
    {
        public string? Comment { get; set; }
    }
}
