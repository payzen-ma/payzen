using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Payroll;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Payroll;

public class CnssPreetabliService : ICnssPreetabliService
{
    private readonly AppDbContext _db;

    public CnssPreetabliService(AppDbContext db) => _db = db;

    public async Task<ServiceResult<CnssPreetabliParseResultDto>> ParseAsync(
        int companyId,
        IFormFile file,
        CancellationToken ct = default
    )
    {
        if (companyId <= 0)
            return ServiceResult<CnssPreetabliParseResultDto>.Fail("CompanyId invalide.");

        if (file.Length == 0)
            return ServiceResult<CnssPreetabliParseResultDto>.Fail("Le fichier est vide.");

        if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            return ServiceResult<CnssPreetabliParseResultDto>.Fail("Le fichier doit être au format .txt");

        var result = new CnssPreetabliParseResultDto();
        result.SourceFileName = file.FileName;
        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var lineNumber = 0;
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var raw = await reader.ReadLineAsync(ct) ?? string.Empty;
            lineNumber++;

            var line = raw.Replace("\r", string.Empty);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.Length < 3)
            {
                result.Issues.Add(new CnssPreetabliIssueDto
                {
                    LineNumber = lineNumber,
                    Message = "Ligne trop courte pour déterminer le type d'enregistrement.",
                });
                continue;
            }

            var type = line[..3];
            if (type is "A00" or "A01" or "A02" or "A03")
            {
                ValidateRecordLength260(line, lineNumber, type, result);
            }

