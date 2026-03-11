using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Company;
using payzen_backend.Models.Company.Dtos;
using payzen_backend.Models.Permissions.Dtos;

namespace payzen_backend.Controllers.v1.SystemData
{
    [Route("api/v{version:apiVersion}/holidays")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class HolidaysController : ControllerBase
    {
        private readonly AppDbContext _db;

        public HolidaysController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// R�cup�re tous les jours f�ri�s
        /// GET /api/holidays?countryId=1&scope=Global&year=2025
        /// GET /api/holidays?companyId=5&year=2025
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<HolidayReadDto>>> GetHolidays(
            [FromQuery] int? countryId = null,
            [FromQuery] int? companyId = null,
            [FromQuery] HolidayScope? scope = null,
            [FromQuery] int? year = null,
            [FromQuery] string? holidayType = null,
            [FromQuery] bool? isActive = null)
        {
            var query = _db.Holidays
                .AsNoTracking()
                .Where(h => h.DeletedAt == null);

            // Filtrer par pays
            if (countryId.HasValue)
                query = query.Where(h => h.CountryId == countryId.Value);

            // Filtrer par soci�t�
            if (companyId.HasValue)
            {
                var companyExists = await _db.Companies
                    .AnyAsync(c => c.Id == companyId && c.DeletedAt == null);

                if (!companyExists)
                    return NotFound(new { Message = "Soci�t� non trouv�e" });

                query = query.Where(h => h.CompanyId == companyId.Value || h.Scope == HolidayScope.Global);
            }

            // Filtrer par scope
            if (scope.HasValue)
                query = query.Where(h => h.Scope == scope.Value);

            // Filtrer par ann�e
            if (year.HasValue)
                query = query.Where(h => h.Year == year.Value || h.HolidayDate.Year == year.Value);

            // Filtrer par type
            if (!string.IsNullOrWhiteSpace(holidayType))
                query = query.Where(h => h.HolidayType == holidayType.Trim());

            // Filtrer par statut actif
            if (isActive.HasValue)
                query = query.Where(h => h.IsActive == isActive.Value);

            var holidays = await query
                .Include(h => h.Company)
                .Include(h => h.Country)
                .OrderBy(h => h.HolidayDate)
                .ThenBy(h => h.NameFr)
                .Select(h => new HolidayReadDto
                {
                    Id = h.Id,
                    NameFr = h.NameFr,
                    NameAr = h.NameAr,
                    NameEn = h.NameEn,
                    HolidayDate = h.HolidayDate,
                    Description = h.Description,
                    CompanyId = h.CompanyId,
                    CompanyName = h.Company != null ? h.Company.CompanyName : null,
                    CountryId = h.CountryId,
                    CountryName = h.Country!.CountryName,
                    Scope = h.Scope,
                    ScopeDescription = h.Scope == HolidayScope.Global ? "Global" : "Entreprise",
                    HolidayType = h.HolidayType,
                    IsMandatory = h.IsMandatory,
                    IsPaid = h.IsPaid,
                    IsRecurring = h.IsRecurring,
                    RecurrenceRule = h.RecurrenceRule,
                    Year = h.Year,
                    AffectPayroll = h.AffectPayroll,
                    AffectAttendance = h.AffectAttendance,
                    IsActive = h.IsActive,
                    CreatedAt = h.CreatedAt.DateTime
                })
                .ToListAsync();

            return Ok(holidays);
        }

        /// <summary>
        /// R�cup�re un jour f�ri� par ID
        /// GET /api/holidays/5
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<HolidayReadDto>> GetHolidayById(int id)
        {
            var holiday = await _db.Holidays
                .AsNoTracking()
                .Where(h => h.Id == id && h.DeletedAt == null)
                .Include(h => h.Company)
                .Include(h => h.Country)
                .Select(h => new HolidayReadDto
                {
                    Id = h.Id,
                    NameFr = h.NameFr,
                    NameAr = h.NameAr,
                    NameEn = h.NameEn,
                    HolidayDate = h.HolidayDate,
                    Description = h.Description,
                    CompanyId = h.CompanyId,
                    CompanyName = h.Company != null ? h.Company.CompanyName : null,
                    CountryId = h.CountryId,
                    CountryName = h.Country!.CountryName,
                    Scope = h.Scope,
                    ScopeDescription = h.Scope == HolidayScope.Global ? "Global" : "Entreprise",
                    HolidayType = h.HolidayType,
                    IsMandatory = h.IsMandatory,
                    IsPaid = h.IsPaid,
                    IsRecurring = h.IsRecurring,
                    RecurrenceRule = h.RecurrenceRule,
                    Year = h.Year,
                    AffectPayroll = h.AffectPayroll,
                    AffectAttendance = h.AffectAttendance,
                    IsActive = h.IsActive,
                    CreatedAt = h.CreatedAt.DateTime
                })
                .FirstOrDefaultAsync();

            if (holiday == null)
                return NotFound(new { Message = "Jour f�ri� non trouv�" });

            return Ok(holiday);
        }

        /// <summary>
        /// Cr�e un nouveau jour f�ri�
        /// POST /api/holidays
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<object>> CreateHoliday([FromBody] HolidayCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            // V�rifier que le pays existe
            var countryExists = await _db.Countries
                .AnyAsync(c => c.Id == dto.CountryId && c.DeletedAt == null);

            if (!countryExists)
                return NotFound(new { Message = "Pays non trouv�" });

            // Si Scope = Company, v�rifier que CompanyId est fourni
            if (dto.Scope == HolidayScope.Company)
            {
                if (!dto.CompanyId.HasValue)
                    return BadRequest(new { Message = "CompanyId est requis pour un jour f�ri� de scope 'Company'" });

                var companyExists = await _db.Companies
                    .AnyAsync(c => c.Id == dto.CompanyId && c.DeletedAt == null);

                if (!companyExists)
                    return NotFound(new { Message = "Soci�t� non trouv�e" });
            }

            // Si le jour f�ri� est r�current, cr�er plusieurs entr�es
            if (dto.IsRecurring)
            {
                // Valider que RecurrenceRule et Year sont fournis
                if (string.IsNullOrWhiteSpace(dto.RecurrenceRule))
                    return BadRequest(new { Message = "RecurrenceRule est requis pour un jour f�ri� r�current" });

                // D�terminer la plage d'ann�es pour la r�currence
                int startYear = dto.Year ?? dto.HolidayDate.Year;
                int endYear = startYear + 10; // Cr�er pour les 10 prochaines ann�es

                var createdHolidays = new List<HolidayReadDto>();

                for (int year = startYear; year < endYear; year++)
                {
                    // Calculer la date pour cette ann�e
                    DateOnly holidayDate;
                    
                    try
                    {
                        // Si la r�gle de r�currence est "annual" ou similaire, conserver le m�me jour/mois
                        holidayDate = CalculateRecurringDate(dto.HolidayDate, year, dto.RecurrenceRule);
                    }
                    catch (Exception ex)
                    {
                        return BadRequest(new { Message = $"Erreur lors du calcul de la date r�currente: {ex.Message}" });
                    }

                    // V�rifier qu'il n'existe pas d�j� un jour f�ri� pour cette date/soci�t�
                    var existingQuery = _db.Holidays
                        .Where(h => h.HolidayDate == holidayDate &&
                                   h.CountryId == dto.CountryId &&
                                   h.DeletedAt == null);

                    if (dto.CompanyId.HasValue)
                        existingQuery = existingQuery.Where(h => h.CompanyId == dto.CompanyId);
                    else
                        existingQuery = existingQuery.Where(h => h.CompanyId == null);

                    // Si la date existe d�j�, passer � la suivante
                    if (await existingQuery.AnyAsync())
                        continue;

                    var holiday = new Holiday
                    {
                        NameFr = dto.NameFr.Trim(),
                        NameAr = dto.NameAr.Trim(),
                        NameEn = dto.NameEn.Trim(),
                        HolidayDate = holidayDate,
                        Description = dto.Description?.Trim(),
                        CompanyId = dto.CompanyId,
                        CountryId = dto.CountryId,
                        Scope = dto.Scope,
                        HolidayType = dto.HolidayType.Trim(),
                        IsMandatory = dto.IsMandatory,
                        IsPaid = dto.IsPaid,
                        IsRecurring = true,
                        RecurrenceRule = dto.RecurrenceRule.Trim(),
                        Year = year,
                        AffectPayroll = dto.AffectPayroll,
                        AffectAttendance = dto.AffectAttendance,
                        IsActive = dto.IsActive,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = userId
                    };

                    _db.Holidays.Add(holiday);
                    await _db.SaveChangesAsync();

                    var created = await GetHolidayById(holiday.Id);
                    if (created.Value != null)
                        createdHolidays.Add(created.Value);
                }

                return CreatedAtAction(nameof(GetHolidays), new
                {
                    Message = $"{createdHolidays.Count} jours f�ri�s r�currents cr��s avec succ�s",
                    Count = createdHolidays.Count,
                    Holidays = createdHolidays
                });
            }
            else
            {
                // Cr�ation d'un jour f�ri� non r�current (comportement original)
                var query = _db.Holidays
                    .Where(h => h.HolidayDate == dto.HolidayDate &&
                               h.CountryId == dto.CountryId &&
                               h.DeletedAt == null);

                if (dto.CompanyId.HasValue)
                    query = query.Where(h => h.CompanyId == dto.CompanyId);
                else
                    query = query.Where(h => h.CompanyId == null);

                if (await query.AnyAsync())
                    return Conflict(new { Message = "Un jour f�ri� existe d�j� pour cette date" });

                var holiday = new Holiday
                {
                    NameFr = dto.NameFr.Trim(),
                    NameAr = dto.NameAr.Trim(),
                    NameEn = dto.NameEn.Trim(),
                    HolidayDate = dto.HolidayDate,
                    Description = dto.Description?.Trim(),
                    CompanyId = dto.CompanyId,
                    CountryId = dto.CountryId,
                    Scope = dto.Scope,
                    HolidayType = dto.HolidayType.Trim(),
                    IsMandatory = dto.IsMandatory,
                    IsPaid = dto.IsPaid,
                    IsRecurring = false,
                    RecurrenceRule = null,
                    Year = dto.Year,
                    AffectPayroll = dto.AffectPayroll,
                    AffectAttendance = dto.AffectAttendance,
                    IsActive = dto.IsActive,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = userId
                };

                _db.Holidays.Add(holiday);
                await _db.SaveChangesAsync();

                var createdHoliday = await GetHolidayById(holiday.Id);
                return CreatedAtAction(nameof(GetHolidayById), new { id = holiday.Id }, createdHoliday.Value);
            }
        }

        /// <summary>
        /// Met � jour un jour f�ri�
        /// PUT /api/holidays/5
        /// </summary>
        [HttpPut("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<HolidayReadDto>> UpdateHoliday(int id, [FromBody] HolidayUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            var holiday = await _db.Holidays
                .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

            if (holiday == null)
                return NotFound(new { Message = "Jour f�ri� non trouv�" });

            // V�rifier si le jour f�ri� est de scope Global
            if (holiday.Scope == HolidayScope.Global)
            {
                // R�cup�rer l'utilisateur et ses r�les
                var user = await _db.Users
                    .Where(u => u.Id == userId)
                    .Include(u => u.UsersRoles!.Where(ur => ur.DeletedAt == null))
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync();

                // V�rifier si l'utilisateur a le r�le "Admin Payzen"
                var isAdminPayzen = user?.UsersRoles?
                    .Any(ur => ur.Role != null && 
                              ur.Role.DeletedAt == null &&
                              ur.Role.Name.ToLower() == "admin payzen") ?? false;

                if (!isAdminPayzen)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, 
                        new { Message = "Seul l'admin Payzen peut modifier les jours f�ri�s de scope Global" });
                }
            }

            var hasChanges = false;

            // Mise � jour des noms multilingues
            if (!string.IsNullOrWhiteSpace(dto.NameFr) && dto.NameFr.Trim() != holiday.NameFr)
            {
                holiday.NameFr = dto.NameFr.Trim();
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.NameAr) && dto.NameAr.Trim() != holiday.NameAr)
            {
                holiday.NameAr = dto.NameAr.Trim();
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.NameEn) && dto.NameEn.Trim() != holiday.NameEn)
            {
                holiday.NameEn = dto.NameEn.Trim();
                hasChanges = true;
            }

