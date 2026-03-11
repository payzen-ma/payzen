using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Employee.Dtos;
using payzen_backend.Authorization;
using payzen_backend.Extensions;

namespace payzen_backend.Controllers.Employees
{
    [Route("api/employee-addresses")]
    [ApiController]
    [Authorize]
    public class EmployeeAddresssController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EmployeeAddresssController(AppDbContext db) => _db = db;

        /// <summary>
        /// R�cup�re toutes les adresses d'employ�s
        /// </summary>
        [HttpGet]
        //[HasPermission("READ_EMPLOYEE_ADDRESSES")]
        public async Task<ActionResult<IEnumerable<EmployeeAddressReadDto>>> GetAll()
        {
            var addresses = await _db.EmployeeAddresses
                .AsNoTracking()
                .Where(ea => ea.DeletedAt == null)
                .Include(ea => ea.Employee)
                .Include(ea => ea.City)
                    .ThenInclude(c => c.Country)
                .OrderBy(ea => ea.Employee.LastName)
                .ThenBy(ea => ea.Employee.FirstName)
                .ToListAsync();

            var result = addresses.Select(ea => new EmployeeAddressReadDto
            {
                Id = ea.Id,
                EmployeeId = ea.EmployeeId,
                EmployeeFullName = $"{ea.Employee?.FirstName} {ea.Employee?.LastName}",
                AddressLine1 = ea.AddressLine1,
                AddressLine2 = ea.AddressLine2,
                ZipCode = ea.ZipCode,
                CityId = ea.CityId,
                CityName = ea.City?.CityName ?? "",
                CreatedAt = ea.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re une adresse par ID
        /// </summary>
        [HttpGet("{id}")]
        //[HasPermission("VIEW_EMPLOYEE_ADDRESS")]
        public async Task<ActionResult<EmployeeAddressReadDto>> GetById(int id)
        {
            var address = await _db.EmployeeAddresses
                .AsNoTracking()
                .Where(ea => ea.DeletedAt == null)
                .Include(ea => ea.Employee)
                .Include(ea => ea.City)
                    .ThenInclude(c => c.Country)
                .FirstOrDefaultAsync(ea => ea.Id == id);

            if (address == null)
                return NotFound(new { Message = "Adresse non trouv�e" });

            var result = new EmployeeAddressReadDto
            {
                Id = address.Id,
                EmployeeId = address.EmployeeId,
                EmployeeFullName = $"{address.Employee?.FirstName} {address.Employee?.LastName}",
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                ZipCode = address.ZipCode,
                CityId = address.CityId,
                CityName = address.City?.CityName ?? "",
                CreatedAt = address.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re toutes les adresses d'un employ�
        /// </summary>
        [HttpGet("employee/{employeeId}")]
        //[HasPermission("VIEW_EMPLOYEE_ADDRESS")]
        public async Task<ActionResult<IEnumerable<EmployeeAddressReadDto>>> GetByEmployeeId(int employeeId)
        {
            var employeeExists = await _db.Employees.AnyAsync(e => e.Id == employeeId && e.DeletedAt == null);
            if (!employeeExists)
                return NotFound(new { Message = "Employ� non trouv�" });

            var addresses = await _db.EmployeeAddresses
                .AsNoTracking()
                .Where(ea => ea.EmployeeId == employeeId && ea.DeletedAt == null)
                .Include(ea => ea.Employee)
                .Include(ea => ea.City)
                    .ThenInclude(c => c.Country)
                .OrderByDescending(ea => ea.CreatedAt)
                .ToListAsync();

            var result = addresses.Select(ea => new EmployeeAddressReadDto
            {
                Id = ea.Id,
                EmployeeId = ea.EmployeeId,
                EmployeeFullName = $"{ea.Employee?.FirstName} {ea.Employee?.LastName}",
                AddressLine1 = ea.AddressLine1,
                AddressLine2 = ea.AddressLine2,
                ZipCode = ea.ZipCode,
                CityId = ea.CityId,
                CityName = ea.City?.CityName ?? "",
                CreatedAt = ea.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// Cr�e une nouvelle adresse pour un employ�
        /// </summary>
        [HttpPost]
        //[HasPermission("CREATE_EMPLOYEE_ADDRESS")]
        public async Task<ActionResult<EmployeeAddressReadDto>> Create([FromBody] EmployeeAddressCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new 
                { 
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            // V�rifier que l'employ� existe
            var employeeExists = await _db.Employees.AnyAsync(e => e.Id == dto.EmployeeId && e.DeletedAt == null);
            if (!employeeExists)
                return NotFound(new { Message = "Employ� non trouv�" });

            // V�rifier que la ville existe
            var cityExists = await _db.Cities.AnyAsync(c => c.Id == dto.CityId && c.DeletedAt == null);
            if (!cityExists)
                return NotFound(new { Message = "Ville non trouv�e" });

            // V�rifier que le pays existe
            var countryExists = await _db.Countries.AnyAsync(c => c.Id == dto.CountryId && c.DeletedAt == null);
            if (!countryExists)
                return NotFound(new { Message = "Pays non trouv�" });

            // V�rifier que la ville appartient bien au pays sp�cifi�
            var city = await _db.Cities.FindAsync(dto.CityId);
            if (city?.CountryId != dto.CountryId)
                return BadRequest(new { Message = "La ville ne correspond pas au pays sp�cifi�" });

            var address = new EmployeeAddress
            {
                EmployeeId = dto.EmployeeId,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                ZipCode = dto.ZipCode,
                CityId = dto.CityId,
                CreatedBy = User.GetUserId(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.EmployeeAddresses.Add(address);
            await _db.SaveChangesAsync();

            // R�cup�rer l'adresse cr��e avec ses relations
            var createdAddress = await _db.EmployeeAddresses
                .AsNoTracking()
                .Include(ea => ea.Employee)
                .Include(ea => ea.City)
                .FirstAsync(ea => ea.Id == address.Id);

            var result = new EmployeeAddressReadDto
            {
                Id = createdAddress.Id,
                EmployeeId = createdAddress.EmployeeId,
                EmployeeFullName = $"{createdAddress.Employee?.FirstName} {createdAddress.Employee?.LastName}",
                AddressLine1 = createdAddress.AddressLine1,
                AddressLine2 = createdAddress.AddressLine2,
                ZipCode = createdAddress.ZipCode,
                CityId = createdAddress.CityId,
                CityName = createdAddress.City?.CityName ?? "",
                CreatedAt = createdAddress.CreatedAt.DateTime
            };

            return CreatedAtAction(nameof(GetById), new { id = address.Id }, result);
        }

        /// <summary>
        /// Met � jour une adresse
        /// </summary>
        [HttpPut("{id}")]
        //[HasPermission("UPDATE_EMPLOYEE_ADDRESS")]
        public async Task<ActionResult<EmployeeAddressReadDto>> Update(int id, [FromBody] EmployeeAddressUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new 
                { 
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var address = await _db.EmployeeAddresses.FirstOrDefaultAsync(ea => ea.Id == id && ea.DeletedAt == null);
            if (address == null)
                return NotFound(new { Message = "Adresse non trouv�e" });

            if (dto.AddressLine1 != null)
                address.AddressLine1 = dto.AddressLine1;

            if (dto.AddressLine2 != null)
                address.AddressLine2 = dto.AddressLine2;

            if (dto.ZipCode != null)
                address.ZipCode = dto.ZipCode;

            if (dto.CityId.HasValue)
            {
                var cityExists = await _db.Cities.AnyAsync(c => c.Id == dto.CityId.Value && c.DeletedAt == null);
                if (!cityExists)
                    return NotFound(new { Message = "Ville non trouv�e" });
                
                address.CityId = dto.CityId.Value;
            }

            if (dto.CountryId.HasValue)
            {
                var countryExists = await _db.Countries.AnyAsync(c => c.Id == dto.CountryId.Value && c.DeletedAt == null);
                if (!countryExists)
                    return NotFound(new { Message = "Pays non trouv�" });
                
                // V�rifier que la ville appartient au nouveau pays
                var currentCityId = dto.CityId ?? address.CityId;
                var city = await _db.Cities.FindAsync(currentCityId);
                if (city?.CountryId != dto.CountryId.Value)
                    return BadRequest(new { Message = "La ville ne correspond pas au pays sp�cifi�" });
            }

            address.ModifiedAt = DateTimeOffset.UtcNow;
            address.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            // R�cup�rer l'adresse mise � jour avec ses relations
            var updatedAddress = await _db.EmployeeAddresses
                .AsNoTracking()
                .Include(ea => ea.Employee)
                .Include(ea => ea.City)
                .FirstAsync(ea => ea.Id == id);

            var result = new EmployeeAddressReadDto
            {
                Id = updatedAddress.Id,
                EmployeeId = updatedAddress.EmployeeId,
                EmployeeFullName = $"{updatedAddress.Employee?.FirstName} {updatedAddress.Employee?.LastName}",
                AddressLine1 = updatedAddress.AddressLine1,
                AddressLine2 = updatedAddress.AddressLine2,
                ZipCode = updatedAddress.ZipCode,
                CityId = updatedAddress.CityId,
                CityName = updatedAddress.City?.CityName ?? "",
                CreatedAt = updatedAddress.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Supprime une adresse (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        //[HasPermission("DELETE_EMPLOYEE_ADDRESS")]
        public async Task<IActionResult> Delete(int id)
        {
            var address = await _db.EmployeeAddresses.FirstOrDefaultAsync(ea => ea.Id == id && ea.DeletedAt == null);
            if (address == null)
                return NotFound(new { Message = "Adresse non trouv�e" });

            address.DeletedAt = DateTimeOffset.UtcNow;
            address.DeletedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
