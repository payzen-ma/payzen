using System.ComponentModel.DataAnnotations;
using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Leave;

/// <summary>
/// Congé reporté accordé à un employé (Art. 240 Code du Travail).
/// L'employé doit donner un accord écrit pour le report des jours de congé non utilisés.
/// </summary>
public class LeaveCarryOverAgreement : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee.Employee Employee { get; set; } = null!;

    public int CompanyId { get; set; }
    public Company.Company Company { get; set; } = null!;

    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public int FromYear { get; set; }
    public int ToYear { get; set; }

    public DateOnly AgreementDate { get; set; }

    [MaxLength(500)]
    public string? AgreementDocRef { get; set; }
}
