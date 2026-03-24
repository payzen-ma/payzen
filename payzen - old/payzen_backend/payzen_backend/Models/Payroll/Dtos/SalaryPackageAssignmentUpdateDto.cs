using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Payroll.Dtos
{
    public class SalaryPackageAssignmentUpdateDto
    {
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
    }
}
