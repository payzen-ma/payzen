using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Payroll;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Payroll;

public class CnssBdsService : ICnssBdsService
{
    private const long SmigMonthlyCentimes = 184_184;
    private readonly ICnssPreetabliService _preetabliService;
    private readonly AppDbContext _db;

    public CnssBdsService(ICnssPreetabliService preetabliService, AppDbContext db)
    {
        _preetabliService = preetabliService;
        _db = db;
    }

    public async Task<ServiceResult<CnssBdsGenerationResultDto>> GeneratePrincipalBdsAsync(
        int companyId,
        IFormFile preetabliFile,
        CancellationToken ct = default
    )
    {
        var parsed = await _preetabliService.ParseAsync(companyId, preetabliFile, ct);
        if (!parsed.Success || parsed.Data?.Header == null)
            return ServiceResult<CnssBdsGenerationResultDto>.Fail(parsed.Error ?? "Préétabli invalide.");

        var data = parsed.Data;
        var period = data.Header.Period;
        if (period.Length != 6 || !int.TryParse(period[..4], out var year) || !int.TryParse(period[4..], out var month))
            return ServiceResult<CnssBdsGenerationResultDto>.Fail("Période du préétabli invalide.");

        var payrollResults = await _db
            .PayrollResults.AsNoTracking()
            .Include(p => p.Employee)
            .Where(p =>
                p.CompanyId == companyId
                && p.Year == year
                && p.Month == month
                && p.Status == PayrollResultStatus.OK
                && p.DeletedAt == null
            )
            .ToListAsync(ct);

        var payrollByCnss = payrollResults
            .Where(p => p.Employee != null && !string.IsNullOrWhiteSpace(p.Employee.CnssNumber))
            .GroupBy(p => NormalizeImmatriculation(p.Employee.CnssNumber!))
            .ToDictionary(g => g.Key, g => g.First());

        var b02Details = BuildB02Details(data.Employees, payrollByCnss, data.Header.AffiliateNumber, data.Header.Period);
        var b03 = BuildB03(b02Details, data.Header.AffiliateNumber, data.Header.Period);

        var entrants = BuildEntrants(payrollResults, data.Employees, data.Header.AffiliateNumber, data.Header.Period);
        var b05 = BuildB05(entrants, data.Header.AffiliateNumber, data.Header.Period);
        var b06 = BuildB06(b03, b05, data.Header.AffiliateNumber, data.Header.Period);

        var validationIssues = ValidateBds(data.Header, b02Details, b03, entrants, b05, b06);
        var blockingErrors = validationIssues
            .Where(i => i.IsBlocking)
            .Select(i => $"[BLOQUANTE] {i.Scope}: {i.Message}")
            .ToList();
        if (blockingErrors.Count > 0)
            return ServiceResult<CnssBdsGenerationResultDto>.Fail(blockingErrors);

        var lines = new List<string>
        {
            BuildB00(data.Header),
            BuildB01(data.Header),
        };
        lines.AddRange(b02Details.OrderBy(x => x.NumAssure).Select(BuildB02));
        lines.Add(BuildB03Line(b03));
        lines.AddRange(entrants.Select(BuildB04));
        lines.Add(BuildB05Line(b05));
        lines.Add(BuildB06Line(b06));

        var content = string.Join("\n", lines);
        var fileName = $"DS_{data.Header.AffiliateNumber}_{month:D2}{year}.txt";
        return ServiceResult<CnssBdsGenerationResultDto>.Ok(
            new CnssBdsGenerationResultDto
            {
                Content = Encoding.ASCII.GetBytes(content),
                FileName = fileName,
                Warnings = validationIssues
                    .Where(i => !i.IsBlocking)
                    .Select(i => $"[WARNING] {i.Scope}: {i.Message}")
                    .ToList(),
            }
        );
    }

    private static string BuildB00(CnssPreetabliHeaderDto h)
    {
        return BuildLine(
            AN("B00", 3),
            N(h.TransferIdentifier, 14),
            AN("B0", 2),
            AN("", 241)
        );
    }

