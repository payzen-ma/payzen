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
    [Route("api/employee-documents")]
    [ApiController]
    [Authorize]
    public class EmployeeDocumentsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EmployeeDocumentsController(AppDbContext db) => _db = db;

        /// <summary>
        /// R�cup�re tous les documents
        /// </summary>
        [HttpGet]
        //[HasPermission("READ_EMPLOYEE_DOCUMENTS")]
        public async Task<ActionResult<IEnumerable<EmployeeDocumentReadDto>>> GetAll()
        {
            var documents = await _db.EmployeeDocuments
                .AsNoTracking()
                .Where(ed => ed.DeletedAt == null)
                .Include(ed => ed.Employee)
                .OrderBy(ed => ed.Employee.LastName)
                .ThenBy(ed => ed.Employee.FirstName)
                .ToListAsync();

            var result = documents.Select(ed => new EmployeeDocumentReadDto
            {
                Id = ed.Id,
                EmployeeId = ed.EmployeeId,
                EmployeeFullName = $"{ed.Employee?.FirstName} {ed.Employee?.LastName}",
                Name = ed.Name,
                FilePath = ed.FilePath,
                ExpirationDate = ed.ExpirationDate,
                DocumentType = ed.DocumentType,
                CreatedAt = ed.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re un document par ID
        /// </summary>
        [HttpGet("{id}")]
        //[HasPermission("VIEW_EMPLOYEE_DOCUMENT")]
        public async Task<ActionResult<EmployeeDocumentReadDto>> GetById(int id)
        {
            var document = await _db.EmployeeDocuments
                .AsNoTracking()
                .Where(ed => ed.DeletedAt == null)
                .Include(ed => ed.Employee)
                .FirstOrDefaultAsync(ed => ed.Id == id);

            if (document == null)
                return NotFound(new { Message = "Document non trouv�" });

            var result = new EmployeeDocumentReadDto
            {
                Id = document.Id,
                EmployeeId = document.EmployeeId,
                EmployeeFullName = $"{document.Employee?.FirstName} {document.Employee?.LastName}",
                Name = document.Name,
                FilePath = document.FilePath,
                ExpirationDate = document.ExpirationDate,
                DocumentType = document.DocumentType,
                CreatedAt = document.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re tous les documents d'un employ�
        /// </summary>
        [HttpGet("employee/{employeeId}")]
        //[HasPermission("VIEW_EMPLOYEE_DOCUMENT")]
        public async Task<ActionResult<IEnumerable<EmployeeDocumentReadDto>>> GetByEmployeeId(int employeeId)
        {
            var employeeExists = await _db.Employees.AnyAsync(e => e.Id == employeeId && e.DeletedAt == null);
            if (!employeeExists)
                return NotFound(new { Message = "Employ� non trouv�" });

            var documents = await _db.EmployeeDocuments
                .AsNoTracking()
                .Where(ed => ed.EmployeeId == employeeId && ed.DeletedAt == null)
                .Include(ed => ed.Employee)
                .OrderByDescending(ed => ed.CreatedAt)
                .ToListAsync();

            var result = documents.Select(ed => new EmployeeDocumentReadDto
            {
                Id = ed.Id,
                EmployeeId = ed.EmployeeId,
                EmployeeFullName = $"{ed.Employee?.FirstName} {ed.Employee?.LastName}",
                Name = ed.Name,
                FilePath = ed.FilePath,
                ExpirationDate = ed.ExpirationDate,
                DocumentType = ed.DocumentType,
                CreatedAt = ed.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// Cr�e un nouveau document
        /// </summary>
        [HttpPost]
        //[HasPermission("CREATE_EMPLOYEE_DOCUMENT")]
        public async Task<ActionResult<EmployeeDocumentReadDto>> Create([FromBody] EmployeeDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new 
                { 
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var employeeExists = await _db.Employees.AnyAsync(e => e.Id == dto.EmployeeId && e.DeletedAt == null);
            if (!employeeExists)
                return NotFound(new { Message = "Employ� non trouv�" });

            var document = new EmployeeDocument
            {
                EmployeeId = dto.EmployeeId,
                Name = dto.Name,
                FilePath = dto.FilePath,
                ExpirationDate = dto.ExpirationDate,
                DocumentType = dto.DocumentType,
                CreatedBy = User.GetUserId(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.EmployeeDocuments.Add(document);
            await _db.SaveChangesAsync();

            var createdDocument = await _db.EmployeeDocuments
                .AsNoTracking()
                .Include(ed => ed.Employee)
                .FirstAsync(ed => ed.Id == document.Id);

            var result = new EmployeeDocumentReadDto
            {
                Id = createdDocument.Id,
                EmployeeId = createdDocument.EmployeeId,
                EmployeeFullName = $"{createdDocument.Employee?.FirstName} {createdDocument.Employee?.LastName}",
                Name = createdDocument.Name,
                FilePath = createdDocument.FilePath,
                ExpirationDate = createdDocument.ExpirationDate,
                DocumentType = createdDocument.DocumentType,
                CreatedAt = createdDocument.CreatedAt.DateTime
            };

            return CreatedAtAction(nameof(GetById), new { id = document.Id }, result);
        }

        /// <summary>
        /// Met � jour un document
        /// </summary>
        [HttpPut("{id}")]
        //[HasPermission("UPDATE_EMPLOYEE_DOCUMENT")]
        public async Task<ActionResult<EmployeeDocumentReadDto>> Update(int id, [FromBody] EmployeeDocumentUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new 
                { 
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var document = await _db.EmployeeDocuments.FirstOrDefaultAsync(ed => ed.Id == id && ed.DeletedAt == null);
            if (document == null)
                return NotFound(new { Message = "Document non trouv�" });

            if (dto.Name != null)
                document.Name = dto.Name;

            if (dto.FilePath != null)
                document.FilePath = dto.FilePath;

            if (dto.ExpirationDate.HasValue)
                document.ExpirationDate = dto.ExpirationDate;

            if (dto.DocumentType != null)
                document.DocumentType = dto.DocumentType;

            document.ModifiedAt = DateTimeOffset.UtcNow;
            document.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            var updatedDocument = await _db.EmployeeDocuments
                .AsNoTracking()
                .Include(ed => ed.Employee)
                .FirstAsync(ed => ed.Id == id);

            var result = new EmployeeDocumentReadDto
            {
                Id = updatedDocument.Id,
                EmployeeId = updatedDocument.EmployeeId,
                EmployeeFullName = $"{updatedDocument.Employee?.FirstName} {updatedDocument.Employee?.LastName}",
                Name = updatedDocument.Name,
                FilePath = updatedDocument.FilePath,
                ExpirationDate = updatedDocument.ExpirationDate,
                DocumentType = updatedDocument.DocumentType,
                CreatedAt = updatedDocument.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Supprime un document (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        //[HasPermission("DELETE_EMPLOYEE_DOCUMENT")]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _db.EmployeeDocuments.FirstOrDefaultAsync(ed => ed.Id == id && ed.DeletedAt == null);
            if (document == null)
                return NotFound(new { Message = "Document non trouv�" });

            document.DeletedAt = DateTimeOffset.UtcNow;
            document.DeletedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
