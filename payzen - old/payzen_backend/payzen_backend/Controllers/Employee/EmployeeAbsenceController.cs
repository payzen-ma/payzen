using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Employee.Dtos;

namespace payzen_backend.Controllers.Employees
{
    [Route("api/absences")]
    [ApiController]
    [Authorize]
    public class EmployeeAbsenceController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EmployeeAbsenceController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// R�cup�re les statistiques des absences
        /// GET /api/absences/stats?companyId=5
        /// GET /api/absences/stats?companyId=5&employeeId=7
        /// </summary>
        [HttpGet("stats")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeAbsenceStatsDto>> GetAbsenceStats(
            [FromQuery] int companyId,
            [FromQuery] int? employeeId = null)
        {
            // Company exists (global HasQueryFilter handles DeletedAt)
            var company = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId);

            if (company == null)
                return NotFound(new { Message = "Société non trouvée" });

            // Base query for absences scoped to this company's employees
            var query = _db.EmployeeAbsences
                .AsNoTracking()
                .Where(a => a.Employee.CompanyId == companyId);

            payzen_backend.Models.Employee.Employee? employee = null;
            if (employeeId.HasValue)
            {
                // Ensure the employee exists and belongs to the company (single query)
                employee = await _db.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == employeeId && e.CompanyId == companyId);

                if (employee == null)
                    return NotFound(new { Message = "Employé non trouvé" });

                query = query.Where(a => a.EmployeeId == employeeId.Value);
            }

            // Aggregate counts in a single SQL query to avoid loading all rows into memory
            var agg = await query
                .GroupBy(a => 1)
                .Select(g => new
                {
                    TotalAbsences = g.Count(),
                    FullDayAbsences = g.Count(a => a.DurationType == AbsenceDurationType.FullDay),
                    HalfDayAbsences = g.Count(a => a.DurationType == AbsenceDurationType.HalfDay),
                    HourlyAbsences = g.Count(a => a.DurationType == AbsenceDurationType.Hourly),
                    SubmittedCount = g.Count(a => a.Status == AbsenceStatus.Submitted),
                    ApprovedCount = g.Count(a => a.Status == AbsenceStatus.Approved),
                    RejectedCount = g.Count(a => a.Status == AbsenceStatus.Rejected),
                    CancelledCount = g.Count(a => a.Status == AbsenceStatus.Cancelled)
                })
                .FirstOrDefaultAsync();

            var stats = new EmployeeAbsenceStatsDto
            {
                CompanyId = companyId,
                CompanyName = company.CompanyName,
                EmployeeId = employeeId,
                EmployeeFullName = employee != null ? $"{employee.FirstName} {employee.LastName}" : null,
                TotalAbsences = agg?.TotalAbsences ?? 0,
                FullDayAbsences = agg?.FullDayAbsences ?? 0,
                HalfDayAbsences = agg?.HalfDayAbsences ?? 0,
                HourlyAbsences = agg?.HourlyAbsences ?? 0,
                SubmittedCount = agg?.SubmittedCount ?? 0,
                ApprovedCount = agg?.ApprovedCount ?? 0,
                RejectedCount = agg?.RejectedCount ?? 0,
                CancelledCount = agg?.CancelledCount ?? 0,
                GeneratedAt = DateTimeOffset.UtcNow
            };