            // Mise � jour de la date
            if (dto.HolidayDate.HasValue && dto.HolidayDate.Value != holiday.HolidayDate)
            {
                // V�rifier qu'il n'existe pas d�j� un jour f�ri� pour cette nouvelle date
                var query = _db.Holidays
                    .Where(h => h.HolidayDate == dto.HolidayDate.Value &&
                               h.CountryId == holiday.CountryId &&
                               h.Id != id &&
                               h.DeletedAt == null);

                if (holiday.CompanyId.HasValue)
                    query = query.Where(h => h.CompanyId == holiday.CompanyId);

                if (await query.AnyAsync())
                    return Conflict(new { Message = "Un jour f�ri� existe d�j� pour cette date" });

                holiday.HolidayDate = dto.HolidayDate.Value;
                hasChanges = true;
            }

            // Mise � jour de la description
            if (dto.Description != null && dto.Description.Trim() != holiday.Description)
            {
                holiday.Description = dto.Description.Trim();
                hasChanges = true;
            }

            // Mise � jour du pays
            if (dto.CountryId.HasValue && dto.CountryId != holiday.CountryId)
            {
                var countryExists = await _db.Countries
                    .AnyAsync(c => c.Id == dto.CountryId && c.DeletedAt == null);

                if (!countryExists)
                    return NotFound(new { Message = "Pays non trouv�" });

                holiday.CountryId = dto.CountryId.Value;
                hasChanges = true;
            }

