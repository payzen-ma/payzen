namespace payzen_backend.Services.Company.Interfaces
{
    public interface ICompanyDefaultsSeeder
    {
        Task SeedDefaultsAsync(int companyId, int userId);
    }
}
