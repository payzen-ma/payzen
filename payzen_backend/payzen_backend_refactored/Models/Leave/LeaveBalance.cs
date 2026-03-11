using System.ComponentModel.DataAnnotations.Schema;

namespace payzen_backend.Models.Leave
{
    public class LeaveBalance
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee.Employee Employee { get; set; } = null!;

        public int CompanyId { get; set; }
        public Company.Company Company { get; set; } = null!;

        public int LeaveTypeId { get; set; }
        public LeaveType LeaveType { get; set; } = null!;

        /// <summary>Année du solde (période mensuelle).</summary>
        public int Year { get; set; }

        /// <summary>Mois du solde (1-12). Le solde expire 2 ans après la fin de ce mois.</summary>
        public int Month { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal OpeningDays { get; set; } = 0m;

        [Column(TypeName = "decimal(10,2)")]
        public decimal AccruedDays { get; set; } = 0m;

        [Column(TypeName = "decimal(10,2)")]
        public decimal UsedDays { get; set; } = 0m;

        [Column(TypeName = "decimal(10,2)")]
        public decimal CarryInDays { get; set; } = 0m;

        [Column(TypeName = "decimal(10,2)")]
        public decimal CarryOutDays { get; set; } = 0m;

        [Column(TypeName = "decimal(10,2)")]
        public decimal ClosingDays { get; set; } = 0m;

        public DateOnly? CarryoverExpiresOn { get; set; }
        public DateTimeOffset? LastRecalculatedAt { get; set; }

        // Audit / soft delete
        public DateTimeOffset? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }


        /// <summary>Nombre d'années après la fin du mois avant expiration du solde.</summary>
        public const int BalanceValidityYears = 2;

        /// <summary>Date d'expiration du solde : dernier jour du mois (Year, Month) + 2 ans.</summary>
        public DateOnly GetBalanceExpiresOn()
        {
            var lastDayOfMonth = new DateOnly(Year, Month, DateTime.DaysInMonth(Year, Month));
            return lastDayOfMonth.AddYears(BalanceValidityYears);
        }

        /// <summary>Indique si le solde est encore valide à la date donnée.</summary>
        public bool IsValidAt(DateOnly atDate) => atDate <= GetBalanceExpiresOn();
    }
}