            // Mise � jour du scope
            if (dto.Scope.HasValue && dto.Scope != holiday.Scope)
            {
                holiday.Scope = dto.Scope.Value;
                hasChanges = true;
            }

            // Mise � jour du type
            if (!string.IsNullOrWhiteSpace(dto.HolidayType) && dto.HolidayType.Trim() != holiday.HolidayType)
            {
                holiday.HolidayType = dto.HolidayType.Trim();
                hasChanges = true;
            }

            // Mise � jour des flags bool�ens
            if (dto.IsMandatory.HasValue && dto.IsMandatory != holiday.IsMandatory)
            {
                holiday.IsMandatory = dto.IsMandatory.Value;
                hasChanges = true;
            }

            if (dto.IsPaid.HasValue && dto.IsPaid != holiday.IsPaid)
            {
                holiday.IsPaid = dto.IsPaid.Value;
                hasChanges = true;
            }

            if (dto.IsRecurring.HasValue && dto.IsRecurring != holiday.IsRecurring)
            {
                holiday.IsRecurring = dto.IsRecurring.Value;
                hasChanges = true;
            }

            if (dto.RecurrenceRule != null && dto.RecurrenceRule.Trim() != holiday.RecurrenceRule)
            {
                holiday.RecurrenceRule = dto.RecurrenceRule.Trim();
                hasChanges = true;
            }

