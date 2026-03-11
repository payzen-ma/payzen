using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeSalaryUpdateDto
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "Le salaire doit �tre sup�rieur � 0")]
        public decimal? BaseSalary { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}