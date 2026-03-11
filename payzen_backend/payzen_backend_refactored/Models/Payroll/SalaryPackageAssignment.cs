using EmployeeContractEntity = payzen_backend.Models.Employee.EmployeeContract;
using EmployeeEntity = payzen_backend.Models.Employee.Employee;
using EmployeeSalaryEntity = payzen_backend.Models.Employee.EmployeeSalary;

namespace payzen_backend.Models.Payroll
{
    public class SalaryPackageAssignment
    {
        public int Id { get; set; }
        public int SalaryPackageId { get; set; }
        public int EmployeeId { get; set; }
        public int ContractId { get; set; }
        public int EmployeeSalaryId { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Version snapshot for audit/reproducibility
        public int PackageVersion { get; set; } // Snapshot of package version at assignment time

        // Audit fields
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public SalaryPackage? SalaryPackage { get; set; }
        public EmployeeEntity? Employee { get; set; }
        public EmployeeContractEntity? Contract { get; set; }
        public EmployeeSalaryEntity? EmployeeSalary { get; set; }
    }
}