            if (dto.Year.HasValue && dto.Year != holiday.Year)
            {
                holiday.Year = dto.Year.Value;
                hasChanges = true;
            }

            if (dto.AffectPayroll.HasValue && dto.AffectPayroll != holiday.AffectPayroll)
            {
                holiday.AffectPayroll = dto.AffectPayroll.Value;
                hasChanges = true;
            }

            if (dto.AffectAttendance.HasValue && dto.AffectAttendance != holiday.AffectAttendance)
            {
                holiday.AffectAttendance = dto.AffectAttendance.Value;
                hasChanges = true;
            }

            if (dto.IsActive.HasValue && dto.IsActive != holiday.IsActive)
            {
                holiday.IsActive = dto.IsActive.Value;
                hasChanges = true;
            }

            if (hasChanges)
            {
                holiday.ModifiedAt = DateTimeOffset.UtcNow;
                holiday.ModifiedBy = userId;
                await _db.SaveChangesAsync();
            }

            // R�cup�rer et retourner directement le DTO mis � jour
            var updatedHoliday = await _db.Holidays
                .AsNoTracking()
                .Where(h => h.Id == id && h.DeletedAt == null)
                .Include(h => h.Company)
                .Include(h => h.Country)
                .Select(h => new HolidayReadDto
                {
                    Id = h.Id,
                    NameFr = h.NameFr,
                    NameAr = h.NameAr,
                    NameEn = h.NameEn,
                    HolidayDate = h.HolidayDate,
                    Description = h.Description,
                    CompanyId = h.CompanyId,
                    CompanyName = h.Company != null ? h.Company.CompanyName : null,
                    CountryId = h.CountryId,
                    CountryName = h.Country!.CountryName,
                    Scope = h.Scope,
                    ScopeDescription = h.Scope == HolidayScope.Global ? "Global" : "Entreprise",
                    HolidayType = h.HolidayType,
                    IsMandatory = h.IsMandatory,
                    IsPaid = h.IsPaid,
                    IsRecurring = h.IsRecurring,
                    RecurrenceRule = h.RecurrenceRule,
                    Year = h.Year,
                    AffectPayroll = h.AffectPayroll,
                    AffectAttendance = h.AffectAttendance,
                    IsActive = h.IsActive,
                    CreatedAt = h.CreatedAt.DateTime
                })
                .FirstOrDefaultAsync();

            if (updatedHoliday == null)
                return NotFound(new { Message = "Erreur lors de la r�cup�ration du jour f�ri� mis � jour" });

