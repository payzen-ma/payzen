using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using payzen_backend.Data;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Payroll;
using payzen_backend.Services.Payroll;

namespace payzen_backend.Tests.Services
{
    /// <summary>
    /// Tests unitaires pour PayrollExportService.
    /// Base de données en mémoire — soft-delete non actif (pas de query filter sur InMemory).
    /// </summary>
    public class PayrollExportServiceTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly PayrollExportService _service;

        // Fixtures partagées
        private const int CompanyId = 1;
        private const int Year = 2025;
        private const int Month = 6;

        public PayrollExportServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
                .Options;

            _db = new AppDbContext(options);
            _service = new PayrollExportService(_db, NullLogger<PayrollExportService>.Instance);

            SeedData();
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetJournalPaie
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetJournalPaie_ReturnsRowsForValidatedBulletins()
        {
            var result = await _service.GetJournalPaie(CompanyId, Year, Month);

            result.Should().HaveCount(2);
            result.Should().AllSatisfy(r =>
            {
                r.NomPrenom.Should().NotBeNullOrWhiteSpace();
                r.TotalBrut.Should().BeGreaterThan(0);
                r.NetAPayer.Should().BeGreaterThan(0);
            });
        }

        [Fact]
        public async Task GetJournalPaie_ExcludesPendingBulletins()
        {
            // L'employé 3 a un bulletin Pending — il ne doit pas apparaître
            var result = await _service.GetJournalPaie(CompanyId, Year, Month);

            result.Should().NotContain(r => r.Matricule == "3");
        }

        [Fact]
        public async Task GetJournalPaie_ContainsDetailsPrimesForEmployeeWithPrimes()
        {
            var result = await _service.GetJournalPaie(CompanyId, Year, Month);

            var row = result.Single(r => r.Matricule == "1");
            row.DetailsPrimes.Should().Contain("Prime excellence");
        }

        [Fact]
        public async Task GetJournalPaie_ReturnsEmptyListWhenNoBulletins()
        {
            var result = await _service.GetJournalPaie(CompanyId, Year + 1, Month);

            result.Should().BeEmpty();
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetEtatCnss
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetEtatCnss_ReturnsOnlyEmployeesWithCnssNumber()
        {
            // L'employé 2 n'a pas de numéro CNSS
            var result = await _service.GetEtatCnss(CompanyId, Year, Month);

            result.Should().HaveCount(1);
            result[0].NumeroCnss.Should().Be("CNSS-001");
            result[0].NombreJoursDeclare.Should().Be(26);
        }

        [Fact]
        public async Task GetEtatCnss_SalaireBrutDeclareUsesBaseWhenAvailable()
        {
            var result = await _service.GetEtatCnss(CompanyId, Year, Month);

            // SeedData: CnssBase = 9500 pour l'employé 1
            result[0].SalaireBrutDeclare.Should().Be(9_500m);
        }

        [Fact]
        public async Task GetEtatCnss_ReturnsEmptyWhenNoValidatedBulletins()
        {
            var result = await _service.GetEtatCnss(CompanyId, Year, Month + 1);

            result.Should().BeEmpty();
        }

        // ─────────────────────────────────────────────────────────────────────
        // GetEtatIr
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetEtatIr_ReturnsRowsWithIrValues()
        {
            var result = await _service.GetEtatIr(CompanyId, Year, Month);

            result.Should().HaveCount(2);
            result.Should().AllSatisfy(r => r.IRRetenu.Should().BeGreaterThanOrEqualTo(0));
        }

        [Fact]
        public async Task GetEtatIr_OrderedByLastNameThenFirstName()
        {
            var result = await _service.GetEtatIr(CompanyId, Year, Month);

            // AMINE vient avant KARIM (tri alphabétique LastName)
            result[0].NomPrenom.Should().StartWith("AMINE");
        }

        [Fact]
        public async Task GetEtatIr_ReturnsEmptyWhenNoValidatedBulletins()
        {
            var result = await _service.GetEtatIr(99, Year, Month);

            result.Should().BeEmpty();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Seed helpers
        // ─────────────────────────────────────────────────────────────────────

        private void SeedData()
        {
            var emp1 = new Employee
            {
                Id          = 1,
                FirstName   = "Youssef",
                LastName    = "AMINE",
                CinNumber   = "AB123456",
                CnssNumber  = "CNSS-001",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Phone       = "0600000001",
                Email       = "youssef@test.com",
                CompanyId   = CompanyId,
                CreatedBy   = 1
            };

            var emp2 = new Employee
            {
                Id          = 2,
                FirstName   = "Hassan",
                LastName    = "KARIM",
                CinNumber   = "CD789012",
                CnssNumber  = null, // Pas de CNSS → absent de l'état CNSS
                DateOfBirth = new DateOnly(1985, 5, 15),
                Phone       = "0600000002",
                Email       = "hassan@test.com",
                CompanyId   = CompanyId,
                CreatedBy   = 1
            };

            var emp3 = new Employee
            {
                Id          = 3,
                FirstName   = "Salma",
                LastName    = "BERRADA",
                CinNumber   = "EF345678",
                CnssNumber  = "CNSS-003",
                DateOfBirth = new DateOnly(1995, 3, 20),
                Phone       = "0600000003",
                Email       = "salma@test.com",
                CompanyId   = CompanyId,
                CreatedBy   = 1
            };

            _db.Employees.AddRange(emp1, emp2, emp3);

            // Bulletin OK — employé 1 (avec prime)
            var res1 = new PayrollResult
            {
                Id          = 1,
                EmployeeId  = 1,
                CompanyId   = CompanyId,
                Month       = Month,
                Year        = Year,
                Status      = PayrollResultStatus.OK,
                SalaireBase = 10_000m,
                TotalBrut   = 12_000m,
                CnssBase    = 9_500m,
                BrutImposable = 11_000m,
                ImpotRevenu = 820m,
                TotalCotisationsSalariales = 680m,
                NetAPayer   = 10_500m,
                CreatedBy   = 1,
                Primes      = new List<PayrollResultPrime>
                {
                    new() { Label = "Prime excellence", Montant = 2_000m, Ordre = 1, IsTaxable = true }
                }
            };

            // Bulletin OK — employé 2 (sans CNSS)
            var res2 = new PayrollResult
            {
                Id          = 2,
                EmployeeId  = 2,
                CompanyId   = CompanyId,
                Month       = Month,
                Year        = Year,
                Status      = PayrollResultStatus.OK,
                SalaireBase = 8_000m,
                TotalBrut   = 8_000m,
                BrutImposable = 7_500m,
                ImpotRevenu = 450m,
                TotalCotisationsSalariales = 550m,
                NetAPayer   = 7_000m,
                CreatedBy   = 1
            };

            // Bulletin Pending — ne doit pas apparaître dans les exports
            var res3 = new PayrollResult
            {
                Id          = 3,
                EmployeeId  = 3,
                CompanyId   = CompanyId,
                Month       = Month,
                Year        = Year,
                Status      = PayrollResultStatus.Pending,
                SalaireBase = 6_000m,
                TotalBrut   = 6_000m,
                CreatedBy   = 1
            };

            _db.PayrollResults.AddRange(res1, res2, res3);
            _db.SaveChanges();
        }

        public void Dispose() => _db.Dispose();
    }
}
