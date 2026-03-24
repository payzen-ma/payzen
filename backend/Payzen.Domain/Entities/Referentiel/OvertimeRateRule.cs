using System.ComponentModel.DataAnnotations;
using Payzen.Domain.Enums;
using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Referentiel;

/// <summary>Règle de majoration pour heures supplémentaires</summary>
public class OvertimeRateRule : BaseEntity
{

    [Required][MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required][MaxLength(200)] public string NameAr { get; set; } = string.Empty;
    [Required][MaxLength(200)] public string NameFr { get; set; } = string.Empty;
    [Required][MaxLength(200)] public string NameEn { get; set; } = string.Empty;

    [MaxLength(1000)] public string? Description { get; set; }

    [Required] public OvertimeType AppliesTo { get; set; }

    [Required] public TimeRangeType TimeRangeType { get; set; } = TimeRangeType.AllDay;
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    // Bitmask jours: 1=Lundi, 2=Mardi, 4=Mer, 8=Jeu, 16=Ven, 32=Sam, 64=Dim
    public int? ApplicableDaysOfWeek { get; set; }

    [Required][Range(1.00, 10.00)]
    public decimal Multiplier { get; set; }

    [Required]
    public MultiplierCumulationStrategy CumulationStrategy { get; set; } = MultiplierCumulationStrategy.TakeMaximum;

    [Required][Range(1, 100)] public int Priority { get; set; }

    [MaxLength(50)] public string? Category { get; set; }

    [Required] public bool IsActive { get; set; } = true;
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    [Range(0.01, 24.00)] public decimal? MinimumDurationHours { get; set; }
    [Range(0.01, 24.00)] public decimal? MaximumDurationHours { get; set; }
    public bool RequiresSuperiorApproval { get; set; }

    [MaxLength(500)] public string? LegalReference { get; set; }
    [MaxLength(500)] public string? DocumentationUrl { get; set; }


    [Timestamp] public byte[]? RowVersion { get; set; }

    // Navigation
    public ICollection<Employee.EmployeeOvertime> OvertimeRecords { get; set; } = new List<Employee.EmployeeOvertime>();

    public bool IsValidOn(DateOnly date)
        => IsActive && date >= EffectiveFrom && (EffectiveTo == null || date <= EffectiveTo);

    public bool OverlapsTimeRange(TimeOnly start, TimeOnly end)
    {
        if (TimeRangeType == TimeRangeType.AllDay) return true;
        if (StartTime == null || EndTime == null) return false;
        return TimeRangeType switch
        {
            TimeRangeType.SameDay => start < EndTime && end > StartTime,
            TimeRangeType.CrossesMidnight => true,
            _ => false
        };
    }
}
