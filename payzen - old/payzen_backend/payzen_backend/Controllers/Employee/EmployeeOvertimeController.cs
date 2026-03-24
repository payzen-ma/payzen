using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Common.OvertimeEnums;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Employee.Dtos;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace payzen_backend.Controllers.Employee
{
    [Route("api/employee-overtimes")]
    [ApiController]
    [Authorize]
    public class EmployeeOvertimeController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EmployeeOvertimeController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Retourne la liste des types d'overtime avec leurs descriptions
        /// GET /api/employee-overtimes/types
        /// Utile pour le frontend (légendes, filtres, etc.)
        /// </summary>
        [HttpGet("types")]
        [Produces("application/json")]
        public ActionResult<IEnumerable<object>> GetOvertimeTypes()
        {
            var types = new List<object>
            {
                // Types de base (individuels)
                new { Value = (int)OvertimeType.None, Code = "None", DescriptionFr = "Aucun", DescriptionEn = "None" },
                new { Value = (int)OvertimeType.Standard, Code = "Standard", DescriptionFr = "Standard", DescriptionEn = "Standard" },
                new { Value = (int)OvertimeType.WeeklyRest, Code = "WeeklyRest", DescriptionFr = "Repos hebdomadaire", DescriptionEn = "Weekly Rest" },
                new { Value = (int)OvertimeType.PublicHoliday, Code = "PublicHoliday", DescriptionFr = "Jour férié", DescriptionEn = "Public Holiday" },
                new { Value = (int)OvertimeType.Night, Code = "Night", DescriptionFr = "Nuit", DescriptionEn = "Night" },
                
                // Combinaisons courantes
                new { Value = (int)(OvertimeType.Standard | OvertimeType.Night), Code = "Standard_Night", DescriptionFr = "Standard + Nuit", DescriptionEn = "Standard + Night" },
                new { Value = (int)(OvertimeType.WeeklyRest | OvertimeType.Night), Code = "WeeklyRest_Night", DescriptionFr = "Repos hebdomadaire + Nuit", DescriptionEn = "Weekly Rest + Night" },
                new { Value = (int)(OvertimeType.PublicHoliday | OvertimeType.Night), Code = "PublicHoliday_Night", DescriptionFr = "Jour férié + Nuit", DescriptionEn = "Public Holiday + Night" },
                new { Value = (int)OvertimeType.FerieOrRest, Code = "FerieOrRest", DescriptionFr = "Férié ou Repos", DescriptionEn = "Holiday or Rest" }
            };

            return Ok(types);
        }

        /// <summary>
        /// Recupere les overtimes avec filtres
        /// GET /api/employee-overtimes?employeeId=5&status=Submitted&month=2026-01
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<EmployeeOvertimeListDto>>> GetAll(
            [FromQuery] int? companyId = null,
            [FromQuery] int? employeeId = null,
            [FromQuery] OvertimeStatus? status = null,
            [FromQuery] DateOnly? fromDate = null,
            [FromQuery] DateOnly? toDate = null,
            [FromQuery] bool? isProcessedInPayroll = null)
        {
            var query = _db.EmployeeOvertimes
                .AsNoTracking()
                .Where(o => o.DeletedAt == null);

            // Filtrer par société (obligatoire pour l'isolation des données)
            if (companyId.HasValue)
                query = query.Where(o => o.Employee.CompanyId == companyId.Value);

            // Filtres
            if (employeeId.HasValue)
                query = query.Where(o => o.EmployeeId == employeeId.Value);

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            if (fromDate.HasValue)
                query = query.Where(o => o.OvertimeDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(o => o.OvertimeDate <= toDate.Value);

            if (isProcessedInPayroll.HasValue)
                query = query.Where(o => o.IsProcessedInPayroll == isProcessedInPayroll.Value);

            var overtimesRaw = await query
                .Include(o => o.Employee)
                .Include(o => o.Holiday)
                .OrderByDescending(o => o.OvertimeDate)
                .ThenByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    Id = o.Id,
                    EmployeeFullName = $"{o.Employee.FirstName} {o.Employee.LastName}",
                    OvertimeDate = o.OvertimeDate,
                    OvertimeType = o.OverTimeType,
                    OvertimeTypeDescription = o.OverTimeType.ToString(),
                    StartTime = o.StartTime,             
                    EndTime = o.EndTime,                       
                    HolidayName = o.Holiday != null ? o.Holiday.NameFr : null, 
                    RateRuleNameApplied = o.RateRuleNameApplied,             
                    EmployeeComment = o.EmployeeComment,                    
                    DurationInHours = o.DurationInHours,
                    RateMultiplierApplied = o.RateMultiplierApplied,
                    Status = o.Status,
                    StatusDescription = o.Status.ToString(),
                    IsProcessedInPayroll = o.IsProcessedInPayroll,
                    CreatedAt = o.CreatedAt.DateTime
                })
                .ToListAsync();
            // Mapper vers DTO avec description (ne peut pas être fait dans la requête EF)
            var overtimes = overtimesRaw.Select(o => new EmployeeOvertimeListDto
            {
                Id = o.Id,
                EmployeeFullName = o.EmployeeFullName,
                OvertimeDate = o.OvertimeDate,
                OvertimeType = o.OvertimeType,
                StartTime = o.StartTime,
                EndTime = o.EndTime,
                HolidayName = o.HolidayName,
                OvertimeTypeDescription = OvertimeTypeHelper.GetDescription(o.OvertimeType),
                DurationInHours = o.DurationInHours,
                RateMultiplierApplied = o.RateMultiplierApplied,
                EmployeeComment = o.EmployeeComment,
                Status = o.Status,
                StatusDescription = o.Status.ToString(),
                IsProcessedInPayroll = o.IsProcessedInPayroll,
                CreatedAt = o.CreatedAt
            }).ToList();

            return Ok(overtimes);
        }

        /// <summary>
        /// Recupere un overtime par ID
        /// GET /api/employee-overtimes/5
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeOvertimeReadDto>> GetById(int id)
        {
            var overtimeRaw = await _db.EmployeeOvertimes
                .AsNoTracking()
                .Where(o => o.Id == id && o.DeletedAt == null)
                .Include(o => o.Employee)
                .Include(o => o.Holiday)
                .Select(o => new
                {
                    o.Id,
                    o.EmployeeId,
                    EmployeeFullName = $"{o.Employee.FirstName} {o.Employee.LastName}",
                    OvertimeType = o.OverTimeType,
                    o.EntryMode,
                    o.HolidayId,
                    HolidayName = o.Holiday != null ? o.Holiday.NameFr : null,
                    o.OvertimeDate,
                    o.StartTime,
                    o.EndTime,
                    o.CrossesMidnight,
                    o.DurationInHours,
                    o.StandardDayHours,
                    o.RateRuleId,
                    o.RateRuleCodeApplied,
                    o.RateRuleNameApplied,
                    o.RateMultiplierApplied,
                    o.MultiplierCalculationDetails,
                    o.SplitBatchId,
                    o.SplitSequence,
                    o.SplitTotalSegments,
                    o.Status,
                    o.EmployeeComment,
                    o.ManagerComment,
                    o.ApprovedBy,
                    o.ApprovedAt,
                    o.IsProcessedInPayroll,
                    o.PayrollBatchId,
                    o.ProcessedInPayrollAt,
                    o.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (overtimeRaw == null)
                return NotFound(new { Message = "Overtime non trouve" });

            // Mapper vers DTO avec description lisible (ne peut pas être fait dans EF)
            var overtime = new EmployeeOvertimeReadDto
            {
                Id = overtimeRaw.Id,
                EmployeeId = overtimeRaw.EmployeeId,
                EmployeeFullName = overtimeRaw.EmployeeFullName,
                OvertimeType = overtimeRaw.OvertimeType,
                OvertimeTypeDescription = OvertimeTypeHelper.GetDescription(overtimeRaw.OvertimeType),
                EntryMode = overtimeRaw.EntryMode,
                HolidayId = overtimeRaw.HolidayId,
                HolidayName = overtimeRaw.HolidayName,
                OvertimeDate = overtimeRaw.OvertimeDate,
                StartTime = overtimeRaw.StartTime,
                EndTime = overtimeRaw.EndTime,
                CrossesMidnight = overtimeRaw.CrossesMidnight,
                DurationInHours = overtimeRaw.DurationInHours,
                StandardDayHours = overtimeRaw.StandardDayHours,
                RateRuleId = overtimeRaw.RateRuleId,
                RateRuleCodeApplied = overtimeRaw.RateRuleCodeApplied,
                RateRuleNameApplied = overtimeRaw.RateRuleNameApplied,
                RateMultiplierApplied = overtimeRaw.RateMultiplierApplied,
                MultiplierCalculationDetails = overtimeRaw.MultiplierCalculationDetails,
                SplitBatchId = overtimeRaw.SplitBatchId,
                SplitSequence = overtimeRaw.SplitSequence,
                SplitTotalSegments = overtimeRaw.SplitTotalSegments,
                Status = overtimeRaw.Status,
                StatusDescription = overtimeRaw.Status.ToString(),
                EmployeeComment = overtimeRaw.EmployeeComment,
                ManagerComment = overtimeRaw.ManagerComment,
                ApprovedBy = overtimeRaw.ApprovedBy,
                ApprovedAt = overtimeRaw.ApprovedAt?.DateTime,
                IsProcessedInPayroll = overtimeRaw.IsProcessedInPayroll,
                PayrollBatchId = overtimeRaw.PayrollBatchId,
                ProcessedInPayrollAt = overtimeRaw.ProcessedInPayrollAt?.DateTime,
                CreatedAt = overtimeRaw.CreatedAt.DateTime
            };

            return Ok(overtime);
        }

        /// <summary>
        /// Cree un nouveau overtime avec detection automatique du type et application des regles
        /// POST /api/employee-overtimes
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<object>> Create([FromBody] EmployeeOvertimeCreateDto dto)
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

            // Verifier que l'employe existe
            var employee = await _db.Employees
                .Include(e => e.Company)
                .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId && e.DeletedAt == null);

            if (employee == null)
                return NotFound(new { Message = "Employe non trouve" });

            // === VALIDATION SELON ENTRY MODE ===
            decimal calculatedDuration;
            TimeOnly? effectiveStartTime = null;
            TimeOnly? effectiveEndTime = null;
            bool crossesMidnight = false;

            switch (dto.EntryMode)
            {
                case OvertimeEntryMode.HoursRange:
                    if (dto.StartTime == null || dto.EndTime == null)
                        return BadRequest(new { Message = "StartTime et EndTime requis pour mode HoursRange" });

                    effectiveStartTime = dto.StartTime.Value;
                    effectiveEndTime = dto.EndTime.Value;

                    // Calculer duree et detecter si traverse minuit
                    var start = dto.StartTime.Value;
                    var end = dto.EndTime.Value;
                    
                    // Detector si traverse minuit: si end < start, alors on traverse minuit
                    if (end < start || (end == start && start != new TimeOnly(0, 0)))
                    {
                        // Traverse minuit: calculer duree en ajoutant 24h
                        var duration = TimeSpan.FromHours(24) - (start - end);
                        calculatedDuration = (decimal)duration.TotalHours;
                        crossesMidnight = true;
                    }
                    else
                    {
                        // Meme jour: calcul normal
                        var duration = end - start;
                        calculatedDuration = (decimal)duration.TotalHours;
                        crossesMidnight = false;
                    }
                    break;

                case OvertimeEntryMode.DurationOnly:
                    if (!dto.DurationInHours.HasValue || dto.DurationInHours <= 0)
                        return BadRequest(new { Message = "DurationInHours requis pour mode DurationOnly" });

                    calculatedDuration = dto.DurationInHours.Value;
                    break;

                case OvertimeEntryMode.FullDay:
                    Console.WriteLine("================================");
                    Console.WriteLine("================================");
                    Console.WriteLine("================Switch Full Day================");
                    if (!dto.StandardDayHours.HasValue || dto.StandardDayHours <= 0)
                        return BadRequest(new { Message = "StandardDayHours requis pour mode FullDay" });

                    calculatedDuration = dto.StandardDayHours.Value;
                    break;

                default:
                    return BadRequest(new { Message = "Mode de saisie invalide" });
            }

            // === DeTECTION AUTOMATIQUE DU TYPE D'OVERTIME ===

            // 1. Verifier si c'est un jour ferie
            var holiday = await _db.Holidays
                .Where(h => h.DeletedAt == null && h.IsActive)
                .Where(h => h.HolidayDate == dto.OvertimeDate)
                .Where(h => h.CompanyId == null || h.CompanyId == employee.CompanyId)
                .OrderByDescending(h => h.CompanyId) // Priorite aux jours feries de l'entreprise
                .FirstOrDefaultAsync();
            Console.WriteLine("================================");
            Console.WriteLine("================================");
            Console.WriteLine("=========Holiday From 1=======================");
            Console.WriteLine($"The Holdays is {holiday}");

            // 2. Verifier si c'est un jour de repos hebdomadaire
            var dayOfWeek = (int)dto.OvertimeDate.DayOfWeek; // 0=Sunday, 1=Monday, ..., 6=Saturday
            var workingCalendarDay = await _db.WorkingCalendars
                .Where(wc => wc.CompanyId == employee.CompanyId
                          && wc.DayOfWeek == dayOfWeek
                          && wc.DeletedAt == null)
                .FirstOrDefaultAsync();

            bool isWeeklyRest = false;
            if (workingCalendarDay != null)
            {
                // Si IsWorkingDay = false, c'est un jour de repos
                isWeeklyRest = !workingCalendarDay.IsWorkingDay;
            }
            else
            {
                // Si pas de calendrier defini pour ce jour, considerer comme jour travaille par defaut
                isWeeklyRest = false;
            }

            // 3. Determiner le type principal
            // FIX: Ne pas inclure Standard par defaut si c'est ferie/repos
            // Standard ne doit etre ajoute que si aucun type specifique n'est detecte
            OvertimeType overtimeType = OvertimeType.None;

            if (holiday != null)
            {
                overtimeType |= OvertimeType.PublicHoliday;
            }
            else if (isWeeklyRest)
            {
                overtimeType |= OvertimeType.WeeklyRest;
            }
            else
            {
                // Seulement si ce n'est ni ferie ni repos, alors c'est Standard
                overtimeType = OvertimeType.Standard;
            }

            // 4. Verifier si c'est du travail de nuit (pour HoursRange uniquement)
            bool hasNightWork = false;
            if (dto.EntryMode == OvertimeEntryMode.HoursRange && effectiveStartTime != null && effectiveEndTime != null)
            {
                // Definir les heures de nuit (21h-6h selon legislation marocaine)
                var nightStart = new TimeOnly(21, 0);
                var nightEnd = new TimeOnly(6, 0);

                // Verifier chevauchement avec periode de nuit
                hasNightWork = CheckNightWorkOverlap(effectiveStartTime.Value, effectiveEndTime.Value, crossesMidnight, nightStart, nightEnd);

                if (hasNightWork)
                {
                    overtimeType |= OvertimeType.Night;
                }
            }

            // === BESOIN DE SPLIT? ===
            // Split si: traverse minuit OU traverse les plages jour/nuit (6h-21h = jour, 21h-6h = nuit)
            List<EmployeeOvertime> overtimesToCreate = new List<EmployeeOvertime>();

            bool needsSplit = false;
            if (dto.EntryMode == OvertimeEntryMode.HoursRange && effectiveStartTime != null && effectiveEndTime != null)
            {
                var dayStart = new TimeOnly(6, 0);
                var dayEnd = new TimeOnly(21, 0);
                var nightStart = new TimeOnly(21, 0);
                var nightEnd = new TimeOnly(6, 0);

                // Split necessaire si:
                // 1. Traverse minuit
                // 2. OU traverse la frontiere jour/nuit (6h ou 21h)
                if (crossesMidnight)
                {
                    needsSplit = true;
                }
                else
                {
                    // Meme jour: verifier si traverse 6h ou 21h
                    var start = effectiveStartTime.Value;
                    var end = effectiveEndTime.Value;
                    
                    // Si commence avant 6h et finit apres 6h -> split
                    // Si commence avant 21h et finit apres 21h -> split
                    if ((start < dayStart && end > dayStart) || (start < dayEnd && end > dayEnd))
                    {
                        needsSplit = true;
                    }
                }
            }

            if (needsSplit && effectiveStartTime != null && effectiveEndTime != null)
            {
                // Effectuer le split automatique en segments jour/nuit
                var splitResult = await CreateSplitOvertimes(
                    dto,
                    employee,
                    userId,
                    effectiveStartTime.Value,
                    effectiveEndTime.Value,
                    holiday?.Id);

                overtimesToCreate.AddRange(splitResult);
            }
            else
            {
                // Pas de split necessaire, creer un overtime simple
                var rateRule = await FindBestRateRule(dto.OvertimeDate, overtimeType, effectiveStartTime, effectiveEndTime, calculatedDuration);

                var overtime = new EmployeeOvertime
                {
                    EmployeeId = dto.EmployeeId,
                    OverTimeType = overtimeType,
                    EntryMode = dto.EntryMode,
                    HolidayId = holiday?.Id,
                    OvertimeDate = dto.OvertimeDate,
                    StartTime = effectiveStartTime,
                    EndTime = effectiveEndTime,
                    CrossesMidnight = crossesMidnight,
                    DurationInHours = calculatedDuration,
                    StandardDayHours = dto.StandardDayHours,
                    RateRuleId = rateRule?.Id,
                    RateRuleCodeApplied = rateRule?.Code,
                    RateRuleNameApplied = rateRule?.NameFr,
                    RateMultiplierApplied = rateRule?.Multiplier ?? 1.00m,
                    MultiplierCalculationDetails = rateRule != null ? CreateCalculationDetails(rateRule) : null,
                    Status = isRhOrAdmin ? OvertimeStatus.Approved : OvertimeStatus.Draft,
                    ApprovedBy = isRhOrAdmin ? userId : (int?)null,
                    ApprovedAt = isRhOrAdmin ? DateTimeOffset.UtcNow : (DateTimeOffset?)null,
                    EmployeeComment = dto.EmployeeComment?.Trim(),
                    CreatedBy = userId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                overtimesToCreate.Add(overtime);
            }

            // Si RH ou Admin, approuver automatiquement tous les overtimes (y compris les segments split)
            if (isRhOrAdmin)
            {
                foreach (var ot in overtimesToCreate)
                {
                    ot.Status = OvertimeStatus.Approved;
                    ot.ApprovedBy = userId;
                    ot.ApprovedAt = DateTimeOffset.UtcNow;
                }
            }

            // Sauvegarder tous les overtimes
            _db.EmployeeOvertimes.AddRange(overtimesToCreate);
            await _db.SaveChangesAsync();

            // Retourner le resultat approprie
            if (overtimesToCreate.Count == 1)
            {
                var result = await GetById(overtimesToCreate[0].Id);
                return CreatedAtAction(nameof(GetById), new { id = overtimesToCreate[0].Id }, result.Value);
            }
            else
            {
                // Plusieurs overtimes crees (split)
                var results = new List<EmployeeOvertimeReadDto>();
                foreach (var ot in overtimesToCreate)
                {
                    var dto_result = await GetById(ot.Id);
                    if (dto_result.Value != null)
                        results.Add(dto_result.Value);
                }

                return Ok(new
                {
                    Message = $"{overtimesToCreate.Count} segments d'overtime crees avec succes (split automatique)",
                    SplitBatchId = overtimesToCreate[0].SplitBatchId,
                    TotalSegments = overtimesToCreate.Count,
                    Overtimes = results
                });
            }
        }

        /// <summary>
        /// Cree plusieurs overtimes en cas de split (traverse minuit ou traverse frontiere jour/nuit)
        /// Divise en segments selon: jour (6h-21h) et nuit (21h-6h)
        /// </summary>
        private async Task<List<EmployeeOvertime>> CreateSplitOvertimes(
            EmployeeOvertimeCreateDto dto,
            Models.Employee.Employee employee,
            int userId,
            TimeOnly startTime,
            TimeOnly endTime,
            int? holidayId)
        {
            var splitBatchId = Guid.NewGuid();
            var overtimes = new List<EmployeeOvertime>();

            // Definir les frontieres jour/nuit
            var dayStart = new TimeOnly(6, 0);   // 6h matin = debut du jour
            var dayEnd = new TimeOnly(21, 0);    // 21h soir = fin du jour, debut de la nuit
            var midnight = new TimeOnly(0, 0);

            var currentDate = dto.OvertimeDate;
            var currentStart = startTime;
            var sequence = 1;
            var crossesMidnight = endTime < startTime;
            var nextDay = crossesMidnight ? dto.OvertimeDate.AddDays(1) : dto.OvertimeDate;

            // Liste des points de coupure (frontieres jour/nuit et minuit)
            var breakPoints = new List<TimeOnly> { dayStart, dayEnd, midnight };
            breakPoints = breakPoints.OrderBy(t => t).ToList();

            // Generer les segments
            while (true)
            {
                TimeOnly segmentEnd;
                bool isLastSegment = false;
                DateOnly segmentDate = currentDate;

                // Determiner la fin du segment actuel
                if (crossesMidnight)
                {
                    // Cas 1: On est le jour 1, avant minuit
                    if (currentDate == dto.OvertimeDate)
                    {
                        // Trouver le prochain point de coupure avant minuit
                        var nextBreak = breakPoints.Where(bp => bp > currentStart && bp <= dayEnd).FirstOrDefault();
                        
                        if (nextBreak == default(TimeOnly))
                        {
                            // Pas de coupure avant minuit, aller jusqu'a minuit
                            segmentEnd = new TimeOnly(23, 59, 59);
                        }
                        else
                        {
                            segmentEnd = nextBreak;
                        }

                        // Si on depasse endTime (qui est le lendemain), on va jusqu'a minuit
                        if (segmentEnd > new TimeOnly(23, 0, 0))
                        {
                            segmentEnd = new TimeOnly(23, 59, 59);
                        }
                    }
                    // Cas 2: On est le jour 2 (apres minuit)
                    else
                    {
                        // Trouver le prochain point de coupure ou finir a endTime
                        var nextBreak = breakPoints.Where(bp => bp > currentStart && bp <= dayEnd).FirstOrDefault();
                        
                        if (nextBreak == default(TimeOnly) || nextBreak >= endTime)
                        {
                            segmentEnd = endTime;
                            isLastSegment = true;
                        }
                        else
                        {
                            segmentEnd = nextBreak;
                        }
                    }
                }
                else
                {
                    // Meme jour: trouver le prochain point de coupure ou finir a endTime
                    var nextBreak = breakPoints.Where(bp => bp > currentStart && bp < endTime).FirstOrDefault();
                    
                    if (nextBreak == default(TimeOnly))
                    {
                        segmentEnd = endTime;
                        isLastSegment = true;
                    }
                    else
                    {
                        segmentEnd = nextBreak;
                    }
                }

                // Calculer la duree du segment
                decimal segmentDuration;
                if (currentStart > segmentEnd)
                {
                    // Traverse minuit dans ce segment
                    segmentDuration = (decimal)(TimeSpan.FromHours(24) - (currentStart - segmentEnd)).TotalHours;
                }
                else
                {
                    segmentDuration = (decimal)(segmentEnd - currentStart).TotalHours;
                }

                if (segmentDuration <= 0)
                    break;

                // Determiner si c'est jour ou nuit
                // Jour: 6h-21h, Nuit: 21h-6h
                // Comme on split aux frontieres, chaque segment est soit jour soit nuit
                bool isNightSegment = false;
                
                // Nuit = 21h a 6h (le lendemain)
                // Si le segment commence a 21h (dayEnd) ou apres, c'est de la nuit
                if (currentStart >= dayEnd)
                {
                    isNightSegment = true;
                }
                // Si le segment commence avant 6h (dayStart) et finit avant ou a 6h, c'est de la nuit
                else if (currentStart < dayStart && segmentEnd <= dayStart)
                {
                    isNightSegment = true;
                }
                // Si le segment est entre 6h (inclus) et 21h (inclus), c'est du jour
                else if (currentStart >= dayStart && segmentEnd <= dayEnd)
                {
                    isNightSegment = false;
                }
                // Cas special: segment qui va de minuit (00:00) jusqu'a avant 6h
                else if (currentStart == midnight && segmentEnd < dayStart)
                {
                    isNightSegment = true;
                }
                // Si le segment commence exactement a 6h, c'est du jour (sauf si on finit apres 21h, mais ca ne devrait pas arriver avec le split)
                else if (currentStart == dayStart)
                {
                    isNightSegment = false;
                }
                // Par defaut: si on n'est pas sur, utiliser la position de debut
                else
                {
                    // Fallback: si on commence avant 6h ou apres 21h, c'est de la nuit
                    isNightSegment = currentStart < dayStart || currentStart >= dayEnd;
                }

                // Determiner le type d'overtime pour cette date
                // Pour chaque segment, verifier s'il y a un jour ferie pour cette date specifique
                int? segmentHolidayId = null;
                if (segmentDate == dto.OvertimeDate)
                {
                    // Utiliser le jour ferie deja trouve pour le jour initial
                    segmentHolidayId = holidayId;
                }
                else
                {
                    // Pour le jour suivant, verifier s'il y a un jour ferie
                    var nextDayHoliday = await _db.Holidays
                        .Where(h => h.DeletedAt == null && h.IsActive)
                        .Where(h => h.HolidayDate == segmentDate)
                        .Where(h => h.CompanyId == null || h.CompanyId == employee.CompanyId)
                        .OrderByDescending(h => h.CompanyId) // Priorite aux jours feries de l'entreprise
                        .FirstOrDefaultAsync();
                    
                    segmentHolidayId = nextDayHoliday?.Id;
                }
                
                var segmentType = await DetermineOvertimeType(segmentDate, employee.CompanyId, segmentHolidayId);
                
                if (isNightSegment)
                {
                    segmentType |= OvertimeType.Night;
                }

                Console.WriteLine($"[CreateSplitOvertimes] Segment {sequence}: Date={segmentDate}, Start={currentStart}, End={segmentEnd}, Type={segmentType} ({(int)segmentType}), Night={isNightSegment}, HolidayId={segmentHolidayId}");

                // Trouver la regle de taux
                var rateRule = await FindBestRateRule(segmentDate, segmentType, currentStart, segmentEnd, segmentDuration);
                
                Console.WriteLine($"[CreateSplitOvertimes] Segment {sequence}: RateRule={rateRule?.Code ?? "NULL"}, Multiplier={rateRule?.Multiplier ?? 1.00m}");

                // Creer le segment
                overtimes.Add(new EmployeeOvertime
                {
                    EmployeeId = dto.EmployeeId,
                    OverTimeType = segmentType,
                    EntryMode = dto.EntryMode,
                    HolidayId = segmentHolidayId,
                    OvertimeDate = segmentDate,
                    StartTime = currentStart,
                    EndTime = segmentEnd,
                    CrossesMidnight = false, // Les segments individuels ne traversent pas minuit
                    DurationInHours = segmentDuration,
                    StandardDayHours = dto.StandardDayHours,
                    RateRuleId = rateRule?.Id,
                    RateRuleCodeApplied = rateRule?.Code,
                    RateRuleNameApplied = rateRule?.NameFr,
                    RateMultiplierApplied = rateRule?.Multiplier ?? 1.00m,
                    MultiplierCalculationDetails = rateRule != null ? CreateCalculationDetails(rateRule) : null,
                    SplitBatchId = splitBatchId,
                    SplitSequence = sequence,
                    SplitTotalSegments = 0, // Sera mis a jour a la fin
                    Status = OvertimeStatus.Draft,
                    EmployeeComment = dto.EmployeeComment?.Trim(),
                    CreatedBy = userId,
                    CreatedAt = DateTimeOffset.UtcNow
                });

                sequence++;

                // Si c'est le dernier segment, on a fini
                if (isLastSegment)
                    break;

                // Preparer le segment suivant
                if (segmentEnd == new TimeOnly(23, 59, 59))
                {
                    // Passer au jour suivant
                    currentDate = currentDate.AddDays(1);
                    currentStart = midnight;
                }
                else
                {
                    currentStart = segmentEnd;
                }
            }

            // Mettre a jour le nombre total de segments
            foreach (var ot in overtimes)
            {
                ot.SplitTotalSegments = overtimes.Count;
            }

            return overtimes;
        }

        /// <summary>
        /// Determine le type d'overtime pour une date donnee
        /// FIX: Ne pas inclure Standard par defaut si c'est ferie/repos
        /// </summary>
        private async Task<OvertimeType> DetermineOvertimeType(DateOnly date, int companyId, int? holidayId)
        {
            // FIX: Commencer avec None au lieu de Standard
            var type = OvertimeType.None;

            // Verifier jour ferie
            Console.WriteLine("================================");
            Console.WriteLine("================================");
            Console.WriteLine("==============Try verification jour férié==================");
            if (holidayId.HasValue || await _db.Holidays
                .AnyAsync(h => h.HolidayDate == date && h.DeletedAt == null && h.IsActive &&
                              (h.CompanyId == null || h.CompanyId == companyId)))
            {
                Console.WriteLine("================================");
                Console.WriteLine("================================");
                Console.WriteLine("================================");
                type |= OvertimeType.PublicHoliday;
                Console.WriteLine($"The Holiday type est {type}");
                return type; // Ferie prime sur repos hebdomadaire
            }

            // Verifier repos hebdomadaire
            var dayOfWeek = (int)date.DayOfWeek; // 0=Sunday, 1=Monday, ..., 6=Saturday
            var workingCalendarDay = await _db.WorkingCalendars
                .Where(wc => wc.CompanyId == companyId
                          && wc.DayOfWeek == dayOfWeek
                          && wc.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (workingCalendarDay != null && !workingCalendarDay.IsWorkingDay)
            {
                type |= OvertimeType.WeeklyRest;
                return type;
            }

            // Seulement si ce n'est ni ferie ni repos, alors c'est Standard
            return OvertimeType.Standard;
        }

        /// <summary>
        /// Verifie si une plage horaire chevauche les heures de nuit
        /// Nuit = 21h-6h, Jour = 6h-21h
        /// </summary>
        private bool CheckNightWorkOverlap(TimeOnly start, TimeOnly end, bool crossesMidnight, TimeOnly nightStart, TimeOnly nightEnd)
        {
            // Nuit: 21h (nightStart) a 6h (nightEnd) le lendemain
            // Jour: 6h a 21h

            if (!crossesMidnight)
            {
                // Cas simple: meme jour
                // La plage est de nuit si:
                // - Elle commence apres 21h (dans la nuit)
                // - OU elle finit avant 6h (dans la nuit)
                // - OU elle commence avant 6h ET finit apres 6h (traverse la frontiere 6h)
                // - OU elle commence avant 21h ET finit apres 21h (traverse la frontiere 21h)
                
                if (start >= nightStart) // Commence apres 21h = nuit
                    return true;
                
                if (end <= nightEnd) // Finit avant 6h = nuit
                    return true;
                
                // Traverse la frontiere 6h (de nuit vers jour)
                if (start < nightEnd && end > nightEnd)
                    return true;
                
                // Traverse la frontiere 21h (de jour vers nuit)
                if (start < nightStart && end > nightStart)
                    return true;
                
                return false;
            }
            else
            {
                // La plage traverse minuit
                // Si elle commence avant 21h, elle traverse forcement la nuit (21h-minuit)
                // Si elle finit apres 6h, elle traverse forcement la nuit (minuit-6h)
                // Donc elle touche toujours la nuit
                return true;
            }
        }

        /// <summary>
        /// Trouve la meilleure regle de majoration applicable
        /// FIX: Utilise un systeme de ranking base sur la specificite (nombre de flags couverts)
        /// </summary>
        private async Task<Models.Referentiel.OvertimeRateRule?> FindBestRateRule(
     DateOnly date,
     OvertimeType overtimeType,
     TimeOnly? startTime,
     TimeOnly? endTime,
     decimal duration)
        {
            // ===== LOGS: Parametres d'entree =====
            Console.WriteLine($"[FindBestRateRule] ===== DEBUT RECHERCHE REGLE =====");
            Console.WriteLine($"[FindBestRateRule] Date: {date}, Type: {overtimeType} ({(int)overtimeType}), Start: {startTime}, End: {endTime}, Duration: {duration}h");

            // Recuperer toutes les regles actives et valides a cette date
            var rules = await _db.OvertimeRateRules
                .Where(r => r.DeletedAt == null && r.IsActive)
                .ToListAsync();

            if (!rules.Any())
            {
                Console.WriteLine($"[FindBestRateRule] ❌ Aucune regle active trouvee pour la date {date}");
                return null;
            }

            Console.WriteLine($"[FindBestRateRule] 📋 {rules.Count} regles actives trouvees pour la date {date}");

            // ===== ETAPE 1: Filtrer par compatibilite stricte sur les flags =====
            // On considère "specific" = tout sauf Standard
            var requestedSpecificFlags = overtimeType & ~OvertimeType.Standard;
            var requestIsPureStandard = requestedSpecificFlags == OvertimeType.None;

            bool IsRuleTypeApplicable(OvertimeType ruleAppliesTo)
            {
                var ruleSpecificFlags = ruleAppliesTo & ~OvertimeType.Standard;
                var ruleHasStandard = (ruleAppliesTo & OvertimeType.Standard) != 0;

                // Cas A: la demande est "Standard pur" (aucun flag spécifique)
                if (requestIsPureStandard)
                {
                    // Règle doit être Standard ET ne demander aucun flag spécifique
                    return ruleHasStandard && ruleSpecificFlags == OvertimeType.None;
                }

                // Cas B: la demande a au moins 1 flag spécifique (Holiday/Rest/Night/...)
                // La règle est applicable si ses flags spécifiques sont un sous-ensemble de ceux demandés.
                // => Empêche HOLIDAY_NIGHT de matcher HOLIDAY.
                return (ruleSpecificFlags & requestedSpecificFlags) == ruleSpecificFlags;
            }

            var applicableRules = rules
                .Where(r => IsRuleTypeApplicable(r.AppliesTo))
                .ToList();

            if (!applicableRules.Any())
            {
                Console.WriteLine($"[FindBestRateRule] ⚠️ Aucune regle avec matching strict, fallback sur overlap partiel");

                // Fallback (si tu veux absolument un fallback) : au moins 1 flag commun,
                // sinon Standard.
                applicableRules = rules.Where(r =>
                {
                    var ruleSpecificFlags = r.AppliesTo & ~OvertimeType.Standard;

                    if (!requestIsPureStandard)
                        return (ruleSpecificFlags & requestedSpecificFlags) != OvertimeType.None;

                    return (r.AppliesTo & OvertimeType.Standard) != 0;
                }).ToList();
            }

            if (!applicableRules.Any())
            {
                Console.WriteLine($"[FindBestRateRule] ❌ Aucune regle ne correspond au type {overtimeType} (valeur: {(int)overtimeType})");
                Console.WriteLine($"[FindBestRateRule] Types disponibles: {string.Join(", ", rules.Select(r => $"{r.Code}:{(int)r.AppliesTo}"))}");
                return null;
            }

            Console.WriteLine($"[FindBestRateRule] ✅ {applicableRules.Count} regles passent le filtre de type");

            // ===== ETAPE 2: Filtrer selon la duree =====
            var beforeDurationFilter = applicableRules.Count;
            applicableRules = applicableRules
                .Where(r => (!r.MinimumDurationHours.HasValue || duration >= r.MinimumDurationHours.Value) &&
                           (!r.MaximumDurationHours.HasValue || duration <= r.MaximumDurationHours.Value))
                .ToList();

            if (!applicableRules.Any())
            {
                Console.WriteLine($"[FindBestRateRule] ❌ Aucune regle ne correspond a la duree {duration}h");
                Console.WriteLine($"[FindBestRateRule] Regles exclues par duree: {beforeDurationFilter - applicableRules.Count}");
                return null;
            }

            Console.WriteLine($"[FindBestRateRule] ✅ {applicableRules.Count} regles passent le filtre de duree (exclues: {beforeDurationFilter - applicableRules.Count})");

            // ===== ETAPE 3: Filtrer selon la plage horaire =====
            if (startTime.HasValue && endTime.HasValue)
            {
                var beforeTimeFilter = applicableRules.Count;
                applicableRules = applicableRules
                    .Where(r =>
                    {
                        if (r.TimeRangeType == TimeRangeType.AllDay)
                            return true;

                        if (!r.StartTime.HasValue || !r.EndTime.HasValue)
                            return false;

                        return CheckTimeRangeOverlap(
                            startTime.Value,
                            endTime.Value,
                            r.StartTime.Value,
                            r.EndTime.Value,
                            r.TimeRangeType);
                    })
                    .ToList();

                Console.WriteLine($"[FindBestRateRule] ✅ {applicableRules.Count} regles passent le filtre horaire (exclues: {beforeTimeFilter - applicableRules.Count}, plage: {startTime.Value}-{endTime.Value})");
            }

            // ===== ETAPE 4: Filtrer selon le jour de la semaine =====
            var dayOfWeek = (int)date.DayOfWeek;
            var dayBitmask = 1 << dayOfWeek;

            var beforeDayFilter = applicableRules.Count;

            var dayFilteredRules = applicableRules
                .Where(r =>
                {
                    // Pas de restriction => OK
                    if (!r.ApplicableDaysOfWeek.HasValue)
                        return true;

                    // Warning configuration: Standard/Night ne devrait pas avoir ApplicableDaysOfWeek
                    // mais on l'accepte pour ne pas bloquer.
                    var ruleSpecificFlags = r.AppliesTo & ~OvertimeType.Standard;
                    var ruleIsOnlyStandardOrNight =
                        ((r.AppliesTo & (OvertimeType.Standard | OvertimeType.Night)) != 0) &&
                        (ruleSpecificFlags == OvertimeType.Night || ruleSpecificFlags == OvertimeType.None);

                    if (ruleIsOnlyStandardOrNight)
                    {
                        Console.WriteLine($"[FindBestRateRule] ⚠️ WARNING: Règle {r.Code} est Standard/Night mais a ApplicableDaysOfWeek défini (configuration incorrecte)");
                        return true;
                    }

                    return (r.ApplicableDaysOfWeek.Value & dayBitmask) != 0;
                })
                .ToList();

            if (!dayFilteredRules.Any())
            {
                Console.WriteLine($"[FindBestRateRule] ❌ Aucune regle ne correspond au jour de la semaine (jour {dayOfWeek}, bitmask {dayBitmask})");
                Console.WriteLine($"[FindBestRateRule] Regles exclues par jour: {beforeDayFilter - applicableRules.Count}");
                return null;
            }

            applicableRules = dayFilteredRules;
            Console.WriteLine($"[FindBestRateRule] ✅ {applicableRules.Count} regles passent le filtre jour de semaine (exclues: {beforeDayFilter - applicableRules.Count})");

            // ===== ETAPE 5: Ranking par specificite et priorite =====
            // Objectif:
            // - préférer la règle la plus "spécifique" (plus de flags spécifiques)
            // - donner un bonus si match EXACT des flags spécifiques
            // - pénaliser Standard quand on est dans un contexte spécifique
            var rankedRules = applicableRules
                .Select(r =>
                {
                    var ruleSpecificFlags = r.AppliesTo & ~OvertimeType.Standard;

                    // "covered" = flags de la règle qui sont présents dans la requête
                    var flagsCovered = CountFlags(r.AppliesTo & overtimeType);

                    // Bonus exact = égalité stricte sur les flags spécifiques (vrai exact)
                    var exactMatchBonus =
                        (!requestIsPureStandard && ruleSpecificFlags == requestedSpecificFlags)
                            ? 1000
                            : 0;

                    // Spécificité = nombre de flags spécifiques dans la règle (pas juste covered)
                    var specificityScore = CountFlags(ruleSpecificFlags) * 100;

                    // Pénalité si règle contient Standard alors qu'on est en contexte spécifique
                    var standardPenalty =
                        (!requestIsPureStandard && (r.AppliesTo & OvertimeType.Standard) != 0)
                            ? -500
                            : 0;

                    // Score plage horaire
                    var timeRangeScore = 0;
                    if (startTime.HasValue && endTime.HasValue && r.StartTime.HasValue && r.EndTime.HasValue && r.TimeRangeType != TimeRangeType.AllDay)
                    {
                        var ruleDuration = r.EndTime.Value < r.StartTime.Value
                            ? (24 - (r.StartTime.Value - r.EndTime.Value).TotalHours)
                            : (r.EndTime.Value - r.StartTime.Value).TotalHours;

                        timeRangeScore = (int)(50 - Math.Abs(ruleDuration - (double)duration));
                    }
                    else if (r.TimeRangeType == TimeRangeType.AllDay)
                    {
                        timeRangeScore = 0;
                    }

                    var totalScore = exactMatchBonus + specificityScore + standardPenalty + timeRangeScore - r.Priority;

                    return new
                    {
                        Rule = r,
                        FlagsCovered = flagsCovered,
                        SpecificityScore = specificityScore,
                        ExactMatchBonus = exactMatchBonus,
                        StandardPenalty = standardPenalty,
                        TimeRangeScore = timeRangeScore,
                        TotalScore = totalScore
                    };
                })
                .OrderByDescending(x => x.TotalScore)
                .ThenBy(x => x.Rule.Priority)
                .ToList();

            // ===== LOGS: Details des regles candidates =====
            Console.WriteLine($"[FindBestRateRule] 📊 RANKING DES REGLES CANDIDATES:");
            foreach (var ranked in rankedRules)
            {
                var r = ranked.Rule;
                Console.WriteLine(
                    $"  [{ranked.TotalScore}] {r.Code} | AppliesTo: {r.AppliesTo} ({(int)r.AppliesTo}) | " +
                    $"Multiplier: {r.Multiplier} | Priority: {r.Priority} | " +
                    $"TimeRange: {r.StartTime}-{r.EndTime} ({r.TimeRangeType}) | " +
                    $"Duration: {r.MinimumDurationHours?.ToString() ?? "min"} - {r.MaximumDurationHours?.ToString() ?? "max"} | " +
                    $"Score: flags={ranked.FlagsCovered}, spec={ranked.SpecificityScore}, exact={ranked.ExactMatchBonus}, penalty={ranked.StandardPenalty}, time={ranked.TimeRangeScore}"
                );
            }

            var bestRule = rankedRules.First().Rule;

            Console.WriteLine($"[FindBestRateRule] ✅ REGLE SELECTIONNEE: {bestRule.Code} (Multiplier: {bestRule.Multiplier}, Priority: {bestRule.Priority}, Score: {rankedRules.First().TotalScore})");
            Console.WriteLine($"[FindBestRateRule] ===== FIN RECHERCHE REGLE =====");

            return bestRule;
        }

        /// <summary>
        /// Compte le nombre de flags actifs dans un OvertimeType
        /// </summary>
        private int CountFlags(OvertimeType type)
        {
            if (type == OvertimeType.None)
                return 0;
            
            int count = 0;
            int value = (int)type;
            while (value != 0)
            {
                count++;
                value &= value - 1; // Enleve le bit le moins significatif
            }
            return count;
        }

        /// <summary>
        /// Verifie si deux plages horaires se chevauchent
        /// Gere les plages qui traversent minuit (ex: 21:00-06:00)
        /// </summary>
        private bool CheckTimeRangeOverlap(TimeOnly start1, TimeOnly end1, TimeOnly start2, TimeOnly end2, TimeRangeType rangeType)
        {
            if (rangeType == TimeRangeType.AllDay)
                return true;

            // Determiner si chaque plage traverse minuit
            bool range1CrossesMidnight = end1 < start1 || (start1 == end1 && start1 != new TimeOnly(0, 0));
            bool range2CrossesMidnight = end2 < start2 || (start2 == end2 && start2 != new TimeOnly(0, 0));

            // Cas 1: Aucune plage ne traverse minuit
            if (!range1CrossesMidnight && !range2CrossesMidnight)
            {
                return start1 < end2 && end1 > start2;
            }

            // Cas 2: Les deux plages traversent minuit
            if (range1CrossesMidnight && range2CrossesMidnight)
            {
                // Les deux traversent minuit, donc elles se chevauchent forcement
                return true;
            }

            // Cas 3: Une seule plage traverse minuit
            if (range1CrossesMidnight)
            {
                // range1 traverse minuit (ex: 21:00-06:00), range2 ne traverse pas
                // range1 couvre: [start1, 23:59:59] U [00:00:00, end1]
                // Donc range2 chevauche si:
                // - Il commence apres start1 (dans la premiere partie)
                // - OU il finit avant end1 (dans la deuxieme partie)
                return start2 >= start1 || end2 <= end1;
            }
            else // range2CrossesMidnight
            {
                // range2 traverse minuit (ex: 21:00-06:00), range1 ne traverse pas
                // range2 couvre: [start2, 23:59:59] U [00:00:00, end2]
                // Donc range1 chevauche si:
                // - Il commence apres start2 (dans la premiere partie)
                // - OU il finit avant end2 (dans la deuxieme partie)
                return start1 >= start2 || end1 <= end2;
            }
        }

        /// <summary>
        /// Cree les details du calcul de majoration (JSON)
        /// </summary>
        private string CreateCalculationDetails(Models.Referentiel.OvertimeRateRule rule)
        {
            var details = new
            {
                RuleCode = rule.Code,
                RuleName = rule.NameFr,
                Multiplier = rule.Multiplier,
                AppliedOn = DateTime.UtcNow,
                Category = rule.Category
            };

            return JsonSerializer.Serialize(details);
        }

        // === AUTRES ENDPOINTS (Submit, Approve, Update, Delete) ===
        // Ces endpoints restent identiques au code original...

        /// <summary>
        /// Soumet un overtime pour approbation
        /// PUT /api/employee-overtimes/5/submit
        /// </summary>
        [HttpPut("{id}/submit")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeOvertimeReadDto>> Submit(int id, [FromBody] EmployeeOvertimeSubmitDto dto)
        {
            var userId = User.GetUserId();

            var overtime = await _db.EmployeeOvertimes
                .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

            if (overtime == null)
                return NotFound(new { Message = "Overtime non trouve" });

            if (overtime.Status != OvertimeStatus.Draft)
                return BadRequest(new { Message = "Seuls les overtimes en brouillon peuvent etre soumis" });

            // Verifier que l'utilisateur est l'employe ou son manager
            var user = await _db.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId);

            // ===== VÉRIFIER LE RÔLE DE L'UTILISATEUR ACTUEL =====
            var currentUser = await _db.Users
                .AsNoTracking()
                .Include(u => u.UsersRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            bool isRH = currentUser?.UsersRoles?.Any(ur =>
                ur.Role.Name.Equals("RH", StringComparison.OrdinalIgnoreCase)
            ) ?? false;

            if (user?.EmployeeId != overtime.EmployeeId && isRH != true)
                return Forbid();

            overtime.Status = OvertimeStatus.Submitted;
            overtime.EmployeeComment = dto.EmployeeComment?.Trim();
            overtime.ModifiedBy = userId;
            overtime.ModifiedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync();

            var result = await GetById(id);
            return Ok(result.Value);
        }

        /// <summary>
        /// Annuler un overtime soumis (employe)
        /// PUT /api/employee-overtimes/5/cancel
        /// </summary>
        [HttpPut("{id}/cancel")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeOvertimeReadDto>> Cancel(int id)
        {
            var userId = User.GetUserId();

            var user = await _db.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId);
            // Vérifie que l'utilisateur est un employé (seuls les employés peuvent annuler leurs demandes) ou RH de leur company
            var isRH = await _db.UsersRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == "RH" && ur.User.Employee.CompanyId == user.Employee.CompanyId);
            if (user?.EmployeeId == null && !isRH)
                return Forbid();

            var overtime = await _db.EmployeeOvertimes
                .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);
            if (overtime == null)
                return NotFound(new { Message = "Overtime non trouve" });
            // les overtime soumis, approuves peuvent etre annules par l'employe, pas les autres
                if (overtime.Status != OvertimeStatus.Submitted && overtime.Status != OvertimeStatus.Approved)
                    return BadRequest(new { Message = "Seuls les overtimes soumis ou approuves peuvent etre annules" });

            overtime.Status = OvertimeStatus.Cancelled;
            overtime.ModifiedBy = userId;
            overtime.ModifiedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();
            var result = await GetById(id);
            return Ok(result.Value);
        }
       
        /// <summary>
        /// Approuve ou rejette un overtime (Manager)
        /// PUT /api/employee-overtimes/5/approve
        /// </summary>
        [HttpPut("{id}/approve")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeOvertimeReadDto>> Approve(int id, [FromBody] EmployeeOvertimeApprovalDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.Status != OvertimeStatus.Approved && dto.Status != OvertimeStatus.Rejected)
                return BadRequest(new { Message = "Status doit etre Approved ou Rejected" });

            var userId = User.GetUserId();

            var overtime = await _db.EmployeeOvertimes
                .Include(o => o.Employee)
                .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

            if (overtime == null)
                return NotFound(new { Message = "Overtime non trouve" });

            // Si la demande est faite par un RH, il peut la approver meme si elle en draft
            //if (overtime.Status != OvertimeStatus.Submitted )
            //    return BadRequest(new { Message = "Seuls les overtimes soumis/ peuvent etre approuves/rejetes" });

            // Verifier que l'utilisateur est le manager de l'employe ou un RH
            // Verification du RH par Role == RH
            var user = await _db.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == userId);

            // ===== VÉRIFIER LE RÔLE DE L'UTILISATEUR ACTUEL =====
            var currentUser = await _db.Users
                .AsNoTracking()
                .Include(u => u.UsersRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            bool isRH = currentUser?.UsersRoles?.Any(ur =>
                ur.Role.Name.Equals("RH", StringComparison.OrdinalIgnoreCase)
            ) ?? false;

            Console.WriteLine($"isRH is : {isRH}");

            if (user?.EmployeeId != overtime.Employee.ManagerId  && isRH != true) 
                return Forbid();

            overtime.Status = dto.Status;
            overtime.ManagerComment = dto.ManagerComment?.Trim();
            overtime.ApprovedBy = userId;
            overtime.ApprovedAt = DateTimeOffset.UtcNow;
            overtime.ModifiedBy = userId;
            overtime.ModifiedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync();

            var result = await GetById(id);
            return Ok(result.Value);
        }
 
        /// <summary>
        /// Met e jour un overtime (avant soumission)
        /// PUT /api/employee-overtimes/5
        /// </summary>
        [HttpPut("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<EmployeeOvertimeReadDto>> Update(int id, [FromBody] EmployeeOvertimeUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            var overtime = await _db.EmployeeOvertimes
                .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

            if (overtime == null)
                return NotFound(new { Message = "Overtime non trouve" });

            if (overtime.Status != OvertimeStatus.Draft)
                return BadRequest(new { Message = "Seuls les overtimes en brouillon peuvent etre modifies" });

            bool hasChanges = false;

            if (dto.OvertimeDate.HasValue && dto.OvertimeDate != overtime.OvertimeDate)
            {
                overtime.OvertimeDate = dto.OvertimeDate.Value;
                hasChanges = true;
            }

            if (dto.DurationInHours.HasValue && dto.DurationInHours != overtime.DurationInHours)
            {
                overtime.DurationInHours = dto.DurationInHours.Value;
                hasChanges = true;
            }

            if (dto.StartTime.HasValue && dto.StartTime != overtime.StartTime)
            {
                overtime.StartTime = dto.StartTime.Value;
                hasChanges = true;
            }

            if (dto.EndTime.HasValue && dto.EndTime != overtime.EndTime)
            {
                overtime.EndTime = dto.EndTime.Value;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.EmployeeComment) && dto.EmployeeComment != overtime.EmployeeComment)
            {
                overtime.EmployeeComment = dto.EmployeeComment.Trim();
                hasChanges = true;
            }

            if (hasChanges)
            {
                overtime.ModifiedBy = userId;
                overtime.ModifiedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();
            }

            var result = await GetById(id);
            return Ok(result.Value);
        }

        /// <summary>
        /// Supprime un overtime (soft delete)
        /// DELETE /api/employee-overtimes/5
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();

            var overtime = await _db.EmployeeOvertimes
                .FirstOrDefaultAsync(o => o.Id == id && o.DeletedAt == null);

            if (overtime == null)
                return NotFound(new { Message = "Overtime non trouve" });

            if (overtime.IsProcessedInPayroll)
                return BadRequest(new { Message = "Impossible de supprimer un overtime deje traite en paie" });

            overtime.DeletedBy = userId;
            overtime.DeletedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
