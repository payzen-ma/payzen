using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.Interfaces;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Payroll;

/// <summary>
/// Analyse de convergence CNSS vs DGI sur les règles d'éléments du référentiel payroll.
/// Compare les règles de deux autorités (CNSS et DGI) pour chaque élément
/// et met à jour le flag HasConvergence sur l'élément.
/// </summary>
public class ConvergenceAnalysisService : IConvergenceService
{
    private readonly AppDbContext _db;

    public ConvergenceAnalysisService(AppDbContext db) => _db = db;

    public async Task<ServiceResult<bool>> RecalculateAllAsync(CancellationToken ct = default)
    {
        var elements = await _db.ReferentielElements
            .Include(e => e.Rules)
            .Where(e => e.DeletedAt == null)
            .ToListAsync(ct);

        foreach (var element in elements)
        {
            element.HasConvergence = CheckConvergence(element.Rules);
        }

        await _db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<bool>> RecalculateElementAsync(int elementId, CancellationToken ct = default)
    {
        var element = await _db.ReferentielElements
            .Include(e => e.Rules)
            .FirstOrDefaultAsync(e => e.Id == elementId && e.DeletedAt == null, ct);

        if (element == null)
            return ServiceResult<bool>.Fail($"Élément {elementId} introuvable.");

        element.HasConvergence = CheckConvergence(element.Rules);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Ok(true);
    }

    private static bool CheckConvergence(IEnumerable<Domain.Entities.Payroll.Referentiel.ElementRule> rules)
    {
        var activeRules = rules.Where(r => r.DeletedAt == null).ToList();
        if (!activeRules.Any()) return true;

        // Convergence = toutes les règles actives ont le même type d'exonération
        var firstType = activeRules.First().ExemptionType;
        return activeRules.All(r => r.ExemptionType == firstType);
    }
}
