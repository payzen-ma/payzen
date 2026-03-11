using System.Threading.Tasks;
using payzen_backend.Models.Company.Dtos;

namespace payzen_backend.Services.Company.Interfaces
{
    public interface ICompanyService
    {
        Task<CompanyCreateResponseDto> CreateCompanyAsync(CompanyCreateDto dto, int currentUserId);
        Task<IEnumerable<CompanyListDto>> GetAllCompaniesAsync();
        Task<CompanyListDto?> GetCompanyByIdAsync(int id);
        Task<IEnumerable<CompanyListDto>> GetCompaniesByCityAsync(int cityId);
        Task<IEnumerable<CompanyListDto>> GetCompaniesByCountryAsync(int countryId);
        Task<IEnumerable<CompanyListDto>> SearchCompaniesAsync(string searchTerm);
        Task<CompanyFormDataDto> GetFormDataAsync();
        Task<CompanyCreateResponseDto> CreateCompanyByExpertAsync(CompanyCreateByExpertDto dto, int currentUserId);
        Task<IEnumerable<CompanyListDto>> GetCompaniesManagedByAsync(int expertCompanyId);
        Task<CompanyReadDto> PatchCompanyAsync(int id, CompanyUpdateDto dto, int currentUserId);
        Task<List<CompanyHistoryDto>> GetCompanyHistoryAsync(int companyId);
    }
}
