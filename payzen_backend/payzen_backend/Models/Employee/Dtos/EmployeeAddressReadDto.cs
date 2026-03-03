namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeAddressReadDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeFullName { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string ZipCode { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public int CountryId { get; set; }
        public string CountryName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}