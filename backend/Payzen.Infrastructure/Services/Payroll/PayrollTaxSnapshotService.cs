using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Payroll;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Payroll;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Payroll;

public class PayrollTaxSnapshotService : IPayrollTaxSnapshotService
{
    private readonly AppDbContext _db;

    public PayrollTaxSnapshotService(AppDbContext db)
    {
        _db = db;
    }

    // LECTURE — toute l'année (endpoint dashboard)
    public async Task<ServiceResult<List<PayrollTaxSnapshotDto>>> GetYearSummaryAsync(
        int employeeId,
        int companyId,
        int year,
        CancellationToken ct = default)
    {
        try
        {
            Console.WriteLine("Try is called first");
            var data = await _db.PayrollTaxSnapshots
                .AsNoTracking()
                .Include(x => x.PayrollResult)
                .Where(x =>
                   x.EmployeeId == employeeId &&
                    x.CompanyId == companyId &&
                    x.Year == year)
                .OrderBy(x => x.Month)
                .Select (x => new PayrollTaxSnapshotDto
                {
                    Month =         x.Month,
                    Year =          x.Year,
                    // Mois courant - depuis PayrollResult
                    BrutMois =      x.PayrollResult.BrutImposable        ?? 0,
                    SniMois =       x.PayrollResult.NetImposable          ?? 0,
                    CnssMois =      x.PayrollResult.CnssPartSalariale    ?? 0,
                    AmoMois =       x.PayrollResult.AmoPartSalariale      ?? 0,
                    IrMois =        x.PayrollResult.ImpotRevenu            ?? 0,
                    NetMois =       x.PayrollResult.NetAPayer             ?? 0,
                    TauxIrMois =    x.PayrollResult.IrTaux             ?? 0,
                    CumulBrut =     x.CumulBrut,
                    CumulSni =      x.CumulSni,
                    CumulCnss =     x.CumulCnss,
                    CumulAmo =      x.CumulAmo,
                    CumulIr =       x.CumulIr,
                    CumulNet =      x.CumulNet,
                    TauxEffectif =  x.TauxEffectif,
                })
                .ToListAsync(ct);
            Console.WriteLine($"Data is : {data}");

            return ServiceResult<List<PayrollTaxSnapshotDto>>.Ok(data);
        }
        catch (Exception ex)
        {
            return ServiceResult<List<PayrollTaxSnapshotDto>>.Fail(ex.Message);
        }
    }


    // LECTURE — mois précis (usage interne pour construire le cumul)
    public async Task<ServiceResult<PayrollTaxSnapshot?>> GetByMonthAsync(
        int employeeId, int companyId,
        int month, int year,
        CancellationToken ct = default)
    {
        try
        {
            var snap = await _db.PayrollTaxSnapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.CompanyId == companyId &&
                    x.Month == month &&
                    x.Year == year, ct);

            return ServiceResult<PayrollTaxSnapshot?>.Ok(snap);
        }
        catch (Exception ex)
        {
            return ServiceResult<PayrollTaxSnapshot?>.Fail(ex.Message);
        }
    }

    // ÉCRITURE — construire et sauvegarder le snapshot après calcul

    public async Task<ServiceResult> BuildAndSaveAsync(
        PayrollResult pr,
        CancellationToken ct = default)
    {
        try
        {
            // 1. Récupérer le snapshot du mois précédent pour construire les cumulés
            // null si janvier ( aucun cumul avant)
            var prevResult = await GetByMonthAsync(
            pr.EmployeeId,
            pr.CompanyId,
            pr.Month - 1,
            pr.Year,
            ct);

            if (!prevResult.Success)
                return ServiceResult.Fail($"Erreur chargement cumul M-1 : {prevResult.Error}");

            var prev = prevResult.Data;

            // 2. Calculer les nouveau cumuls
            var cumulIr = (prev?.CumulIr ?? 0) + (pr.ImpotRevenu ?? 0);
            var cumulSni = (prev?.CumulSni ?? 0) + (pr.NetImposable ?? 0);

            // 3. Vérifier si un snapshot existe déjà pour ce mois
            var existing = await _db.PayrollTaxSnapshots
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == pr.EmployeeId &&
                    x.CompanyId == pr.CompanyId &&
                    x.Month == pr.Month &&
                    x.Year == pr.Year, ct);

            if (existing is null)
            {
                // INSERT
                _db.PayrollTaxSnapshots.Add(new PayrollTaxSnapshot
                {
                    PayrollResultId = pr.Id,
                    EmployeeId = pr.EmployeeId,
                    CompanyId = pr.CompanyId,
                    Month = pr.Month,
                    Year = pr.Year,
                    CumulBrut = (prev?.CumulBrut ?? 0) + (pr.BrutImposable ?? 0),
                    CumulCnss = (prev?.CumulCnss ?? 0) + (pr.CnssPartSalariale ?? 0),
                    CumulAmo = (prev?.CumulAmo ?? 0) + (pr.AmoPartSalariale ?? 0),
                    CumulSni = cumulSni,
                    CumulIr = cumulIr,
                    CumulNet = (prev?.CumulNet ?? 0) + (pr.NetAPayer ?? 0),
                    TauxEffectif = cumulSni > 0 ? Math.Round(cumulIr / cumulSni * 100, 2) : 0,
                });
            }
            else
            {  // UPDATE - recalcule si bulletin recalculé
                existing.PayrollResultId = pr.Id;
                existing.CumulBrut = (prev?.CumulBrut ?? 0) + (pr.BrutImposable ?? 0);
                existing.CumulCnss = (prev?.CumulCnss ?? 0) + (pr.CnssPartSalariale ?? 0);
                existing.CumulAmo = (prev?.CumulAmo ?? 0) + (pr.AmoPartSalariale ?? 0);
                existing.CumulSni = cumulSni;
                existing.CumulIr = cumulIr;
                existing.CumulNet = (prev?.CumulNet ?? 0) + (pr.NetAPayer ?? 0);
                existing.TauxEffectif = cumulSni > 0
                    ? Math.Round(cumulIr / cumulSni * 100, 2) : 0;
            }

            await _db.SaveChangesAsync(ct);

            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }
}
