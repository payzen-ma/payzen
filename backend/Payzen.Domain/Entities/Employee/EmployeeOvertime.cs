using Payzen.Domain.Common;
using System.ComponentModel.DataAnnotations;
using Payzen.Domain.Enums;

namespace Payzen.Domain.Entities.Employee;

public class EmployeeOvertime : BaseEntity
{

    [Required]
    public int EmployeeId
    {
        get; set;
    }
    [Required]
    public OvertimeType OverTimeType
    {
        get; set;
    }
    [Required]
    public OvertimeEntryMode EntryMode
    {
        get; set;
    }

    public int? HolidayId
    {
        get; set;
    }
    public Company.Holiday? Holiday
    {
        get; set;
    }

    [Required]
    public DateOnly OvertimeDate
    {
        get; set;
    }

    public TimeOnly? StartTime
    {
        get; set;
    }
    public TimeOnly? EndTime
    {
        get; set;
    }
    public bool CrossesMidnight
    {
        get; set;
    }

    [Required]
    [Range(0.01, 24.00)]
    public decimal DurationInHours
    {
        get; set;
    }

    public decimal? StandardDayHours
    {
        get; set;
    }

    public int? RateRuleId
    {
        get; set;
    }

    [MaxLength(50)]
    public string? RateRuleCodeApplied
    {
        get; set;
    }
    [MaxLength(200)]
    public string? RateRuleNameApplied
    {
        get; set;
    }

    [Required]
    [Range(1.00, 10.00)]
    public decimal RateMultiplierApplied { get; set; } = 1.00m;

    [MaxLength(1000)]
    public string? MultiplierCalculationDetails
    {
        get; set;
    }

    // Split batch
    public Guid? SplitBatchId
    {
        get; set;
    }
    public int? SplitSequence
    {
        get; set;
    }
    public int? SplitTotalSegments
    {
        get; set;
    }

    [Required] public OvertimeStatus Status { get; set; } = OvertimeStatus.Draft;

    [MaxLength(500)]
    public string? EmployeeComment
    {
        get; set;
    }
    [MaxLength(500)]
    public string? ManagerComment
    {
        get; set;
    }
    public int? ApprovedBy
    {
        get; set;
    }
    public DateTimeOffset? ApprovedAt
    {
        get; set;
    }

    // Intégration paie
    public bool IsProcessedInPayroll
    {
        get; set;
    }
    public int? PayrollBatchId
    {
        get; set;
    }
    public DateTimeOffset? ProcessedInPayrollAt
    {
        get; set;
    }

    [Timestamp]
    public byte[]? RowVersion
    {
        get; set;
    }

    // Navigation
    public Employee Employee { get; set; } = null!;
    public Referentiel.OvertimeRateRule? RateRule
    {
        get; set;
    }

    public bool IsSplit() => SplitBatchId.HasValue;
    /// <summary>Parité ancien contrôleur : seul le brouillon est modifiable.</summary>
    public bool CanBeModified() => Status == OvertimeStatus.Draft;
    /// <summary>Parité ancien contrôleur : suppression refusée uniquement si déjà en paie.</summary>
    public bool CanBeDeleted() => !IsProcessedInPayroll;
}
