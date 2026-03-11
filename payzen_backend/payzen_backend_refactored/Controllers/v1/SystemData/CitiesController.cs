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
    [Route("api/v{version:apiVersion}/cities")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class CitiesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CitiesController(AppDbContext db) => _db = db;

        /// <summary>
        /// R�cup�re toutes les villes actives
        /// </summary>
        [HttpGet]
        //[HasPermission("READ_CITIES")]
        public async Task<ActionResult<IEnumerable<CityReadDto>>> GetAll()
        {
            var cities = await _db.Cities
                .AsNoTracking()
                .Where(c => c.DeletedAt == null)
                .Include(c => c.Country)
                .OrderBy(c => c.CityName)
                .ToListAsync();

            var result = cities.Select(c => new CityReadDto
            {
                Id = c.Id,
                CityName = c.CityName,
                CountryId = c.CountryId,
                CountryName = c.Country?.CountryName ?? "",
                CreatedAt = c.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re une ville par ID
        /// </summary>
        [HttpGet("{id}")]
        //[HasPermission("VIEW_CITY")]
        public async Task<ActionResult<CityReadDto>> GetById(int id)
        {
            var city = await _db.Cities
                .AsNoTracking()
                .Where(c => c.DeletedAt == null)
                .Include(c => c.Country)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (city == null)
                return NotFound(new { Message = "Ville non trouv�e" });

            var result = new CityReadDto
            {
                Id = city.Id,
                CityName = city.CityName,
                CountryId = city.CountryId,
                CountryName = city.Country?.CountryName ?? "",
                CreatedAt = city.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re toutes les villes d'un pays
        /// </summary>
        [HttpGet("country/{countryId}")]
        //[HasPermission("READ_CITIES")]
        public async Task<ActionResult<IEnumerable<CityReadDto>>> GetByCountryId(int countryId)
        {
            var countryExists = await _db.Countries.AnyAsync(c => c.Id == countryId && c.DeletedAt == null);
            if (!countryExists)
                return NotFound(new { Message = "Pays non trouv�" });

            var cities = await _db.Cities
                .AsNoTracking()
                .Where(c => c.CountryId == countryId && c.DeletedAt == null)
                .Include(c => c.Country)
                .OrderBy(c => c.CityName)
                .ToListAsync();

            var result = cities.Select(c => new CityReadDto
            {
                Id = c.Id,
                CityName = c.CityName,
                CountryId = c.CountryId,
                CountryName = c.Country?.CountryName ?? "",
                CreatedAt = c.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// Cr�e une nouvelle ville
        /// </summary>
        [HttpPost]
        //[HasPermission("CREATE_CITY")]
        public async Task<ActionResult<CityReadDto>> Create([FromBody] CityCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new 
                { 
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            // V�rifier que le pays existe
            var countryExists = await _db.Countries.AnyAsync(c => c.Id == dto.CountryId && c.DeletedAt == null);
            if (!countryExists)
                return NotFound(new { Message = "Pays non trouv�" });

            // V�rifier qu'une ville avec ce nom n'existe pas d�j� dans ce pays
            if (await _db.Cities.AnyAsync(c => c.CountryId == dto.CountryId && c.CityName == dto.CityName && c.DeletedAt == null))
                return Conflict(new { Message = "Une ville avec ce nom existe d�j� dans ce pays" });

            var city = new City
            {
                CityName = dto.CityName,
                CountryId = dto.CountryId,
                CreatedBy = User.GetUserId(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Cities.Add(city);
            await _db.SaveChangesAsync();

            var createdCity = await _db.Cities
                .AsNoTracking()
                .Include(c => c.Country)
                .FirstAsync(c => c.Id == city.Id);

            var result = new CityReadDto
            {
                Id = createdCity.Id,
                CityName = createdCity.CityName,
                CountryId = createdCity.CountryId,
                CountryName = createdCity.Country?.CountryName ?? "",
                CreatedAt = createdCity.CreatedAt.DateTime
            };

            return CreatedAtAction(nameof(GetById), new { id = city.Id }, result);
        }

        /// <summary>
        /// Met � jour une ville
        /// </summary>
        [HttpPut("{id}")]
        //[HasPermission("UPDATE_CITY")]
        public async Task<ActionResult<CityReadDto>> Update(int id, [FromBody] CityUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new 
                { 
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var city = await _db.Cities.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);
            if (city == null)
                return NotFound(new { Message = "Ville non trouv�e" });

            if (dto.CityName != null)
            {
                var currentCountryId = dto.CountryId ?? city.CountryId;
                if (await _db.Cities.AnyAsync(c => c.CountryId == currentCountryId && c.CityName == dto.CityName && c.Id != id && c.DeletedAt == null))
                    return Conflict(new { Message = "Une ville avec ce nom existe d�j� dans ce pays" });
                
                city.CityName = dto.CityName;
            }

            if (dto.CountryId.HasValue && dto.CountryId.Value != city.CountryId)
            {
                var countryExists = await _db.Countries.AnyAsync(c => c.Id == dto.CountryId.Value && c.DeletedAt == null);
                if (!countryExists)
                    return NotFound(new { Message = "Pays non trouv�" });
                
                city.CountryId = dto.CountryId.Value;
            }

            city.ModifiedAt = DateTimeOffset.UtcNow;
            city.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            var updatedCity = await _db.Cities
                .AsNoTracking()
                .Include(c => c.Country)
                .FirstAsync(c => c.Id == id);

            var result = new CityReadDto
            {
                Id = updatedCity.Id,
                CityName = updatedCity.CityName,
                CountryId = updatedCity.CountryId,
                CountryName = updatedCity.Country?.CountryName ?? "",
                CreatedAt = updatedCity.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Supprime une ville (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        //[HasPermission("DELETE_CITY")]
        public async Task<IActionResult> Delete(int id)
        {
            var city = await _db.Cities.FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null);
            if (city == null)
                return NotFound(new { Message = "Ville non trouv�e" });

            // V�rifier si la ville est utilis�e par des soci�t�s
            var hasCompanies = await _db.Companies.AnyAsync(c => c.CityId == id && c.DeletedAt == null);
            if (hasCompanies)
                return BadRequest(new { Message = "Impossible de supprimer cette ville car elle est utilis�e par des soci�t�s" });

            city.DeletedAt = DateTimeOffset.UtcNow;
            city.DeletedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}