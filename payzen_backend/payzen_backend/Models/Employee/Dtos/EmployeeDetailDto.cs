namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeDetailDto
    {
        public int Id { get; set; }
        public int? Matricule { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string CinNumber { get; set; } = string.Empty;
        public string? MaritalStatusName { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string? StatusName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CountryPhoneCode { get; set; }
        public int? GenderId { get; set; }
        public string? GenderName { get; set; }

        // Adresse
        public EmployeeAddressDto? Address { get; set; }
        
        // Informations de contrat
        public string? JobPositionName { get; set; }
        public string? ManagerName { get; set; }
        public DateTime? ContractStartDate { get; set; }
        public string? ContractTypeName { get; set; }
        public string? departments { get; set; }
        // Informations salariales
        public decimal? BaseSalary { get; set; }
        public decimal? BaseSalaryHourly { get; set; }
        public List<SalaryComponentDto> SalaryComponents { get; set; } = new();
        public decimal TotalSalary { get; set; }
        
        // Mode de paiement du salaire
        public string? SalaryPaymentMethod { get; set; }

        // Cotisations
        public string? cnss { get; set; }
        public string? cimr { get; set; }
        public decimal? cimrEmployeeRate { get; set; }
        public decimal? cimrCompanyRate { get; set; }
        public bool? hasPrivateInsurance { get; set; }
        public string? privateInsuranceNumber { get; set; }
        public decimal? privateInsuranceRate { get; set; }
        public bool disableAmo { get; set; }
        // Evenements
        public List<dynamic> Events { get; set; } = new();

        public DateTime CreatedAt { get; set; }
    }

    public class EmployeeAddressDto
    {
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string ZipCode { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
    }

    public class SalaryComponentDto
    {
        public string ComponentName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsTaxable { get; set; }
    }
}
