using Microsoft.AspNetCore.Http;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;

namespace Payzen.Application.Interfaces;

public interface ICnssPreetabliService
{
    Task<ServiceResult<CnssPreetabliParseResultDto>> ParseAsync(IFormFile file, CancellationToken ct = default);
}
