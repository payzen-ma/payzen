using Payzen.Application.Common;

namespace Payzen.Application.Interfaces;

public interface IImportTemplateService
{
    Task<ServiceResult<(byte[] Content, string FileName)>> GenerateNewEmployeeTemplateAsync(
        int? companyId,
        int? userId,
        CancellationToken ct = default
    );
}
