using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Referentiel;

public class StateEmploymentProgram : BaseEntity
{
    public required string Code { get; set; } // NONE, ANAPEC, IDMAJ, TAHFIZ
    public required string Name { get; set; }

    // Règles légales
    public bool IsIrExempt { get; set; }
    public bool IsCnssEmployeeExempt { get; set; }
    public bool IsCnssEmployerExempt { get; set; }

    public int? MaxDurationMonths { get; set; }
    public decimal? SalaryCeiling { get; set; }
}
