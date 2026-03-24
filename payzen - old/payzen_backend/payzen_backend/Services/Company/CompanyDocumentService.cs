using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Company;
using payzen_backend.Models.Company.Dtos;

namespace payzen_backend.Services.Company
{
    public interface ICompanyDocumentService
    {
        Task<string> SaveFileAsync(IFormFile file, int companyId, string? documentType);
        Task<bool> DeleteFileAsync(string filePath);
        Task<(byte[] fileBytes, string contentType, string fileName)?> GetFileAsync(string filePath);
    }

    public class CompanyDocumentService : ICompanyDocumentService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<CompanyDocumentService> _logger;
        private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx" };

        public CompanyDocumentService(IWebHostEnvironment environment, ILogger<CompanyDocumentService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<string> SaveFileAsync(IFormFile file, int companyId, string? documentType)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Le fichier est vide ou invalide");

            if (file.Length > MaxFileSize)
                throw new ArgumentException($"Le fichier dépasse la taille maximale de {MaxFileSize / (1024 * 1024)} MB");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                throw new ArgumentException($"Type de fichier non autorisé. Extensions autorisées : {string.Join(", ", AllowedExtensions)}");

            // Créer le chemin du dossier : uploads/companies/{companyId}/{documentType}
            var uploadFolder = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, 
                "uploads", "companies", companyId.ToString(), documentType ?? "general");
            
            Directory.CreateDirectory(uploadFolder);

            // Générer un nom de fichier unique
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadFolder, fileName);

            // Sauvegarder le fichier
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Retourner le chemin relatif
            var relativePath = Path.Combine("uploads", "companies", companyId.ToString(), 
                documentType ?? "general", fileName).Replace("\\", "/");
            
            _logger.LogInformation("Fichier sauvegardé : {FilePath} pour l'entreprise {CompanyId}", relativePath, companyId);
            
            return relativePath;
        }

        public Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, filePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Fichier supprimé : {FilePath}", filePath);
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du fichier : {FilePath}", filePath);
                return Task.FromResult(false);
            }
        }

        public Task<(byte[] fileBytes, string contentType, string fileName)?> GetFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, filePath);
                if (!File.Exists(fullPath))
                    return Task.FromResult<(byte[], string, string)?>(null);

                var fileBytes = File.ReadAllBytes(fullPath);
                var fileName = Path.GetFileName(filePath);
                var contentType = GetContentType(fileName);

                return Task.FromResult<(byte[], string, string)?>((fileBytes, contentType, fileName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du fichier : {FilePath}", filePath);
                return Task.FromResult<(byte[], string, string)?>(null);
            }
        }

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }
}
