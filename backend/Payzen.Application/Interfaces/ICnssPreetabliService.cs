using Microsoft.AspNetCore.Http;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;

namespace Payzen.Application.Interfaces;

public interface ICnssPreetabliService
{
    Task<ServiceResult<CnssPreetabliParseResultDto>> ParseAsync(
        int companyId,
        IFormFile file,
        CancellationToken ct = default
    );
    Task<ServiceResult<CnssPreetabliParseResultDto>> GetLatestAsync(
        int companyId,
        string? period,
        CancellationToken ct = default
    );
}
