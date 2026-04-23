using Payzen.Application.Common;
using Payzen.Application.DTOs.Import;

namespace Payzen.Application.Interfaces;

public interface IModuleImportService
{
    Task<ServiceResult<ModuleImportResultDto>> ImportWorkbookAsync(
        Stream fileStream,
        string fileName,
        int month,
        int year,
        string mode,
        int? half,
        int? companyId,
        int? userId,
        bool sendWelcomeEmail,
        CancellationToken ct = default
    );
}