            // Breakdown by absence type (server-side grouping)
            var byType = await query
                .GroupBy(a => a.AbsenceType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();
            stats.AbsencesByType = byType.ToDictionary(x => x.Type, x => x.Count);

            // Breakdown by month (Year-Month)
            var byMonthRaw = await query
                .GroupBy(a => new { Year = a.AbsenceDate.Year, Month = a.AbsenceDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();
            stats.AbsencesByMonth = byMonthRaw
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToDictionary(x => $"{x.Year:0000}-{x.Month:00}", x => x.Count);

            return Ok(stats);
        }

        /// <summary>
        /// Delete une absence (soft delete) /// DELETE /api/absences/{id}
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> SoftDeleteAbsence(int id)
        {
            // Find absence by id (global query filters handle soft-deleted records)
            var absence = await _db.EmployeeAbsences
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (absence == null)
                return NotFound(new { Message = "Absence non trouvée" });

            // Interdire la suppression des absences approuvées
            if (absence.Status == AbsenceStatus.Approved)
                return BadRequest(new { Message = "Impossible de supprimer une absence approuvée" });

            absence.DeletedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
            /// R�cup�re la liste des absences
            /// GET /api/absences?companyId=5&limit=1000
            /// GET /api/absences?companyId=5&employeeId=7&limit=50
            /// GET /api/absences?companyId=5&startDate=2025-01-01&endDate=2025-01-31
            /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<EmployeeAbsenceReadDto>>> GetAbsences(
            [FromQuery] int companyId,
            [FromQuery] int? employeeId = null,
            [FromQuery] DateOnly? startDate = null,
            [FromQuery] DateOnly? endDate = null,
            [FromQuery] AbsenceDurationType? durationType = null,
            [FromQuery] AbsenceStatus? status = null,
            [FromQuery] string? absenceType = null,
            [FromQuery] int limit = 100)
        {
            // Validation du limit
            if (limit <= 0 || limit > 10000)
                return BadRequest(new { Message = "Le limit doit �tre entre 1 et 10000" });

            // V�rifier que la soci�t� existe
            var companyExists = await _db.Companies
                .AnyAsync(c => c.Id == companyId);

            if (!companyExists)
                return NotFound(new { Message = "Société non trouvée" });

            // Construire la requête (pas d'Include nécessaire pour la projection)
            var query = _db.EmployeeAbsences
                .AsNoTracking()
                .Where(a => a.Employee.CompanyId == companyId);

            // Filtrer par employ�
            if (employeeId.HasValue)
            {
                var employeeExists = await _db.Employees
                    .AnyAsync(e => e.Id == employeeId && e.CompanyId == companyId);

                if (!employeeExists)
                    return NotFound(new { Message = "Employ� non trouv� pour cette soci�t�" });

                query = query.Where(a => a.EmployeeId == employeeId.Value);
            }

            // Filtrer par p�riode
            if (startDate.HasValue)
                query = query.Where(a => a.AbsenceDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.AbsenceDate <= endDate.Value);

            // Filtrer par type de dur�e
            if (durationType.HasValue)
                query = query.Where(a => a.DurationType == durationType.Value);

            // Filtrer par statut
            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);

            // Filtrer par type d'absence
            if (!string.IsNullOrWhiteSpace(absenceType))
                query = query.Where(a => a.AbsenceType == absenceType.Trim());

            // R�cup�rer les absences avec limite
            var absences = await query
                .OrderByDescending(a => a.AbsenceDate)
                .ThenBy(a => a.Employee.LastName)
                .ThenBy(a => a.Employee.FirstName)
                .Take(limit)
                .Select(a => new EmployeeAbsenceReadDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeFirstName = a.Employee.FirstName,
                    EmployeeLastName = a.Employee.LastName,
                    EmployeeFullName = $"{a.Employee.FirstName} {a.Employee.LastName}",
                    AbsenceDate = a.AbsenceDate,
                    AbsenceDateFormatted = a.AbsenceDate.ToString("yyyy-MM-dd"),
                    DurationType = a.DurationType,
                    DurationTypeDescription = a.DurationType == AbsenceDurationType.FullDay ? "Journ�e enti�re" :
                                              a.DurationType == AbsenceDurationType.HalfDay ? "Demi-journ�e" : "Horaire",
                    IsMorning = a.IsMorning,
                    HalfDayDescription = a.DurationType == AbsenceDurationType.HalfDay
                        ? (a.IsMorning == true ? "Matin" : a.IsMorning == false ? "Apr�s-midi" : null)
                        : null,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    AbsenceType = a.AbsenceType,
                    Reason = a.Reason,
                    Status = a.Status,
                    StatusDescription = a.Status.ToString(),
                    DecisionAt = a.DecisionAt.HasValue ? a.DecisionAt.Value.DateTime : null,
                    DecisionBy = a.DecisionBy,
                    DecisionComment = a.DecisionComment,
                    CreatedAt = a.CreatedAt.DateTime
                })
                .ToListAsync();

            // Charger les noms des d�cideurs si n�cessaire
            var decisionByIds = absences.Where(a => a.DecisionBy.HasValue).Select(a => a.DecisionBy!.Value).Distinct().ToList();
            if (decisionByIds.Any())
            {
                var users = await _db.Users
                    .Where(u => decisionByIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.Username })
                    .ToListAsync();

                foreach (var absence in absences.Where(a => a.DecisionBy.HasValue))
                {
                    var user = users.FirstOrDefault(u => u.Id == absence.DecisionBy!.Value);
                    if (user != null)
                        absence.DecisionByName = user.Username;
                }
            }

            return Ok(absences);
        }

        /// <summary>
        /// Cr�e une nouvelle absence
        /// POST /api/absences
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeAbsenceReadDto>> CreateAbsence([FromBody] EmployeeAbsenceCreateDto dto)
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

