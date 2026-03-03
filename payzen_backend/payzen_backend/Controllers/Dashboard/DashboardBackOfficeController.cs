using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Dashboard.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace payzen_backend.Controllers.DashboardBackOffice
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardBackOfficeController : ControllerBase
    {
        private readonly AppDbContext _db;

        public DashboardBackOfficeController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Récupère le résumé du dashboard (totaux, distribution, dernières sociétés)
        /// GET /api/dashboard/summary
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
        {
            // Récupérer les sociétés actives
            var companies = await _db.Companies
                .AsNoTracking()
                .Where(c => c.DeletedAt == null)
                .Select(c => new
                {
                    c.Id,
                    c.CompanyName,
                    c.IsCabinetExpert,
                    CountryName = c.Country != null ? c.Country.CountryName : null,
                    CityName = c.City != null ? c.City.CityName : null,
                    c.CreatedAt
                })
                .ToListAsync();

            var totalCompanies = companies.Count;

            // Regrouper les employés par companyId (uniquement employés non supprimés)
            var empGroups = await _db.Employees
                .AsNoTracking()
                .Where(e => e.DeletedAt == null && e.CompanyId != null)
                .GroupBy(e => e.CompanyId)
                .Select(g => new { CompanyId = g.Key, Count = g.Count() })
                .ToListAsync();

            var empDict = empGroups.ToDictionary(x => x.CompanyId, x => x.Count);

            var totalEmployees = empDict.Values.Sum();

            var accountingFirmsCount = companies.Count(c => c.IsCabinetExpert);

            var avgEmployeesPerCompany = totalCompanies > 0 ? Math.Round((double)totalEmployees / totalCompanies, 2) : 0.0;

            // Buckets : "1-10", "11-50", "51-200", ">200"
            var buckets = new Dictionary<string, (int companiesCount, int employeesCount)>
            {
                { "1-10", (0, 0) },
                { "11-50", (0, 0) },
                { "51-200", (0, 0) },
                { ">200", (0, 0) }
            };

            foreach (var c in companies)
            {
                var empCount = empDict.TryGetValue(c.Id, out var ct) ? ct : 0;

                string bucket;
                if (empCount <= 10) bucket = "1-10";
                else if (empCount <= 50) bucket = "11-50";
                else if (empCount <= 200) bucket = "51-200";
                else bucket = ">200";

                var current = buckets[bucket];
                current.companiesCount += 1;
                current.employeesCount += empCount;
                buckets[bucket] = current;
            }

            var employeeDistribution = buckets.Select(b =>
            {
                var employeesCount = b.Value.employeesCount;
                var percentage = totalEmployees > 0 ? Math.Round((double)employeesCount / totalEmployees * 100, 1) : 0.0;
                return new DistributionBucketDto
                {
                    Bucket = b.Key,
                    CompaniesCount = b.Value.companiesCount,
                    EmployeesCount = employeesCount,
                    Percentage = percentage
                };
            }).ToList();

            // Récupérer les sociétés récentes (5 dernières)
            var recent = companies
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new RecentCompanyDto
                {
                    Id = c.Id,
                    CompanyName = c.CompanyName,
                    CountryName = c.CountryName,
                    CityName = c.CityName,
                    EmployeesCount = empDict.TryGetValue(c.Id, out var ct) ? ct : 0,
                    CreatedAt = c.CreatedAt
                })
                .ToList();

            var result = new DashboardSummaryDto
            {
                TotalCompanies = totalCompanies,
                TotalEmployees = totalEmployees,
                AccountingFirmsCount = accountingFirmsCount,
                AvgEmployeesPerCompany = avgEmployeesPerCompany,
                EmployeeDistribution = employeeDistribution,
                RecentCompanies = recent,
                AsOf = DateTimeOffset.UtcNow
            };

            return Ok(result);
        }
    }
}