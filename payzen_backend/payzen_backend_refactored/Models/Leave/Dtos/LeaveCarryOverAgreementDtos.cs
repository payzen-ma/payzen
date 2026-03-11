using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Leave.Dtos
{
    public class LeaveCarryOverAgreementCreateDto
    {
        [Required] public int EmployeeId { get; set; }
        [Required] public int CompanyId { get; set; }
        [Required] public int LeaveTypeId { get; set; }

        [Required] public int FromYear { get; set; }
        [Required] public int ToYear { get; set; }

        [Required] public DateOnly AgreementDate { get; set; }

        [MaxLength(500)]
        public string? AgreementDocRef { get; set; }
    }

    public class LeaveCarryOverAgreementPatchDto
    {
        public int? FromYear { get; set; }
        public int? ToYear { get; set; }
        public DateOnly? AgreementDate { get; set; }

        [MaxLength(500)]
        public string? AgreementDocRef { get; set; }
    }

    public class LeaveCarryOverAgreementReadDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int CompanyId { get; set; }
        public int LeaveTypeId { get; set; }

        public int FromYear { get; set; }
        public int ToYear { get; set; }
        public DateOnly AgreementDate { get; set; }
        public string? AgreementDocRef { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }
    }
}