            var employee = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId);

            if (employee == null)
                return NotFound(new { Message = "Employ� non trouv�" });

            // Validations selon le type de dur�e
            if (dto.DurationType == AbsenceDurationType.HalfDay && !dto.IsMorning.HasValue)
                return BadRequest(new { Message = "IsMorning est requis pour une demi-journ�e" });

            if (dto.DurationType == AbsenceDurationType.Hourly)
            {
                if (!dto.StartTime.HasValue || !dto.EndTime.HasValue)
                    return BadRequest(new { Message = "StartTime et EndTime sont requis pour une absence horaire" });

                if (dto.StartTime >= dto.EndTime)
                    return BadRequest(new { Message = "L'heure de fin doit �tre apr�s l'heure de d�but" });
            }

            // V�rification selon le type de dur�e
            if (dto.DurationType == AbsenceDurationType.Hourly)
            {
                // Pour les absences horaires, v�rifier les chevauchements
                var overlappingAbsences = await _db.EmployeeAbsences
                    .Where(a => a.EmployeeId == dto.EmployeeId &&
                                a.AbsenceDate == dto.AbsenceDate &&
                                a.DurationType == AbsenceDurationType.Hourly &&
                                a.Status != AbsenceStatus.Rejected &&
                                a.Status != AbsenceStatus.Cancelled)
                    .ToListAsync();

                foreach (var existing in overlappingAbsences)
                {
                    if (existing.StartTime.HasValue && existing.EndTime.HasValue &&
                        dto.StartTime.HasValue && dto.EndTime.HasValue)
                    {
                        bool hasOverlap = dto.StartTime < existing.EndTime && dto.EndTime > existing.StartTime;

                        if (hasOverlap)
                        {
                            return Conflict(new
                            {
                                Message = $"Cette tranche horaire chevauche une absence existante ({existing.StartTime:HH\\:mm} - {existing.EndTime:HH\\:mm})"
                            });
                        }
                    }
                }

                // V�rifier qu'il n'existe pas d'absence FullDay ou HalfDay
                var fullOrHalfDayAbsence = await _db.EmployeeAbsences
                    .AnyAsync(a => a.EmployeeId == dto.EmployeeId &&
                                  a.AbsenceDate == dto.AbsenceDate &&
                                  (a.DurationType == AbsenceDurationType.FullDay ||
                                   a.DurationType == AbsenceDurationType.HalfDay) &&
                                  a.Status != AbsenceStatus.Rejected &&
                                  a.Status != AbsenceStatus.Cancelled);

                if (fullOrHalfDayAbsence)
                {
                    return Conflict(new { Message = "Une absence journ�e enti�re ou demi-journ�e existe d�j� pour cette date" });
                }
            }
            else
            {
                // Pour FullDay et HalfDay, interdire toute autre absence
                var existingAbsence = await _db.EmployeeAbsences
                    .AnyAsync(a => a.EmployeeId == dto.EmployeeId &&
                                  a.AbsenceDate == dto.AbsenceDate &&
                                  a.Status != AbsenceStatus.Rejected &&
                                  a.Status != AbsenceStatus.Cancelled);

                if (existingAbsence)
                {
                    return Conflict(new { Message = "Une absence existe d�j� pour cette date" });
                }
            }

            var absence = new EmployeeAbsence
            {
                EmployeeId = dto.EmployeeId,
                AbsenceDate = dto.AbsenceDate,
                DurationType = dto.DurationType,
                IsMorning = dto.DurationType == AbsenceDurationType.HalfDay ? dto.IsMorning : null,
                StartTime = dto.DurationType == AbsenceDurationType.Hourly ? dto.StartTime : null,
                EndTime = dto.DurationType == AbsenceDurationType.Hourly ? dto.EndTime : null,
                AbsenceType = dto.AbsenceType.Trim(),
                Reason = dto.Reason?.Trim(),
                Status = isRhOrAdmin ? AbsenceStatus.Approved : AbsenceStatus.Draft,
                DecisionAt = isRhOrAdmin ? DateTimeOffset.UtcNow : (DateTimeOffset?)null,
                DecisionBy = isRhOrAdmin ? userId : (int?)null,
                DecisionComment = isRhOrAdmin ? "Approbation automatique (RH/Admin)" : null,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            Console.WriteLine($"Absence status {absence.Status}");

            _db.EmployeeAbsences.Add(absence);
            await _db.SaveChangesAsync();

            var createdAbsence = await _db.EmployeeAbsences
                .AsNoTracking()
                .Include(a => a.Employee)
                .Where(a => a.Id == absence.Id)
                .Select(a => new EmployeeAbsenceReadDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeFirstName = a.Employee.FirstName,
                    EmployeeLastName = a.Employee.LastName,
                    EmployeeFullName = $"{a.Employee.FirstName} {a.Employee.LastName}",
                    AbsenceDate = a.AbsenceDate,
                    AbsenceDateFormatted = a.AbsenceDate.ToString("yyyy-MM-dd"),
                    DurationType = a.DurationType,
                    DurationTypeDescription = a.DurationType == AbsenceDurationType.FullDay ? "Journ�e enti�re" :
                                              a.DurationType == AbsenceDurationType.HalfDay ? "Demi-journ�e" : "Horaire",
                    IsMorning = a.IsMorning,
                    HalfDayDescription = a.DurationType == AbsenceDurationType.HalfDay
                        ? (a.IsMorning == true ? "Matin" : a.IsMorning == false ? "Apr�s-midi" : null)
                        : null,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    AbsenceType = a.AbsenceType,
                    Reason = a.Reason,
                    Status = a.Status,
                    StatusDescription = a.Status.ToString(),
                    DecisionAt = a.DecisionAt.HasValue ? a.DecisionAt.Value.DateTime : null,
                    DecisionBy = a.DecisionBy,
                    DecisionComment = a.DecisionComment,
                    CreatedAt = a.CreatedAt.DateTime
                })
                .FirstAsync();

            return CreatedAtAction(nameof(GetAbsenceById), new { id = absence.Id }, createdAbsence);
        }
        /// <summary>
        /// Soumet une demande d'absence (change le statut de Draft � Submitted)
        /// POST /api/absences/{id}/submit
        /// </summary>
        [HttpPost("{id}/submit")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeAbsenceReadDto>> SubmitAbsence(int id)
        {
            var userId = User.GetUserId();

            var absence = await _db.EmployeeAbsences
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (absence == null)
                return NotFound(new { Message = "Absence non trouv�e" });

            // V�rifier que l'absence est en statut Draft
            if (absence.Status != AbsenceStatus.Draft)
                return BadRequest(new { Message = $"Impossible de soumettre une absence avec le statut {absence.Status}" });

            // Optionnel : v�rifier que l'utilisateur est l'employ� concern�
            var currentUser = await _db.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (currentUser?.Employee == null)
                return BadRequest(new { Message = "Utilisateur non associ� � un employ�" });

            // Autorisation : seul l'employ� concern� peut soumettre sa demande
            bool isOwner = currentUser.EmployeeId == absence.EmployeeId;
            if (!isOwner)
                return Forbid();

            // Mettre � jour le statut
            absence.Status = AbsenceStatus.Submitted;

            await _db.SaveChangesAsync();

            // Pr�parer la r�ponse
            var result = new EmployeeAbsenceReadDto
            {
                Id = absence.Id,
                EmployeeId = absence.EmployeeId,
                EmployeeFirstName = absence.Employee.FirstName,
                EmployeeLastName = absence.Employee.LastName,
                EmployeeFullName = $"{absence.Employee.FirstName} {absence.Employee.LastName}",
                AbsenceDate = absence.AbsenceDate,
                AbsenceDateFormatted = absence.AbsenceDate.ToString("yyyy-MM-dd"),
                DurationType = absence.DurationType,
                DurationTypeDescription = absence.DurationType == AbsenceDurationType.FullDay ? "Journ�e enti�re" :
                                  absence.DurationType == AbsenceDurationType.HalfDay ? "Demi-journ�e" : "Horaire",
                IsMorning = absence.IsMorning,
                HalfDayDescription = absence.DurationType == AbsenceDurationType.HalfDay
                    ? (absence.IsMorning == true ? "Matin" : absence.IsMorning == false ? "Apr�s-midi" : null)
                    : null,
                StartTime = absence.StartTime,
                EndTime = absence.EndTime,
                AbsenceType = absence.AbsenceType,
                Reason = absence.Reason,
                Status = absence.Status,
                StatusDescription = "Soumise",
                DecisionAt = absence.DecisionAt.HasValue ? absence.DecisionAt.Value.DateTime : null,
                DecisionBy = absence.DecisionBy,
                DecisionComment = absence.DecisionComment,
                CreatedAt = absence.CreatedAt.DateTime
            };

            return Ok(result);
        }



        /// <summary>
        /// Prend une d�cision sur une absence (Approuver/Rejeter)
        /// PUT /api/absences/{id}/decision
        /// </summary>
        [HttpPut("{id}/decision")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeAbsenceReadDto>> MakeDecision(int id, [FromBody] EmployeeAbsenceDecisionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Valider le statut de d�cision
            if (dto.Status != AbsenceStatus.Approved && dto.Status != AbsenceStatus.Rejected && dto.Status != AbsenceStatus.Cancelled)
                return BadRequest(new { Message = "Le statut doit �tre Approved, Rejected ou Cancelled" });

            var userId = User.GetUserId();

            var absence = await _db.EmployeeAbsences
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (absence == null)
                return NotFound(new { Message = "Absence non trouv�e" });

            // V�rifier que l'absence est en statut Submitted
            if (absence.Status != AbsenceStatus.Submitted)
                return BadRequest(new { Message = $"Impossible de modifier une absence avec le statut {absence.Status}" });

            absence.Status = dto.Status;
            absence.DecisionAt = DateTimeOffset.UtcNow;
            absence.DecisionBy = userId;
            absence.DecisionComment = dto.DecisionComment?.Trim();

            await _db.SaveChangesAsync();

            var updatedAbsence = new EmployeeAbsenceReadDto
            {
                Id = absence.Id,
                EmployeeId = absence.EmployeeId,
                EmployeeFirstName = absence.Employee.FirstName,
                EmployeeLastName = absence.Employee.LastName,
                EmployeeFullName = $"{absence.Employee.FirstName} {absence.Employee.LastName}",
                AbsenceDate = absence.AbsenceDate,
                AbsenceDateFormatted = absence.AbsenceDate.ToString("yyyy-MM-dd"),
                DurationType = absence.DurationType,
                DurationTypeDescription = absence.DurationType == AbsenceDurationType.FullDay ? "Journ�e enti�re" :
                                          absence.DurationType == AbsenceDurationType.HalfDay ? "Demi-journ�e" : "Horaire",
                IsMorning = absence.IsMorning,
                HalfDayDescription = absence.DurationType == AbsenceDurationType.HalfDay
                    ? (absence.IsMorning == true ? "Matin" : absence.IsMorning == false ? "Apr�s-midi" : null)
                    : null,
                StartTime = absence.StartTime,
                EndTime = absence.EndTime,
                AbsenceType = absence.AbsenceType,
                Reason = absence.Reason,
                Status = absence.Status,
                StatusDescription = absence.Status.ToString(),
                DecisionAt = absence.DecisionAt.HasValue ? absence.DecisionAt.Value.DateTime : null,
                DecisionBy = absence.DecisionBy,
                DecisionComment = absence.DecisionComment,
                CreatedAt = absence.CreatedAt.DateTime
            };

            // Charger le nom du d�cideur
            if (absence.DecisionBy.HasValue)
            {
                var user = await _db.Users
                    .Where(u => u.Id == absence.DecisionBy.Value)
                    .Select(u => u.Username)
                    .FirstOrDefaultAsync();

                updatedAbsence.DecisionByName = user;
            }

            return Ok(updatedAbsence);
        }

        /// <summary>
        /// R�cup�re une absence par ID
        /// GET /api/absences/{id}
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeAbsenceReadDto>> GetAbsenceById(int id)
        {
            var absence = await _db.EmployeeAbsences
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new EmployeeAbsenceReadDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeFirstName = a.Employee.FirstName,
                    EmployeeLastName = a.Employee.LastName,
                    EmployeeFullName = $"{a.Employee.FirstName} {a.Employee.LastName}",
                    AbsenceDate = a.AbsenceDate,
                    AbsenceDateFormatted = a.AbsenceDate.ToString("yyyy-MM-dd"),
                    DurationType = a.DurationType,
                    DurationTypeDescription = a.DurationType == AbsenceDurationType.FullDay ? "Journ�e enti�re" :
                                              a.DurationType == AbsenceDurationType.HalfDay ? "Demi-journ�e" : "Horaire",
                    IsMorning = a.IsMorning,
                    HalfDayDescription = a.DurationType == AbsenceDurationType.HalfDay
                        ? (a.IsMorning == true ? "Matin" : a.IsMorning == false ? "Apr�s-midi" : null)
                        : null,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    AbsenceType = a.AbsenceType,
                    Reason = a.Reason,
                    Status = a.Status,
                    StatusDescription = a.Status.ToString(),
                    DecisionAt = a.DecisionAt.HasValue ? a.DecisionAt.Value.DateTime : null,
                    DecisionBy = a.DecisionBy,
                    DecisionComment = a.DecisionComment,
                    CreatedAt = a.CreatedAt.DateTime,
                    CreatedBy = a.CreatedBy
                })
                .FirstOrDefaultAsync();

            if (absence == null)
                return NotFound(new { Message = "Absence non trouv�e" });

            // Charger les noms des utilisateurs (DecisionBy et CreatedBy) via Employee
            var userIds = new List<int>();
            if (absence.DecisionBy.HasValue)
                userIds.Add(absence.DecisionBy.Value);
            if (absence.CreatedBy > 0)
                userIds.Add(absence.CreatedBy);

            if (userIds.Any())
            {
                var users = await _db.Users
                    .AsNoTracking()
                    .Where(u => userIds.Contains(u.Id) && u.Employee != null)
                    .Select(u => new
                    {
                        u.Id,
                        EmployeeFirstName = u.Employee!.FirstName,
                        EmployeeLastName = u.Employee!.LastName,
                        FullName = $"{u.Employee.FirstName} {u.Employee.LastName}"
                    })
                    .ToListAsync();

                // Assigner DecisionByName
                if (absence.DecisionBy.HasValue)
                {
                    var decisionUser = users.FirstOrDefault(u => u.Id == absence.DecisionBy.Value);
                    absence.DecisionByName = decisionUser?.FullName;
                }

                // Assigner CreatedByName
                var createdUser = users.FirstOrDefault(u => u.Id == absence.CreatedBy);
                absence.CreatedByName = createdUser?.FullName;
            }

            return Ok(absence);
        }

        /// <summary>
        /// Met � jour une absence (brouillon uniquement)
        /// PATCH /api/absences/{id}
        /// </summary>
        [HttpPatch("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeAbsenceReadDto>> UpdateAbsence(int id, [FromBody] EmployeeAbsenceUpdateDto dto)
        {
            var absence = await _db.EmployeeAbsences
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (absence == null)
                return NotFound(new { Message = "Absence non trouv�e" });

            if (absence.Status != AbsenceStatus.Draft)
                return BadRequest(new { Message = "Seules les absences en brouillon peuvent �tre modifi�es" });

            if (dto.AbsenceDate.HasValue) absence.AbsenceDate = dto.AbsenceDate.Value;
            if (dto.DurationType.HasValue) absence.DurationType = dto.DurationType.Value;
            if (dto.IsMorning.HasValue) absence.IsMorning = dto.IsMorning.Value;
            if (dto.StartTime.HasValue) absence.StartTime = dto.StartTime;
            if (dto.EndTime.HasValue) absence.EndTime = dto.EndTime;
            if (dto.AbsenceType != null) absence.AbsenceType = dto.AbsenceType.Trim();
            if (dto.Reason != null) absence.Reason = dto.Reason.Trim();

            var userId = User.GetUserId();
            absence.ModifiedBy = userId;
            absence.ModifiedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();

            var readDto = await _db.EmployeeAbsences
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new EmployeeAbsenceReadDto
                {
                    Id = a.Id,
                    EmployeeId = a.EmployeeId,
                    EmployeeFirstName = a.Employee.FirstName,
                    EmployeeLastName = a.Employee.LastName,
                    EmployeeFullName = $"{a.Employee.FirstName} {a.Employee.LastName}",
                    AbsenceDate = a.AbsenceDate,
                    AbsenceDateFormatted = a.AbsenceDate.ToString("yyyy-MM-dd"),
                    DurationType = a.DurationType,
                    DurationTypeDescription = a.DurationType == AbsenceDurationType.FullDay ? "Journ�e enti�re" :
                                              a.DurationType == AbsenceDurationType.HalfDay ? "Demi-journ�e" : "Horaire",
                    IsMorning = a.IsMorning,
                    HalfDayDescription = a.DurationType == AbsenceDurationType.HalfDay
                        ? (a.IsMorning == true ? "Matin" : a.IsMorning == false ? "Apr�s-midi" : null)
                        : null,
                    StartTime = a.StartTime,
                    EndTime = a.EndTime,
                    AbsenceType = a.AbsenceType,
                    Reason = a.Reason,
                    Status = a.Status,
                    StatusDescription = a.Status.ToString(),
                    DecisionAt = a.DecisionAt.HasValue ? a.DecisionAt.Value.DateTime : null,
                    DecisionBy = a.DecisionBy,
                    DecisionComment = a.DecisionComment,
                    CreatedAt = a.CreatedAt.DateTime,
                    CreatedBy = a.CreatedBy
                })
                .FirstAsync();

            return Ok(readDto);
        }

        /// <summary>
        /// Supprime une absence
        /// DELETE /api/absences/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAbsence(int id)
        {
            var absence = await _db.EmployeeAbsences
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (absence == null)
                return NotFound(new { Message = "Absence non trouv�e" });

            // On peut interdire la suppression des absences approuv�es
            if (absence.Status == AbsenceStatus.Approved)
                return BadRequest(new { Message = "Impossible de supprimer une absence approuv�e" });

            _db.EmployeeAbsences.Remove(absence);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Annule une demande d'absence
        /// POST /api/absences/{id}/cancel
        /// </summary>
        [HttpPost("{id}/cancel")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeAbsenceReadDto>> CancelAbsence(
            int id,
            [FromBody] EmployeeAbsenceCancellationDto? dto = null)
        {
            var userId = User.GetUserId();

            var absence = await _db.EmployeeAbsences
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (absence == null)
                return NotFound(new { Message = "Absence non trouv�e" });

            // V�rifier que l'absence peut �tre annul�e (Submitted ou Approved uniquement)
            if (absence.Status != AbsenceStatus.Submitted && absence.Status != AbsenceStatus.Approved)
                return BadRequest(new { Message = $"Impossible d'annuler une absence avec le statut {absence.Status}" });

            // Optionnel : v�rifier que l'utilisateur est l'employ� concern� ou un RH/Manager
            var currentUser = await _db.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (currentUser?.Employee == null)
                return BadRequest(new { Message = "Utilisateur non associ� � un employ�" });

            // Autorisation : seul l'employ� concern� ou un manager/RH peut annuler
            bool isOwner = currentUser.EmployeeId == absence.EmployeeId;
            bool isAuthorized = isOwner; // Ajoutez ici la logique pour v�rifier si c'est un RH/Manager

            if (!isAuthorized)
                return Forbid();

            // Mettre � jour le statut
            absence.Status = AbsenceStatus.Cancelled;
            absence.DecisionAt = DateTimeOffset.UtcNow;
            absence.DecisionBy = userId;
            absence.DecisionComment = dto?.Reason?.Trim();

            await _db.SaveChangesAsync();

            // Charger le d�cideur
            var decisionByUser = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync();

            var result = new EmployeeAbsenceReadDto
            {
                Id = absence.Id,
                EmployeeId = absence.EmployeeId,
                EmployeeFirstName = absence.Employee.FirstName,
                EmployeeLastName = absence.Employee.LastName,
                EmployeeFullName = $"{absence.Employee.FirstName} {absence.Employee.LastName}",
                AbsenceDate = absence.AbsenceDate,
                AbsenceDateFormatted = absence.AbsenceDate.ToString("yyyy-MM-dd"),
                DurationType = absence.DurationType,
                DurationTypeDescription = absence.DurationType == AbsenceDurationType.FullDay ? "Journ�e enti�re" :
                                          absence.DurationType == AbsenceDurationType.HalfDay ? "Demi-journ�e" : "Horaire",
                IsMorning = absence.IsMorning,
                HalfDayDescription = absence.DurationType == AbsenceDurationType.HalfDay
                    ? (absence.IsMorning == true ? "Matin" : absence.IsMorning == false ? "Apr�s-midi" : null)
                    : null,
                StartTime = absence.StartTime,
                EndTime = absence.EndTime,
                AbsenceType = absence.AbsenceType,
                Reason = absence.Reason,
                Status = absence.Status,
                StatusDescription = "Annul�e",
                DecisionAt = absence.DecisionAt.Value.DateTime,
                DecisionBy = absence.DecisionBy,
                DecisionByName = decisionByUser,
                DecisionComment = absence.DecisionComment,
                CreatedAt = absence.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re les types d'absence distincts pour une soci�t�
        /// GET /api/absences/types?companyId=5
        /// </summary>
        [HttpGet("types")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<string>>> GetAbsenceTypes([FromQuery] int companyId)
        {
            var companyExists = await _db.Companies
                .AnyAsync(c => c.Id == companyId && c.DeletedAt == null);

            if (!companyExists)
                return NotFound(new { Message = "Soci�t� non trouv�e" });

            var types = await _db.EmployeeAbsences
                .AsNoTracking()
                .Include(a => a.Employee)
                .Where(a => a.Employee.CompanyId == companyId && a.Employee.DeletedAt == null)
                .Select(a => a.AbsenceType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            return Ok(types);
        }

        /// <summary>
        /// Approuve une demande d'absence
        /// POST /api/absences/{id}/approve
        /// </summary>
        [HttpPost("{id}/approve")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeAbsenceReadDto>> ApproveAbsence(
            int id, 
            [FromBody] EmployeeAbsenceApprovalDto? dto = null)
        {
            var userId = User.GetUserId();

            var absence = await _db.EmployeeAbsences
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id && a.Employee.DeletedAt == null);

            if (absence == null)
                return NotFound(new { Message = "Absence non trouv�e" });

            // V�rifier que l'absence est en statut Submitted
            if (absence.Status != AbsenceStatus.Submitted)
                return BadRequest(new { Message = $"Impossible d'approuver une absence avec le statut {absence.Status}" });

            // Mettre � jour le statut
            absence.Status = AbsenceStatus.Approved;
            absence.DecisionAt = DateTimeOffset.UtcNow;
            absence.DecisionBy = userId;
            absence.DecisionComment = dto?.Comment?.Trim();

            await _db.SaveChangesAsync();

            // Charger le d�cideur
            var decisionByUser = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync();

            var result = new EmployeeAbsenceReadDto
            {
                Id = absence.Id,
                EmployeeId = absence.EmployeeId,
                EmployeeFirstName = absence.Employee.FirstName,
                EmployeeLastName = absence.Employee.LastName,
                EmployeeFullName = $"{absence.Employee.FirstName} {absence.Employee.LastName}",
                AbsenceDate = absence.AbsenceDate,
                AbsenceDateFormatted = absence.AbsenceDate.ToString("yyyy-MM-dd"),
                DurationType = absence.DurationType,
                DurationTypeDescription = absence.DurationType == AbsenceDurationType.FullDay ? "Journ�e enti�re" :
                                          absence.DurationType == AbsenceDurationType.HalfDay ? "Demi-journ�e" : "Horaire",
                IsMorning = absence.IsMorning,
                HalfDayDescription = absence.DurationType == AbsenceDurationType.HalfDay
                    ? (absence.IsMorning == true ? "Matin" : absence.IsMorning == false ? "Apr�s-midi" : null)
                    : null,
                StartTime = absence.StartTime,
                EndTime = absence.EndTime,
                AbsenceType = absence.AbsenceType,
                Reason = absence.Reason,
                Status = absence.Status,
                StatusDescription = "Approuv�e",
                DecisionAt = absence.DecisionAt.Value.DateTime,
                DecisionBy = absence.DecisionBy,
                DecisionByName = decisionByUser,
                DecisionComment = absence.DecisionComment,
                CreatedAt = absence.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Rejette une demande d'absence
        /// POST /api/absences/{id}/reject
        /// </summary>
        [HttpPost("{id}/reject")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeAbsenceReadDto>> RejectAbsence(
            int id, 
            [FromBody] EmployeeAbsenceRejectionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            var absence = await _db.EmployeeAbsences
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Id == id && a.Employee.DeletedAt == null);

            if (absence == null)
                return NotFound(new { Message = "Absence non trouv�e" });

            // V�rifier que l'absence est en statut Submitted
            if (absence.Status != AbsenceStatus.Submitted)
                return BadRequest(new { Message = $"Impossible de rejeter une absence avec le statut {absence.Status}" });

            // Mettre � jour le statut
            absence.Status = AbsenceStatus.Rejected;
            absence.DecisionAt = DateTimeOffset.UtcNow;
            absence.DecisionBy = userId;
            absence.DecisionComment = dto.Reason?.Trim();

            await _db.SaveChangesAsync();

            // Charger le d�cideur
            var decisionByUser = await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync();

            var result = new EmployeeAbsenceReadDto
            {
                Id = absence.Id,
                EmployeeId = absence.EmployeeId,
                EmployeeFirstName = absence.Employee.FirstName,
                EmployeeLastName = absence.Employee.LastName,
                EmployeeFullName = $"{absence.Employee.FirstName} {absence.Employee.LastName}",
                AbsenceDate = absence.AbsenceDate,
                AbsenceDateFormatted = absence.AbsenceDate.ToString("yyyy-MM-dd"),
                DurationType = absence.DurationType,
                DurationTypeDescription = absence.DurationType == AbsenceDurationType.FullDay ? "Journ�e enti�re" :
                                          absence.DurationType == AbsenceDurationType.HalfDay ? "Demi-journ�e" : "Horaire",
                IsMorning = absence.IsMorning,
                HalfDayDescription = absence.DurationType == AbsenceDurationType.HalfDay
                    ? (absence.IsMorning == true ? "Matin" : absence.IsMorning == false ? "Apr�s-midi" : null)
                    : null,
                StartTime = absence.StartTime,
                EndTime = absence.EndTime,
                AbsenceType = absence.AbsenceType,
                Reason = absence.Reason,
                Status = absence.Status,
                StatusDescription = "Rejet�e",
                DecisionAt = absence.DecisionAt.Value.DateTime,
                DecisionBy = absence.DecisionBy,
                DecisionByName = decisionByUser,
                DecisionComment = absence.DecisionComment,
                CreatedAt = absence.CreatedAt.DateTime
            };

            return Ok(result);
        }
    }
}
