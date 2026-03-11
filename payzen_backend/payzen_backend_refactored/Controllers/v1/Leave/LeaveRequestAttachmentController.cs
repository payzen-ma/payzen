using payzen_backend.Models.Leave;
using payzen_backend.Models.Leave.Dtos;
using payzen_backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using payzen_backend.Data;
using payzen_backend.Services;

namespace payzen_backend.Controllers.v1.Leave
{
    [Route("api/v{version:apiVersion}/leave-request-attachments")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class LeaveRequestAttachmentController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly LeaveEventLogService _leaveEventLogService;
        private readonly IWebHostEnvironment _environment;

        public LeaveRequestAttachmentController(
            AppDbContext db, 
            LeaveEventLogService leaveEventLogService,
            IWebHostEnvironment environment)
        {
            _db = db;
            _leaveEventLogService = leaveEventLogService;
            _environment = environment;
        }

        /// <summary>
        /// Récupère les pièces jointes d'une demande de congé
        /// </summary>
        [HttpGet("by-request/{leaveRequestId}")]
        public async Task<ActionResult<IEnumerable<LeaveRequestAttachmentReadDto>>> GetByRequest(int leaveRequestId)
        {
            var attachments = await _db.LeaveRequestAttachments
                .AsNoTracking()
                .Where(a => a.LeaveRequestId == leaveRequestId)
                .OrderByDescending(a => a.UploadedAt)
                .Select(a => new LeaveRequestAttachmentReadDto
                {
                    Id = a.Id,
                    LeaveRequestId = a.LeaveRequestId,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileType = a.FileType,
                    UploadedAt = a.UploadedAt,
                    UploadedBy = a.UploadedBy
                })
                .ToListAsync();

            return Ok(attachments);
        }

        /// <summary>
        /// Récupère une pièce jointe par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveRequestAttachmentReadDto>> GetById(int id)
        {
            var attachment = await _db.LeaveRequestAttachments
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => new LeaveRequestAttachmentReadDto
                {
                    Id = a.Id,
                    LeaveRequestId = a.LeaveRequestId,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileType = a.FileType,
                    UploadedAt = a.UploadedAt,
                    UploadedBy = a.UploadedBy
                })
                .FirstOrDefaultAsync();

            if (attachment == null)
                return NotFound(new { Message = "Pièce jointe non trouvée" });

            return Ok(attachment);
        }

        /// <summary>
        /// Upload un fichier pour une demande de congé
        /// </summary>
        [HttpPost("upload")]
        [RequestSizeLimit(10_485_760)] // 10 MB
        public async Task<ActionResult<LeaveRequestAttachmentReadDto>> Upload(
            [FromForm] int leaveRequestId,
            [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { Message = "Aucun fichier fourni" });
            }

            // Vérifier que la LeaveRequest existe
            var leaveRequest = await _db.LeaveRequests
                .FirstOrDefaultAsync(lr => lr.Id == leaveRequestId && lr.DeletedAt == null);

            if (leaveRequest == null)
            {
                return NotFound(new { Message = "Demande de congé non trouvée" });
            }

            // Valider le type de fichier
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { Message = $"Type de fichier non autorisé. Types autorisés: {string.Join(", ", allowedExtensions)}" });
            }

            // Valider la taille du fichier (max 10 MB)
            if (file.Length > 10_485_760)
            {
                return BadRequest(new { Message = "Le fichier ne doit pas dépasser 10 MB" });
            }

            var userId = User.GetUserId();

            // Créer le dossier de destination
            var uploadFolder = Path.Combine(_environment.ContentRootPath, "uploads", "leave-attachments");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Générer un nom de fichier unique
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadFolder, uniqueFileName);

            // Sauvegarder le fichier
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Créer l'enregistrement dans la base de données
            var attachment = new LeaveRequestAttachment
            {
                LeaveRequestId = leaveRequestId,
                FileName = Path.GetFileName(file.FileName),
                FilePath = $"uploads/leave-attachments/{uniqueFileName}",
                FileType = file.ContentType,
                UploadedAt = DateTimeOffset.UtcNow,
                UploadedBy = userId
            };

            _db.LeaveRequestAttachments.Add(attachment);
            await _db.SaveChangesAsync();

            // Logger l'événement
            await _leaveEventLogService.LogLeaveRequestEventAsync(
                leaveRequest.CompanyId,
                leaveRequest.EmployeeId,
                leaveRequest.Id,
                LeaveEventLogService.EventNames.AttachmentUploaded,
                null,
                $"Fichier '{file.FileName}' uploadé",
                userId
            );

            var readDto = new LeaveRequestAttachmentReadDto
            {
                Id = attachment.Id,
                LeaveRequestId = attachment.LeaveRequestId,
                FileName = attachment.FileName,
                FilePath = attachment.FilePath,
                FileType = attachment.FileType,
                UploadedAt = attachment.UploadedAt,
                UploadedBy = attachment.UploadedBy
            };

            return CreatedAtAction(nameof(GetById), new { id = attachment.Id }, readDto);
        }

        /// <summary>
        /// Télécharge un fichier
        /// </summary>
        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(int id)
        {
            var attachment = await _db.LeaveRequestAttachments
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attachment == null)
                return NotFound(new { Message = "Pièce jointe non trouvée" });

            var filePath = Path.Combine(_environment.ContentRootPath, attachment.FilePath);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { Message = "Fichier physique non trouvé sur le serveur" });
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            var contentType = attachment.FileType ?? "application/octet-stream";
            return File(memory, contentType, attachment.FileName);
        }

        /// <summary>
        /// Supprime une pièce jointe
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var attachment = await _db.LeaveRequestAttachments
                .Include(a => a.LeaveRequest)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attachment == null)
                return NotFound(new { Message = "Pièce jointe non trouvée" });

            var userId = User.GetUserId();
            var fileName = attachment.FileName;
            var leaveRequestId = attachment.LeaveRequestId;
            var companyId = attachment.LeaveRequest.CompanyId;
            var employeeId = attachment.LeaveRequest.EmployeeId;

            // Supprimer le fichier physique
            var filePath = Path.Combine(_environment.ContentRootPath, attachment.FilePath);
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    // Log l'erreur mais continuer avec la suppression de l'enregistrement
                    Console.WriteLine($"Erreur lors de la suppression du fichier: {ex.Message}");
                }
            }

            // Supprimer l'enregistrement de la base de données
            _db.LeaveRequestAttachments.Remove(attachment);
            await _db.SaveChangesAsync();

            // Logger l'événement
            await _leaveEventLogService.LogLeaveRequestEventAsync(
                companyId,
                employeeId,
                leaveRequestId,
                LeaveEventLogService.EventNames.AttachmentDeleted,
                $"Fichier '{fileName}' supprimé",
                null,
                userId
            );

            return NoContent();
        }
    }
}
