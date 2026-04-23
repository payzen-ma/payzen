using Payzen.Application.Common;
using Payzen.Application.DTOs.Import;

namespace Payzen.Application.Interfaces;

public interface INewEmployeeImportService
{
    Task<ServiceResult<NewEmployeeImportResultDto>> ImportFromFileAsync(
        Stream fileStream,
        string fileName,
        int? companyId,
        int? userId,
        bool sendWelcomeEmail,
        CancellationToken ct = default
    );
}
