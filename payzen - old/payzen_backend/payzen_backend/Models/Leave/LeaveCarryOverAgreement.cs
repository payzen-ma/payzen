using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Leave
{
    // Congé reporté accordé à un employé, selon article 240;
    // l'employee doit donner un accord écrit pour le report des jours de congé non utilisés
    public class LeaveCarryOverAgreement
    {
        public int Id { get; set; }

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

        public DateTimeOffset? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }
    }
}