    private static string BuildB01(CnssPreetabliHeaderDto h)
    {
        return BuildLine(
            AN("B01", 3),
            N(h.AffiliateNumber, 7),
            N(h.Period, 6),
            AN(h.CompanyName, 40),
            AN(h.Activity, 40),
            AN(h.Address, 120),
            AN(h.City, 20),
            AN(h.PostalCode, 6),
            N(h.AgencyCode, 2),
            N(h.EmissionDateRaw, 8),
            N(h.ExigibilityDateRaw, 8)
        );
    }

    private static List<B02LineModel> BuildB02Details(
        List<CnssPreetabliEmployeeRowDto> preetabliRows,
        Dictionary<string, PayrollResult> payrollByCnss,
        string affiliateNumber,
        string period
    )
    {
        var list = new List<B02LineModel>();
        foreach (var row in preetabliRows)
        {
            var key = NormalizeImmatriculation(row.InsuredNumber);
            payrollByCnss.TryGetValue(key, out var payroll);

            var jours = payroll != null ? 26 : 0;
            var salaireReel = payroll != null ? ToCentimes(payroll.TotalBrut ?? 0m) : 0L;
            var salairePlaf = payroll != null ? Math.Min(ToCentimes(payroll.CnssBase ?? payroll.TotalBrut ?? 0m), 999_999_999L) : 0L;
            var situation = payroll != null ? "" : "MS";
            var afReverser = 0L;
            var ctr = ToLong(row.InsuredNumber) + afReverser + jours + salaireReel + salairePlaf + SituationRank(situation);

            list.Add(new B02LineModel
            {
                AffiliateNumber = affiliateNumber,
                Period = period,
                NumAssure = key,
                NomPrenom = row.FullName,
                Enfants = row.ChildrenCount,
                AfPayer = row.FamilyAllowanceToPayCentimes,
                AfDeduire = row.FamilyAllowanceToDeductCentimes,
                AfNet = row.FamilyAllowanceNetToPayCentimes,
                AfReverser = afReverser,
                Jours = jours,
                SalaireReel = salaireReel,
                SalairePlaf = salairePlaf,
                Situation = situation,
                Ctr = ctr,
            });
        }
        return list;
    }

    private static string BuildB02(B02LineModel b)
    {
        return BuildLine(
            AN("B02", 3),
            N(b.AffiliateNumber, 7),
            N(b.Period, 6),
            N(b.NumAssure, 9),
            AN(b.NomPrenom, 60),
            N(b.Enfants, 2),
            N(b.AfPayer, 6),
            N(b.AfDeduire, 6),
            N(b.AfNet, 6),
            N(b.AfReverser, 6),
            N(b.Jours, 2),
            N(b.SalaireReel, 13),
            N(b.SalairePlaf, 9),
            AN(b.Situation, 2),
            N(b.Ctr, 19),
            AN("", 104)
        );
    }

    private static B03Model BuildB03(List<B02LineModel> details, string affiliateNumber, string period)
    {
        return new B03Model
        {
            AffiliateNumber = affiliateNumber,
            Period = period,
            NbrSalaries = details.Count,
            TEnfants = details.Sum(x => x.Enfants),
            TAfPayer = details.Sum(x => x.AfPayer),
            TAfDeduire = details.Sum(x => x.AfDeduire),
            TAfNet = details.Sum(x => x.AfNet),
            TNumImma = details.Sum(x => ToLong(x.NumAssure)),
            TAfReverser = details.Sum(x => x.AfReverser),
            TJours = details.Sum(x => x.Jours),
            TSalaireReel = details.Sum(x => x.SalaireReel),
            TSalairePlaf = details.Sum(x => x.SalairePlaf),
            TCtr = details.Sum(x => x.Ctr),
        };
    }

