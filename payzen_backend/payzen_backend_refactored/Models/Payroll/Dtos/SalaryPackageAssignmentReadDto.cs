namespace payzen_backend.Models.Payroll.Dtos
{
    public class SalaryPackageAssignmentReadDto
    {
        public int Id { get; set; }
        public int SalaryPackageId { get; set; }
        public string SalaryPackageName { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string EmployeeFullName { get; set; } = string.Empty;
        public int ContractId { get; set; }
        public int EmployeeSalaryId { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Snapshot of package version at assignment time (for audit/reproducibility)
        /// </summary>
        public int PackageVersion { get; set; }
    }
}
