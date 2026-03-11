using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Company;
using payzen_backend.Models.Company.Dtos;
using payzen_backend.Services.Company;

namespace payzen_backend.Controllers.v1.Company
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CompanyDocumentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ICompanyDocumentService _documentService;
        private readonly ILogger<CompanyDocumentsController> _logger;

        public CompanyDocumentsController(
            AppDbContext db,
            ICompanyDocumentService documentService,
            ILogger<CompanyDocumentsController> logger)
        {
            _db = db;
            _documentService = documentService;
            _logger = logger;
        }

        /// <summary>
        /// R�cup�re tous les documents (non supprim�s)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CompanyDocumentReadDto>>> GetAll()
        {
            var documents = await _db.CompanyDocuments
                .AsNoTracking()
                .Where(cd => cd.DeletedAt == null)
                .Include(cd => cd.Company)
                .OrderByDescending(cd => cd.CreatedAt)
                .ToListAsync();

            var result = documents.Select(cd => new CompanyDocumentReadDto
            {
                Id = cd.Id,
                CompanyId = cd.CompanyId,
                CompanyName = cd.Company?.CompanyName ?? "N/A",
                Name = cd.Name,
                FilePath = cd.FilePath,
                DocumentType = cd.DocumentType,
                CreatedAt = cd.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re un document par son ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<CompanyDocumentReadDto>> GetById(int id)
        {
            var document = await _db.CompanyDocuments
                .AsNoTracking()
                .Where(cd => cd.DeletedAt == null)
                .Include(cd => cd.Company)
                .FirstOrDefaultAsync(cd => cd.Id == id);

            if (document == null)
                return NotFound(new { Message = "Document non trouv�" });

            var result = new CompanyDocumentReadDto
            {
                Id = document.Id,
                CompanyId = document.CompanyId,
                CompanyName = document.Company?.CompanyName ?? "N/A",
                Name = document.Name,
                FilePath = document.FilePath,
                DocumentType = document.DocumentType,
                CreatedAt = document.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re tous les documents d'une entreprise
        /// </summary>
        [HttpGet("company/{companyId}")]
        public async Task<ActionResult<IEnumerable<CompanyDocumentReadDto>>> GetByCompanyId(int companyId)
        {
            var companyExists = await _db.Companies.AnyAsync(c => c.Id == companyId && c.DeletedAt == null);
            if (!companyExists)
                return NotFound(new { Message = "Entreprise non trouv�e" });

            var documents = await _db.CompanyDocuments
                .AsNoTracking()
                .Where(cd => cd.CompanyId == companyId && cd.DeletedAt == null)
                .Include(cd => cd.Company)
                .OrderByDescending(cd => cd.CreatedAt)
                .ToListAsync();

            var result = documents.Select(cd => new CompanyDocumentReadDto
            {
                Id = cd.Id,
                CompanyId = cd.CompanyId,
                CompanyName = cd.Company?.CompanyName ?? "N/A",
                Name = cd.Name,
                FilePath = cd.FilePath,
                DocumentType = cd.DocumentType,
                CreatedAt = cd.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// Upload et cr�e un nouveau document
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
        public async Task<ActionResult<CompanyDocumentReadDto>> Upload([FromForm] CompanyDocumentUploadDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var companyExists = await _db.Companies.AnyAsync(c => c.Id == dto.CompanyId && c.DeletedAt == null);
            if (!companyExists)
                return NotFound(new { Message = "Entreprise non trouv�e" });

            try
            {
                // Sauvegarder le fichier
                var filePath = await _documentService.SaveFileAsync(dto.File, dto.CompanyId, dto.DocumentType);

                // Cr�er l'enregistrement en base
                var document = new CompanyDocument
                {
                    CompanyId = dto.CompanyId,
                    Name = dto.File.FileName,
                    FilePath = filePath,
                    DocumentType = dto.DocumentType,
                    CreatedBy = User.GetUserId(),
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _db.CompanyDocuments.Add(document);
                await _db.SaveChangesAsync();

                var createdDocument = await _db.CompanyDocuments
                    .Include(cd => cd.Company)
                    .FirstAsync(cd => cd.Id == document.Id);

                var result = new CompanyDocumentReadDto
                {
                    Id = createdDocument.Id,
                    CompanyId = createdDocument.CompanyId,
                    CompanyName = createdDocument.Company?.CompanyName ?? "N/A",
                    Name = createdDocument.Name,
                    FilePath = createdDocument.FilePath,
                    DocumentType = createdDocument.DocumentType,
                    CreatedAt = createdDocument.CreatedAt.DateTime
                };

                _logger.LogInformation("Document cr�� avec succ�s : {DocumentId} pour l'entreprise {CompanyId}", 
                    document.Id, dto.CompanyId);

                return CreatedAtAction(nameof(GetById), new { id = document.Id }, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Erreur de validation lors de l'upload : {Message}", ex.Message);
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'upload du document pour l'entreprise {CompanyId}", dto.CompanyId);
                return StatusCode(500, new { Message = "Erreur lors de l'upload du fichier" });
            }
        }

        /// <summary>
        /// Cr�e un nouveau document (sans upload de fichier)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CompanyDocumentReadDto>> Create([FromBody] CompanyDocumentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var companyExists = await _db.Companies.AnyAsync(c => c.Id == dto.CompanyId && c.DeletedAt == null);
            if (!companyExists)
                return NotFound(new { Message = "Entreprise non trouv�e" });

            var document = new CompanyDocument
            {
                CompanyId = dto.CompanyId,
                Name = dto.Name,
                FilePath = dto.FilePath,
                DocumentType = dto.DocumentType,
                CreatedBy = User.GetUserId(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.CompanyDocuments.Add(document);
            await _db.SaveChangesAsync();

            var createdDocument = await _db.CompanyDocuments
                .Include(cd => cd.Company)
                .FirstAsync(cd => cd.Id == document.Id);

            var result = new CompanyDocumentReadDto
            {
                Id = createdDocument.Id,
                CompanyId = createdDocument.CompanyId,
                CompanyName = createdDocument.Company?.CompanyName ?? "N/A",
                Name = createdDocument.Name,
                FilePath = createdDocument.FilePath,
                DocumentType = createdDocument.DocumentType,
                CreatedAt = createdDocument.CreatedAt.DateTime
            };

            _logger.LogInformation("Document cr�� avec succ�s : {DocumentId} pour l'entreprise {CompanyId}", 
                document.Id, dto.CompanyId);

            return CreatedAtAction(nameof(GetById), new { id = document.Id }, result);
        }

        /// <summary>
        /// Met � jour un document
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<CompanyDocumentReadDto>> Update(int id, [FromBody] CompanyDocumentUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var document = await _db.CompanyDocuments
                .Include(cd => cd.Company)
                .FirstOrDefaultAsync(cd => cd.Id == id && cd.DeletedAt == null);

            if (document == null)
                return NotFound(new { Message = "Document non trouv�" });

            // Mise � jour des champs modifiables
            if (!string.IsNullOrWhiteSpace(dto.Name))
                document.Name = dto.Name;

            if (dto.DocumentType != null)
                document.DocumentType = dto.DocumentType;

            await _db.SaveChangesAsync();

            var result = new CompanyDocumentReadDto
            {
                Id = document.Id,
                CompanyId = document.CompanyId,
                CompanyName = document.Company?.CompanyName ?? "N/A",
                Name = document.Name,
                FilePath = document.FilePath,
                DocumentType = document.DocumentType,
                CreatedAt = document.CreatedAt.DateTime
            };

            _logger.LogInformation("Document mis � jour : {DocumentId}", id);

            return Ok(result);
        }

        /// <summary>
        /// Supprime un document (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _db.CompanyDocuments
                .FirstOrDefaultAsync(cd => cd.Id == id && cd.DeletedAt == null);

            if (document == null)
                return NotFound(new { Message = "Document non trouv�" });

            // Soft delete
            document.DeletedAt = DateTimeOffset.UtcNow;
            document.DeletedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            // On Soft delete les documents, NE PAS SUPPRIMER LE DOCUMENT PHYSIQUEMENT
            //await _documentService.DeleteFileAsync(document.FilePath);

            _logger.LogInformation("Document supprim� : {DocumentId}", id);

            return NoContent();
        }

        /// <summary>
        /// T�l�charge un fichier
        /// </summary>
        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(int id)
        {
            var document = await _db.CompanyDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(cd => cd.Id == id && cd.DeletedAt == null);

            if (document == null)
                return NotFound(new { Message = "Document non trouv�" });

            var fileData = await _documentService.GetFileAsync(document.FilePath);
            if (fileData == null)
                return NotFound(new { Message = "Fichier non trouv� sur le serveur" });

            return File(fileData.Value.fileBytes, fileData.Value.contentType, fileData.Value.fileName);
        }
    }
}