    private static string BuildB03Line(B03Model b)
    {
        return BuildLine(
            AN("B03", 3),
            N(b.AffiliateNumber, 7),
            N(b.Period, 6),
            N(b.NbrSalaries, 6),
            N(b.TEnfants, 6),
            N(b.TAfPayer, 12),
            N(b.TAfDeduire, 12),
            N(b.TAfNet, 12),
            N(b.TNumImma, 15),
            N(b.TAfReverser, 12),
            N(b.TJours, 6),
            N(b.TSalaireReel, 15),
            N(b.TSalairePlaf, 13),
            N(b.TCtr, 19),
            AN("", 116)
        );
    }

    private static List<B04LineModel> BuildEntrants(
        List<PayrollResult> payrollResults,
        List<CnssPreetabliEmployeeRowDto> preetabliRows,
        string affiliateNumber,
        string period
    )
    {
        var existing = preetabliRows.Select(x => NormalizeImmatriculation(x.InsuredNumber)).ToHashSet();
        var entrants = payrollResults
            .Where(p => p.Employee != null)
            .Where(p =>
            {
                var imma = NormalizeImmatriculation(p.Employee.CnssNumber);
                return !string.IsNullOrWhiteSpace(imma) && !existing.Contains(imma);
            })
            .Select(p =>
            {
                var numAssure = NormalizeImmatriculation(p.Employee.CnssNumber);
                var salReel = ToCentimes(p.TotalBrut ?? 0m);
                var salPlaf = Math.Min(ToCentimes(p.CnssBase ?? p.TotalBrut ?? 0m), 999_999_999L);
                var jours = 26;
                var ctr = ToLong(numAssure) + jours + salReel + salPlaf;
                return new B04LineModel
                {
                    AffiliateNumber = affiliateNumber,
                    Period = period,
                    NumAssure = numAssure,
                    NomPrenom = $"{p.Employee.LastName} {p.Employee.FirstName}".Trim(),
                    NumCin = p.Employee.CinNumber ?? string.Empty,
                    Jours = jours,
                    SalaireReel = salReel,
                    SalairePlaf = salPlaf,
                    Ctr = ctr,
                };
            })
            .OrderBy(x => x.NumAssure)
            .ToList();

        if (entrants.Count > 0)
            return entrants;

        return
        [
            new B04LineModel
            {
                AffiliateNumber = affiliateNumber,
                Period = period,
                NumAssure = "         ",
                NomPrenom = string.Empty,
                NumCin = string.Empty,
                Jours = 0,
                SalaireReel = 0,
                SalairePlaf = 0,
                Ctr = 0,
            },
        ];
    }

    private static string BuildB04(B04LineModel b)
    {
        var numAssure = b.NumAssure == "         " ? AN("", 9) : N(b.NumAssure, 9);
        return BuildLine(
            AN("B04", 3),
            N(b.AffiliateNumber, 7),
            N(b.Period, 6),
            numAssure,
            AN(b.NomPrenom, 60),
            AN(b.NumCin, 8),
            N(b.Jours, 2),
            N(b.SalaireReel, 13),
            N(b.SalairePlaf, 9),
            N(b.Ctr, 19),
            AN("", 124)
        );
    }

    private static B05Model BuildB05(List<B04LineModel> entrants, string affiliateNumber, string period)
    {
        var actualEntrants = entrants.Where(x => x.NumAssure != "         ").ToList();
        return new B05Model
        {
            AffiliateNumber = affiliateNumber,
            Period = period,
            NbrSalaries = actualEntrants.Count,
            TNumImma = actualEntrants.Sum(x => ToLong(x.NumAssure)),
            TJours = actualEntrants.Sum(x => x.Jours),
            TSalaireReel = actualEntrants.Sum(x => x.SalaireReel),
            TSalairePlaf = actualEntrants.Sum(x => x.SalairePlaf),
            TCtr = actualEntrants.Sum(x => x.Ctr),
        };
    }

    private static string BuildB05Line(B05Model b)
    {
        return BuildLine(
            AN("B05", 3),
            N(b.AffiliateNumber, 7),
            N(b.Period, 6),
            N(b.NbrSalaries, 6),
            N(b.TNumImma, 15),
            N(b.TJours, 6),
            N(b.TSalaireReel, 15),
            N(b.TSalairePlaf, 13),
            N(b.TCtr, 19),
            AN("", 170)
        );
    }

