using System;
using System.Collections.Generic;

namespace payzen_backend.Models.Dashboard.Dtos
{
    public class DashboardSummaryDto
    {
        public int TotalCompanies { get; set; }
        public int TotalEmployees { get; set; }
        public int AccountingFirmsCount { get; set; }
        public double AvgEmployeesPerCompany { get; set; }
        public List<DistributionBucketDto> EmployeeDistribution { get; set; } = new();
        public List<RecentCompanyDto> RecentCompanies { get; set; } = new();
        public DateTimeOffset AsOf { get; set; }
    }

    public class DistributionBucketDto
    {
        public string Bucket { get; set; } = string.Empty;
        public int CompaniesCount { get; set; }
        public int EmployeesCount { get; set; }
        public double Percentage { get; set; } // pourcentage sur le total des employ�s
    }

    public class RecentCompanyDto
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? CountryName { get; set; }
        public string? CityName { get; set; }
        public int EmployeesCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}