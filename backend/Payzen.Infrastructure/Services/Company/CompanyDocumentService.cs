using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Payzen.Application.Common;
using Payzen.Application.Interfaces;
using Payzen.Infrastructure.Hosting;

namespace Payzen.Infrastructure.Services.Company;

/// <summary>
/// Gestion des fichiers physiques liés aux documents d'entreprise.
/// Stockage dans wwwroot/uploads/companies/{companyId}/
/// </summary>
public class CompanyDocumentService : ICompanyDocumentService
{
    private readonly IWebHostEnvironment _env;

    public CompanyDocumentService(IWebHostEnvironment env) => _env = env;

    public async Task<ServiceResult<string>> SaveFileAsync(
        IFormFile file, int companyId, string? documentType, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return ServiceResult<string>.Fail("Fichier vide ou manquant.");

        var webRoot = WebRootPathHelper.ResolveWwwRoot(_env);
        var folder = Path.Combine(webRoot, "uploads", "companies", companyId.ToString());
        Directory.CreateDirectory(folder);

        var ext = Path.GetExtension(file.FileName);
        var fileName = $"{documentType ?? "doc"}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        var relativePath = Path.Combine("uploads", "companies", companyId.ToString(), fileName)
                               .Replace('\\', '/');
        return ServiceResult<string>.Ok(relativePath);
    }

    public Task<ServiceResult> DeleteFileAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult(ServiceResult.Fail("Chemin de fichier manquant."));

        var webRoot = WebRootPathHelper.ResolveWwwRoot(_env);
        var fullPath = Path.Combine(webRoot, filePath.TrimStart('/'));
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.FromResult(ServiceResult.Ok());
    }

    public async Task<ServiceResult<(byte[] fileBytes, string contentType, string fileName)>> GetFileAsync(
        string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return ServiceResult<(byte[], string, string)>.Fail("Chemin de fichier manquant.");

        var webRoot = WebRootPathHelper.ResolveWwwRoot(_env);
        var fullPath = Path.Combine(webRoot, filePath.TrimStart('/'));
        if (!File.Exists(fullPath))
            return ServiceResult<(byte[], string, string)>.Fail("Fichier introuvable.");

        var bytes = await File.ReadAllBytesAsync(fullPath, ct);
        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
        return ServiceResult<(byte[], string, string)>.Ok((bytes, contentType, Path.GetFileName(fullPath)));
    }
}
