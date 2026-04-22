using Microsoft.AspNetCore.Http;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;

namespace Payzen.Application.Interfaces;

public interface ICnssBdsService
{
    Task<ServiceResult<CnssBdsGenerationResultDto>> GeneratePrincipalBdsAsync(
        int companyId,
        IFormFile preetabliFile,
        CancellationToken ct = default
    );
}
