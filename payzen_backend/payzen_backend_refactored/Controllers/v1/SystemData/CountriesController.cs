using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Referentiel;
using payzen_backend.Models.Referentiel.Dtos;
using payzen_backend.Authorization;
using payzen_backend.Extensions;

namespace payzen_backend.Controllers.v1.SystemData
{
    [Route("api/v{version:apiVersion}/countries")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class CountriesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CountriesController(AppDbContext db) => _db = db;

        /// <summary>
        /// R�cup�re tous les pays actifs
        /// </summary>
        [HttpGet]
        //[HasPermission("READ_COUNTRIES")]
        public async Task<ActionResult<IEnumerable<CountryReadDto>>> GetAll()
        {
            var countries = await _db.Countries
                .AsNoTracking()
                .Where(c => c.DeletedAt == null)
                .OrderBy(c => c.CountryName)
                .ToListAsync();

            var result = countries.Select(c => new CountryReadDto
            {
                Id = c.Id,
                CountryName = c.CountryName,
                CountryNameAr = c.CountryNameAr,
                CountryCode = c.CountryCode,
                CountryPhoneCode = c.CountryPhoneCode,
                //Nationality = c.Nationality,
                CreatedAt = c.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re un pays par ID
        /// </summary>
        [HttpGet("{id}")]
        //[HasPermission("VIEW_COUNTRY")]
        public async Task<ActionResult<CountryReadDto>> GetById(int id)
        {
            var country = await _db.Countries
                .AsNoTracking()
                .Where(c => c.DeletedAt == null)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (country == null)
                return NotFound(new { Message = "Pays non trouv�" });

            var result = new CountryReadDto
            {
                Id = country.Id,
                CountryName = country.CountryName,
                CountryNameAr = country.CountryNameAr,
                CountryCode = country.CountryCode,
                CountryPhoneCode = country.CountryPhoneCode,
                //Nationality = country.Nationality,
                CreatedAt = country.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Cr�e un nouveau pays
        /// </summary>
        [HttpPost]
        //[HasPermission("CREATE_COUNTRY")]
        public async Task<ActionResult<CountryReadDto>> Create([FromBody] CountryCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new 
                { 
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            // V�rifier que le code pays n'existe pas d�j�
            if (await _db.Countries.AnyAsync(c => c.CountryCode == dto.CountryCode && c.DeletedAt == null))
                return Conflict(new { Message = "Un pays avec ce code existe d�j�" });

            var country = new Country
            {
                CountryName = dto.CountryName,
                CountryNameAr = dto.CountryNameAr,
                CountryCode = dto.CountryCode.ToUpper(),
                CountryPhoneCode = dto.CountryPhoneCode,
                //Nationality = dto.Nationality,
                CreatedBy = User.GetUserId(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Countries.Add(country);
            await _db.SaveChangesAsync();

            var result = new CountryReadDto
            {
                Id = country.Id,
                CountryName = country.CountryName,
                CountryNameAr = country.CountryNameAr,
                CountryCode = country.CountryCode,
                CountryPhoneCode = country.CountryPhoneCode,
                //Nationality = country.Nationality,
                CreatedAt = country.CreatedAt.DateTime
            };

            return CreatedAtAction(nameof(GetById), new { id = country.Id }, result);
        }

        /// <summary>
        /// Met � jour un pays
        /// </summary>
        [HttpPut("{id}")]
        //[HasPermission("UPDATE_COUNTRY")]
        public async Task<ActionResult<CountryReadDto>> Update(int id, [FromBody] CountryUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new 
                { 
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var country = await _db.Countries.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);
            if (country == null)
                return NotFound(new { Message = "Pays non trouv�" });

            if (dto.CountryName != null)
                country.CountryName = dto.CountryName;

            if (dto.CountryNameAr != null)
                country.CountryNameAr = dto.CountryNameAr;

            if (dto.CountryCode != null && dto.CountryCode != country.CountryCode)
            {
                if (await _db.Countries.AnyAsync(c => c.CountryCode == dto.CountryCode && c.Id != id && c.DeletedAt == null))
                    return Conflict(new { Message = "Un pays avec ce code existe d�j�" });
                
                country.CountryCode = dto.CountryCode.ToUpper();
            }

            if (dto.CountryPhoneCode != null)
                country.CountryPhoneCode = dto.CountryPhoneCode;

            //if (dto.Nationality != null)
            //    country.Nationality = dto.Nationality;

            country.ModifiedAt = DateTimeOffset.UtcNow;
            country.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            var result = new CountryReadDto
            {
                Id = country.Id,
                CountryName = country.CountryName,
                CountryNameAr = country.CountryNameAr,
                CountryCode = country.CountryCode,
                CountryPhoneCode = country.CountryPhoneCode,
                //Nationality = country.Nationality,
                CreatedAt = country.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Supprime un pays (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        //[HasPermission("DELETE_COUNTRY")]
        public async Task<IActionResult> Delete(int id)
        {
            var country = await _db.Countries.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);
            if (country == null)
                return NotFound(new { Message = "Pays non trouv�" });

            // V�rifier si le pays est utilis�
            var hasCities = await _db.Cities.AnyAsync(c => c.CountryId == id && c.DeletedAt == null);
            if (hasCities)
                return BadRequest(new { Message = "Impossible de supprimer ce pays car il contient des villes" });

            var hasCompanies = await _db.Companies.AnyAsync(c => c.CountryId == id && c.DeletedAt == null);
            if (hasCompanies)
                return BadRequest(new { Message = "Impossible de supprimer ce pays car il est utilis� par des soci�t�s" });

            country.DeletedAt = DateTimeOffset.UtcNow;
            country.DeletedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}