    private static B06Model BuildB06(B03Model b03, B05Model b05, string affiliateNumber, string period)
    {
        return new B06Model
        {
            AffiliateNumber = affiliateNumber,
            Period = period,
            NbrSalaries = b03.NbrSalaries + b05.NbrSalaries,
            TNumImma = b03.TNumImma + b05.TNumImma,
            TJours = b03.TJours + b05.TJours,
            TSalaireReel = b03.TSalaireReel + b05.TSalaireReel,
            TSalairePlaf = b03.TSalairePlaf + b05.TSalairePlaf,
            TCtr = b03.TCtr + b05.TCtr,
        };
    }

    private static string BuildB06Line(B06Model b)
    {
        return BuildLine(
            AN("B06", 3),
            N(b.AffiliateNumber, 7),
            N(b.Period, 6),
            N(b.NbrSalaries, 6),
            N(b.TNumImma, 15),
            N(b.TJours, 6),
            N(b.TSalaireReel, 15),
            N(b.TSalairePlaf, 13),
            N(b.TCtr, 19),
            AN("", 170)
        );
    }

    private static string BuildLine(params string[] segments)
    {
        var line = string.Concat(segments);
        return line.Length >= 260 ? line[..260] : line.PadRight(260, ' ');
    }

    private static string AN(string? value, int length)
    {
        var v = (value ?? string.Empty).Trim();
        if (v.Length > length)
            v = v[..length];
        return v.PadRight(length, ' ');
    }

    private static string N(string? value, int length) => N(ToLong(value), length);

    private static string N(int value, int length) => N((long)value, length);

    private static string N(long value, int length)
    {
        var v = Math.Max(0, value).ToString();
        if (v.Length > length)
            v = v[^length..];
        return v.PadLeft(length, '0');
    }

