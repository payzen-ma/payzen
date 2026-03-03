using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.LeaveBalance.Dtos
{
public class LeaveBalanceCreateDto
    {
        [Required] public int EmployeeId { get; set; }
        [Required] public int CompanyId { get; set; }
        [Required] public int LeaveTypeId { get; set; }
        [Required] public int Year { get; set; }
        [Required, Range(1, 12)] public int Month { get; set; }

        public decimal OpeningDays { get; set; } = 0m;
        public decimal AccruedDays { get; set; } = 0m;
        public decimal UsedDays { get; set; } = 0m;
        public decimal CarryInDays { get; set; } = 0m;
        public decimal CarryOutDays { get; set; } = 0m;
        public decimal ClosingDays { get; set; } = 0m;

        public DateOnly? CarryoverExpiresOn { get; set; }
        public DateTimeOffset? LastRecalculatedAt { get; set; }
    }

    public class LeaveBalancePatchDto
    {
        public decimal? OpeningDays { get; set; }
        public decimal? AccruedDays { get; set; }
        public decimal? UsedDays { get; set; }
        public decimal? CarryInDays { get; set; }
        public decimal? CarryOutDays { get; set; }
        public decimal? ClosingDays { get; set; }

        public DateOnly? CarryoverExpiresOn { get; set; }
        public DateTimeOffset? LastRecalculatedAt { get; set; }
    }

    public class LeaveBalanceReadDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int CompanyId { get; set; }
        public int LeaveTypeId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        /// <summary>Date d'expiration du solde (fin du mois + 2 ans).</summary>
        public DateOnly? BalanceExpiresOn { get; set; }

        public decimal OpeningDays { get; set; }
        public decimal AccruedDays { get; set; }
        public decimal UsedDays { get; set; }
        public decimal CarryInDays { get; set; }
        public decimal CarryOutDays { get; set; }
        public decimal ClosingDays { get; set; }

        public DateOnly? CarryoverExpiresOn { get; set; }
        /// <summary>True si le solde est expiré (report non utilisable) à la date de référence.</summary>
        public bool IsCarryoverExpired { get; set; }
        public DateTimeOffset? LastRecalculatedAt { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
    }
}