            return Ok(updatedHoliday);
        }

        /// <summary>
        /// Supprime un jour f�ri� (soft delete)
        /// DELETE /api/holidays/5
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHoliday(int id)
        {
            var userId = User.GetUserId();

            var holiday = await _db.Holidays
                .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

            if (holiday == null)
                return NotFound(new { Message = "Jour f�ri� non trouv�" });

            // V�rifier si le jour f�ri� est de scope Global
            if (holiday.Scope == HolidayScope.Global)
            {
                // R�cup�rer l'utilisateur et ses r�les
                var user = await _db.Users
                    .Where(u => u.Id == userId)
                    .Include(u => u.UsersRoles!.Where(ur => ur.DeletedAt == null))
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync();

                // V�rifier si l'utilisateur a le r�le "Admin Payzen"
                var isAdminPayzen = user?.UsersRoles?
                    .Any(ur => ur.Role != null &&
                              ur.Role.DeletedAt == null &&
                              ur.Role.Name.ToLower() == "admin payzen") ?? false;

                if (!isAdminPayzen)
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { Message = "Seul l'admin Payzen peut modifier les jours f�ri�s de scope Global" });
                }
            }

            // Soft delete
            holiday.DeletedAt = DateTimeOffset.UtcNow;
            holiday.DeletedBy = userId;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// V�rifie si une date est un jour f�ri�
        /// GET /api/holidays/check?countryId=1&date=2025-05-01&companyId=5
        /// </summary>
        [HttpGet("check")]
        [Produces("application/json")]
        public async Task<ActionResult<object>> CheckHoliday(
            [FromQuery] int countryId,
            [FromQuery] DateOnly date,
            [FromQuery] int? companyId = null)
        {
            var countryExists = await _db.Countries
                .AnyAsync(c => c.Id == countryId && c.DeletedAt == null);

            if (!countryExists)
                return NotFound(new { Message = "Pays non trouv�" });

            var query = _db.Holidays
                .AsNoTracking()
                .Where(h => h.CountryId == countryId &&
                           h.HolidayDate == date &&
                           h.IsActive &&
                           h.DeletedAt == null);

            // Inclure les jours f�ri�s globaux et ceux de la soci�t�
            if (companyId.HasValue)
                query = query.Where(h => h.CompanyId == null || h.CompanyId == companyId.Value);
            else
                query = query.Where(h => h.CompanyId == null);

            var holiday = await query
                .Select(h => new
                {
                    h.Id,
                    h.NameFr,
                    h.NameAr,
                    h.NameEn,
                    h.HolidayDate,
                    h.Scope,
                    h.HolidayType,
                    h.IsMandatory,
                    h.IsPaid
                })
                .FirstOrDefaultAsync();

            if (holiday == null)
            {
                return Ok(new
                {
                    IsHoliday = false,
                    Date = date,
                    Message = "Cette date n'est pas un jour f�ri�"
                });
            }

            return Ok(new
            {
                IsHoliday = true,
                Holiday = holiday
            });
        }

        /// <summary>
        /// R�cup�re les types de jours f�ri�s distincts
        /// GET /api/holidays/types?countryId=1
        /// </summary>
        [HttpGet("types")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<string>>> GetHolidayTypes([FromQuery] int? countryId = null)
        {
            var query = _db.Holidays
                .AsNoTracking()
                .Where(h => h.DeletedAt == null);

            if (countryId.HasValue)
                query = query.Where(h => h.CountryId == countryId.Value);

            var types = await query
                .Select(h => h.HolidayType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            return Ok(types);
        }

        /// <summary>
        /// Calcule la date d'un jour f�ri� r�current pour une ann�e donn�e
        /// </summary>
        private DateOnly CalculateRecurringDate(DateOnly originalDate, int targetYear, string recurrenceRule)
        {
            // R�gle simple : "annual" ou "yearly" = m�me jour/mois chaque ann�e
            if (recurrenceRule.ToLower().Contains("annual") || recurrenceRule.ToLower().Contains("yearly"))
            {
                try
                {
                    return new DateOnly(targetYear, originalDate.Month, originalDate.Day);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Gestion du cas 29 f�vrier pour les ann�es non bissextiles
                    if (originalDate.Month == 2 && originalDate.Day == 29)
                    {
                        return new DateOnly(targetYear, 2, 28);
                    }
                    throw;
                }
            }

            // Pour d'autres r�gles de r�currence (mensuel, etc.), � impl�menter selon vos besoins
            // Exemple : "monthly" pourrait r�p�ter chaque mois
            if (recurrenceRule.ToLower().Contains("monthly"))
            {
                // Cr�er pour chaque mois de l'ann�e cible
                return new DateOnly(targetYear, originalDate.Month, originalDate.Day);
            }

            // Par d�faut, comportement annuel
            return new DateOnly(targetYear, originalDate.Month, originalDate.Day);
        }
    }
}