            switch (type)
            {
                case "A00":
                    ParseA00(line, lineNumber, result);
                    break;
                case "A01":
                    ParseA01(line, lineNumber, result);
                    break;
                case "A02":
                    ParseA02(line, lineNumber, result);
                    break;
                case "A03":
                    ParseA03(line, lineNumber, result);
                    break;
                default:
                    result.Issues.Add(new CnssPreetabliIssueDto
                    {
                        LineNumber = lineNumber,
                        Severity = "warning",
                        Message = $"Type d'enregistrement inconnu: {type}.",
                    });
                    break;
            }
        }

        ValidateStructure(result);
        await SaveImportAsync(companyId, result, ct);
        return ServiceResult<CnssPreetabliParseResultDto>.Ok(result);
    }

    public async Task<ServiceResult<CnssPreetabliParseResultDto>> GetLatestAsync(
        int companyId,
        string? period,
        CancellationToken ct = default
    )
    {
        if (companyId <= 0)
            return ServiceResult<CnssPreetabliParseResultDto>.Fail("CompanyId invalide.");

        var query = _db
            .CnssPreetabliImports.AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(period))
            query = query.Where(x => x.Period == period);

        var import = await query.OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(ct);
        if (import == null)
            return ServiceResult<CnssPreetabliParseResultDto>.Fail("Aucun préétabli importé pour ces critères.");

        var data = new CnssPreetabliParseResultDto
        {
            ImportId = import.Id,
            ImportedAt = import.CreatedAt,
            SourceFileName = import.FileName,
            Header = new CnssPreetabliHeaderDto
            {
                AffiliateNumber = import.AffiliateNumber,
                Period = import.Period,
            },
            Employees = import
                .Lines.OrderBy(l => l.LineNumber)
                .Select(l => new CnssPreetabliEmployeeRowDto
                {
                    LineNumber = l.LineNumber,
                    RecordType = "A02",
                    AffiliateNumber = l.AffiliateNumber,
                    Period = l.Period,
                    InsuredNumber = l.InsuredNumber,
                    FullName = l.FullName,
                    ChildrenCount = l.ChildrenCount,
                    FamilyAllowanceToPayCentimes = DecimalToCentimesInt(l.FamilyAllowanceToPay),
                    FamilyAllowanceToDeductCentimes = DecimalToCentimesInt(l.FamilyAllowanceToDeduct),
                    FamilyAllowanceNetToPayCentimes = DecimalToCentimesInt(l.FamilyAllowanceNetToPay),
                    FamilyAllowanceToPay = l.FamilyAllowanceToPay,
                    FamilyAllowanceToDeduct = l.FamilyAllowanceToDeduct,
                    FamilyAllowanceNetToPay = l.FamilyAllowanceNetToPay,
                })
                .ToList(),
            Summary = new CnssPreetabliSummaryDto
            {
                RecordType = "A03",
                AffiliateNumber = import.AffiliateNumber,
                Period = import.Period,
                EmployeeCount = import.EmployeeCount,
                TotalChildren = import.Lines.Sum(l => l.ChildrenCount),
                TotalFamilyAllowanceToPay = import.Lines.Sum(l => l.FamilyAllowanceToPay),
                TotalFamilyAllowanceToDeduct = import.Lines.Sum(l => l.FamilyAllowanceToDeduct),
                TotalFamilyAllowanceNetToPay = import.Lines.Sum(l => l.FamilyAllowanceNetToPay),
                TotalInsuredNumbers = import.Lines.Select(l => ParseLong(l.InsuredNumber)).Sum(),
            },
        };

        return ServiceResult<CnssPreetabliParseResultDto>.Ok(data);
    }

    private static void ParseA00(string line, int lineNumber, CnssPreetabliParseResultDto result)
    {
        var header = result.Header ?? new CnssPreetabliHeaderDto();
        header.NatureRecordType = Slice(line, 1, 3);
        header.TransferIdentifier = Slice(line, 4, 14);
        header.Category = Slice(line, 18, 2);
        header.ReservedZoneA00 = Slice(line, 20, 241);
        result.Header = header;
    }

    private static void ParseA01(string line, int lineNumber, CnssPreetabliParseResultDto result)
    {
        var header = result.Header ?? new CnssPreetabliHeaderDto();
        header.GlobalHeaderRecordType = Slice(line, 1, 3);
        header.AffiliateNumber = Slice(line, 4, 7);
        header.Period = Slice(line, 11, 6);
        header.CompanyName = Slice(line, 17, 40);
        header.Activity = Slice(line, 57, 40);
        header.Address = Slice(line, 97, 120);
        header.City = Slice(line, 217, 20);
        header.PostalCode = Slice(line, 237, 6);
        header.AgencyCode = Slice(line, 243, 2);
        header.EmissionDateRaw = Slice(line, 245, 8);
        header.ExigibilityDateRaw = Slice(line, 253, 8);
        result.Header = header;
    }

    private static void ParseA02(string line, int lineNumber, CnssPreetabliParseResultDto result)
    {
        var afPayerCentimes = ParseInt(Slice(line, 88, 6));
        var afDeduireCentimes = ParseInt(Slice(line, 94, 6));
        var afNetCentimes = ParseInt(Slice(line, 100, 6));
        var row = new CnssPreetabliEmployeeRowDto
        {
            LineNumber = lineNumber,
            RecordType = Slice(line, 1, 3),
            AffiliateNumber = Slice(line, 4, 7),
            Period = Slice(line, 11, 6),
            InsuredNumber = Slice(line, 17, 9),
            FullName = Slice(line, 26, 60),
            ChildrenCount = ParseInt(Slice(line, 86, 2)),
            FamilyAllowanceToPayCentimes = afPayerCentimes,
            FamilyAllowanceToDeductCentimes = afDeduireCentimes,
            FamilyAllowanceNetToPayCentimes = afNetCentimes,
            FamilyAllowanceToPay = ParseCentimesToDecimal(afPayerCentimes),
            FamilyAllowanceToDeduct = ParseCentimesToDecimal(afDeduireCentimes),
            FamilyAllowanceNetToPay = ParseCentimesToDecimal(afNetCentimes),
            ReservedZone = Slice(line, 106, 155),
        };
        result.Employees.Add(row);
    }

    private static void ParseA03(string line, int lineNumber, CnssPreetabliParseResultDto result)
    {
        var totalPayerCentimes = ParseLong(Slice(line, 29, 12));
        var totalDeduireCentimes = ParseLong(Slice(line, 41, 12));
        var totalNetCentimes = ParseLong(Slice(line, 53, 12));
        result.Summary = new CnssPreetabliSummaryDto
        {
            RecordType = Slice(line, 1, 3),
            AffiliateNumber = Slice(line, 4, 7),
            Period = Slice(line, 11, 6),
            EmployeeCount = ParseInt(Slice(line, 17, 6)),
            TotalChildren = ParseInt(Slice(line, 23, 6)),
            TotalFamilyAllowanceToPay = ParseCentimesToDecimal(totalPayerCentimes),
            TotalFamilyAllowanceToDeduct = ParseCentimesToDecimal(totalDeduireCentimes),
            TotalFamilyAllowanceNetToPay = ParseCentimesToDecimal(totalNetCentimes),
            TotalInsuredNumbers = ParseLong(Slice(line, 65, 15)),
            ReservedZone = Slice(line, 80, 181),
        };
    }

    private static void ValidateStructure(CnssPreetabliParseResultDto result)
    {
        if (result.Header == null)
        {
            result.Issues.Add(new CnssPreetabliIssueDto { Message = "En-tête A00/A01 introuvable." });
            return;
        }

        if (string.IsNullOrWhiteSpace(result.Header.AffiliateNumber))
            result.Issues.Add(new CnssPreetabliIssueDto { Message = "Numéro d'affilié (A01) introuvable." });

        if (string.IsNullOrWhiteSpace(result.Header.Period))
            result.Issues.Add(new CnssPreetabliIssueDto { Message = "Période (A01) introuvable." });

        if (result.Summary == null)
            result.Issues.Add(new CnssPreetabliIssueDto { Message = "Récapitulatif A03 introuvable." });

        foreach (var row in result.Employees)
        {
            if (!string.Equals(row.AffiliateNumber, result.Header.AffiliateNumber, StringComparison.Ordinal))
            {
                result.Issues.Add(new CnssPreetabliIssueDto
                {
                    LineNumber = row.LineNumber,
                    Severity = "warning",
                    Message = "Numéro d'affilié de la ligne A02 différent de A01.",
                });
            }

            if (!string.Equals(row.Period, result.Header.Period, StringComparison.Ordinal))
            {
                result.Issues.Add(new CnssPreetabliIssueDto
                {
                    LineNumber = row.LineNumber,
                    Severity = "warning",
                    Message = "Période de la ligne A02 différente de A01.",
                });
            }
        }
    }

    private async Task SaveImportAsync(int companyId, CnssPreetabliParseResultDto result, CancellationToken ct)
    {
        var import = new CnssPreetabliImport
        {
            CompanyId = companyId,
            FileName = result.SourceFileName,
            AffiliateNumber = result.Header?.AffiliateNumber ?? string.Empty,
            Period = result.Header?.Period ?? string.Empty,
            EmployeeCount = result.Employees.Count,
            IssueCount = result.Issues.Count,
            Status = result.Issues.Any(i => string.Equals(i.Severity, "error", StringComparison.OrdinalIgnoreCase))
                ? "parsed_with_errors"
                : "parsed",
            CreatedBy = 0,
            Lines = result
                .Employees.Select(e => new CnssPreetabliLine
                {
                    LineNumber = e.LineNumber,
                    AffiliateNumber = e.AffiliateNumber,
                    Period = e.Period,
                    InsuredNumber = e.InsuredNumber,
                    FullName = e.FullName,
                    ChildrenCount = e.ChildrenCount,
                    FamilyAllowanceToPay = e.FamilyAllowanceToPay,
                    FamilyAllowanceToDeduct = e.FamilyAllowanceToDeduct,
                    FamilyAllowanceNetToPay = e.FamilyAllowanceNetToPay,
                    CreatedBy = 0,
                })
                .ToList(),
        };

        _db.CnssPreetabliImports.Add(import);
        await _db.SaveChangesAsync(ct);
        result.ImportId = import.Id;
        result.ImportedAt = import.CreatedAt;
    }

    private static string Slice(string source, int startOneBased, int length)
    {
        var start = Math.Max(0, startOneBased - 1);
        if (source.Length <= start)
            return string.Empty;
        var take = Math.Min(length, source.Length - start);
        return source.Substring(start, take).Trim();
    }

    private static int ParseInt(string raw)
    {
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private static long ParseLong(string raw)
    {
        return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0L;
    }

    private static decimal ParseCentimesToDecimal(string raw)
    {
        if (!decimal.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return 0m;
        return value / 100m;
    }

    private static decimal ParseCentimesToDecimal(long centimes) => centimes / 100m;

    private static decimal ParseCentimesToDecimal(int centimes) => centimes / 100m;

    private static int DecimalToCentimesInt(decimal value) =>
        (int)Math.Round(value * 100m, 0, MidpointRounding.AwayFromZero);

    private static void ValidateRecordLength260(
        string line,
        int lineNumber,
        string recordType,
        CnssPreetabliParseResultDto result
    )
    {
        if (line.Length == 260)
            return;

        result.Issues.Add(new CnssPreetabliIssueDto
        {
            LineNumber = lineNumber,
            Severity = "warning",
            Message = $"{recordType}: longueur {line.Length} caractères (attendu 260).",
        });
    }
}