    private static string NormalizeImmatriculation(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits))
            return string.Empty;
        return digits.Length >= 9 ? digits[^9..] : digits.PadLeft(9, '0');
    }

    private static long ToCentimes(decimal amountMad) =>
        (long)Math.Round(amountMad * 100m, 0, MidpointRounding.AwayFromZero);

    private static long ToLong(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return long.TryParse(digits, out var parsed) ? parsed : 0;
    }

    private static int SituationRank(string situation) =>
        situation switch
        {
            "SO" => 1,
            "DE" => 2,
            "IT" => 3,
            "IL" => 4,
            "AT" => 5,
            "CS" => 6,
            "MS" => 7,
            "MP" => 8,
            _ => 0,
        };

    private static List<ValidationIssue> ValidateBds(
        CnssPreetabliHeaderDto header,
        List<B02LineModel> b02,
        B03Model b03,
        List<B04LineModel> b04,
        B05Model b05,
        B06Model b06
    )
    {
        var issues = new List<ValidationIssue>();
        var allowedSituations = new HashSet<string> { "", "SO", "DE", "IT", "IL", "AT", "CS", "MS", "MP" };

        if (string.IsNullOrWhiteSpace(header.AffiliateNumber))
            issues.Add(ValidationIssue.Blocking("B01", "Numéro d'affilié manquant."));
        if (string.IsNullOrWhiteSpace(header.Period) || header.Period.Length != 6)
            issues.Add(ValidationIssue.Blocking("B01", "Période invalide (format AAAAMM attendu)."));

        var duplicateImma = b02
            .GroupBy(x => x.NumAssure)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        foreach (var d in duplicateImma)
            issues.Add(ValidationIssue.Blocking("B02", $"Numéro d'immatriculation en double: {d}."));

        foreach (var line in b02)
        {
            if (line.Jours > 26)
                issues.Add(ValidationIssue.Blocking("B02", $"Jours déclarés > 26 pour assuré {line.NumAssure}."));

            if (!allowedSituations.Contains(line.Situation))
                issues.Add(ValidationIssue.Blocking("B02", $"Situation invalide pour assuré {line.NumAssure}: '{line.Situation}'."));

            if (line.SalairePlaf > line.SalaireReel)
                issues.Add(ValidationIssue.Blocking("B02", $"Salaire plafonné > salaire réel pour assuré {line.NumAssure}."));

            if ((line.Situation == "CS" || line.Situation == "MS") && (line.Jours != 0 || line.SalaireReel != 0 || line.SalairePlaf != 0))
                issues.Add(ValidationIssue.Blocking("B02", $"Situation {line.Situation}: jours et salaires doivent être nuls pour {line.NumAssure}."));

            if (line.Situation == "" && (line.Jours <= 0 || line.SalaireReel <= 0 || line.SalairePlaf <= 0))
                issues.Add(ValidationIssue.Blocking("B02", $"Situation normale: jours/salaires obligatoires pour {line.NumAssure}."));

            if (line.AfReverser > line.AfNet)
                issues.Add(ValidationIssue.Blocking("B02", $"AF à reverser > AF net pour {line.NumAssure}."));

            if ((line.Situation == "SO" || line.Situation == "DE") && line.AfReverser != line.AfNet)
                issues.Add(ValidationIssue.Blocking("B02", $"Situation {line.Situation}: AF à reverser doit être égal à AF net pour {line.NumAssure}."));

            var expectedCtr = ToLong(line.NumAssure) + line.AfReverser + line.Jours + line.SalaireReel + line.SalairePlaf + SituationRank(line.Situation);
            if (line.Ctr != expectedCtr)
                issues.Add(ValidationIssue.Blocking("B02", $"Somme horizontale erronée pour {line.NumAssure}."));

            if (line.Jours > 0)
            {
                var minExpected = (SmigMonthlyCentimes / 26m) * Math.Max(0, line.Jours - 1);
                if (line.SalaireReel <= (long)Math.Round(minExpected, 0, MidpointRounding.AwayFromZero))
                    issues.Add(ValidationIssue.Warning("B02", $"Salaire/SMIG suspect pour {line.NumAssure}."));
            }
        }

        if (b03.NbrSalaries != b02.Count)
            issues.Add(ValidationIssue.Blocking("B03", "Nombre salariés B03 incohérent."));
        if (b03.TEnfants != b02.Sum(x => x.Enfants))
            issues.Add(ValidationIssue.Blocking("B03", "Total enfants B03 incohérent."));
        if (b03.TAfPayer != b02.Sum(x => x.AfPayer))
            issues.Add(ValidationIssue.Blocking("B03", "Total AF à payer B03 incohérent."));
        if (b03.TAfDeduire != b02.Sum(x => x.AfDeduire))
            issues.Add(ValidationIssue.Blocking("B03", "Total AF à déduire B03 incohérent."));
        if (b03.TAfNet != b02.Sum(x => x.AfNet))
            issues.Add(ValidationIssue.Blocking("B03", "Total AF net B03 incohérent."));
        if (b03.TCtr != b02.Sum(x => x.Ctr))
            issues.Add(ValidationIssue.Blocking("B03", "Total contrôles B03 incohérent."));

        var entrantsNoBlank = b04.Where(x => x.NumAssure != "         ").ToList();
        foreach (var e in entrantsNoBlank)
        {
            if (e.Jours <= 0 || e.Jours > 26)
                issues.Add(ValidationIssue.Blocking("B04", $"Nombre de jours invalide pour entrant {e.NumAssure}."));
            if (e.SalaireReel <= 0)
                issues.Add(ValidationIssue.Blocking("B04", $"Salaire réel invalide pour entrant {e.NumAssure}."));
            if (e.SalairePlaf > e.SalaireReel)
                issues.Add(ValidationIssue.Blocking("B04", $"Salaire plafonné > réel pour entrant {e.NumAssure}."));

            if (e.NumAssure != "000000000" && e.NumAssure != "999999999")
            {
                if (e.NumAssure.Length != 9 || !e.NumAssure.StartsWith("1", StringComparison.Ordinal) || e.NumAssure == "100000000")
                    issues.Add(ValidationIssue.Blocking("B04", $"Numéro d'immatriculation entrant invalide: {e.NumAssure}."));
            }

            var expectedCtr = ToLong(e.NumAssure) + e.Jours + e.SalaireReel + e.SalairePlaf;
            if (e.Ctr != expectedCtr)
                issues.Add(ValidationIssue.Blocking("B04", $"Somme horizontale erronée pour entrant {e.NumAssure}."));

            var minExpected = (SmigMonthlyCentimes / 26m) * Math.Max(0, e.Jours - 1);
            if (e.SalaireReel <= (long)Math.Round(minExpected, 0, MidpointRounding.AwayFromZero))
                issues.Add(ValidationIssue.Warning("B04", $"Salaire/SMIG suspect pour entrant {e.NumAssure}."));
        }

        if (b05.NbrSalaries != entrantsNoBlank.Count)
            issues.Add(ValidationIssue.Blocking("B05", "Nombre entrants B05 incohérent."));
        if (b05.TCtr != entrantsNoBlank.Sum(x => x.Ctr))
            issues.Add(ValidationIssue.Blocking("B05", "Total contrôles B05 incohérent."));

        if (b06.NbrSalaries != b03.NbrSalaries + b05.NbrSalaries)
            issues.Add(ValidationIssue.Blocking("B06", "Nombre salariés B06 incohérent."));
        if (b06.TCtr != b03.TCtr + b05.TCtr)
            issues.Add(ValidationIssue.Blocking("B06", "Total contrôles B06 incohérent."));

        return issues;
    }

    private sealed class B02LineModel
    {
        public string AffiliateNumber { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public string NumAssure { get; set; } = string.Empty;
        public string NomPrenom { get; set; } = string.Empty;
        public int Enfants { get; set; }
        public long AfPayer { get; set; }
        public long AfDeduire { get; set; }
        public long AfNet { get; set; }
        public long AfReverser { get; set; }
        public int Jours { get; set; }
        public long SalaireReel { get; set; }
        public long SalairePlaf { get; set; }
        public string Situation { get; set; } = string.Empty;
        public long Ctr { get; set; }
    }

    private sealed class B03Model
    {
        public string AffiliateNumber { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public int NbrSalaries { get; set; }
        public int TEnfants { get; set; }
        public long TAfPayer { get; set; }
        public long TAfDeduire { get; set; }
        public long TAfNet { get; set; }
        public long TNumImma { get; set; }
        public long TAfReverser { get; set; }
        public int TJours { get; set; }
        public long TSalaireReel { get; set; }
        public long TSalairePlaf { get; set; }
        public long TCtr { get; set; }
    }

    private sealed class B04LineModel
    {
        public string AffiliateNumber { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public string NumAssure { get; set; } = string.Empty;
        public string NomPrenom { get; set; } = string.Empty;
        public string NumCin { get; set; } = string.Empty;
        public int Jours { get; set; }
        public long SalaireReel { get; set; }
        public long SalairePlaf { get; set; }
        public long Ctr { get; set; }
    }

    private sealed class B05Model
    {
        public string AffiliateNumber { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public int NbrSalaries { get; set; }
        public long TNumImma { get; set; }
        public int TJours { get; set; }
        public long TSalaireReel { get; set; }
        public long TSalairePlaf { get; set; }
        public long TCtr { get; set; }
    }

    private sealed class B06Model
    {
        public string AffiliateNumber { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public int NbrSalaries { get; set; }
        public long TNumImma { get; set; }
        public int TJours { get; set; }
        public long TSalaireReel { get; set; }
        public long TSalairePlaf { get; set; }
        public long TCtr { get; set; }
    }

    private sealed class ValidationIssue
    {
        public bool IsBlocking { get; init; }
        public string Scope { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;

        public static ValidationIssue Blocking(string scope, string message) =>
            new() { IsBlocking = true, Scope = scope, Message = message };

        public static ValidationIssue Warning(string scope, string message) =>
            new() { IsBlocking = false, Scope = scope, Message = message };
    }
}
