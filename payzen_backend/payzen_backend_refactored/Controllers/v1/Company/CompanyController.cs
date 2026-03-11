using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using payzen_backend.Data;
using payzen_backend.Services.Company.Interfaces;
using payzen_backend.Services.Company;
using payzen_backend.Models.Company.Dtos;
using payzen_backend.Services;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace payzen_backend.Controllers.v1.Company
{
    [Route("api/companies")]
    [ApiController]
    [Authorize]
    public class CompanyController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PasswordGeneratorService _passwordGenerator;
        private readonly CompanyEventLogService _companyEventLogService;
        private readonly EmployeeEventLogService _employeeEventLogService;
        private readonly ICompanyOnboardingService _companyOnboardingService;
        private readonly ICompanyService _companyService;

        public CompanyController(
            AppDbContext db,
            PasswordGeneratorService passwordGenerator,
            CompanyEventLogService companyEventLogService,
            EmployeeEventLogService employeeEventLogService,
            ICompanyOnboardingService companyOnboardingService,
            ICompanyService companyService)
        {
            _db = db;
            _passwordGenerator = passwordGenerator;
            _companyEventLogService = companyEventLogService;
            _employeeEventLogService = employeeEventLogService;
            _companyOnboardingService = companyOnboardingService;
            _companyService = companyService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CompanyListDto>>> GetAllCompanies()
        {
            try
            {
                var companies = await _companyService.GetAllCompaniesAsync();
                return Ok(companies);
            }
            catch (ServiceException ex)
            {
                return StatusCode(ex.StatusCode, new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = "Erreur interne lors de la récupération des entreprises" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CompanyListDto>> GetCompanyById(int id)
        {
            try
            {
                var company = await _companyService.GetCompanyByIdAsync(id);
                if (company == null)
                    return NotFound(new { Message = "Entreprise non trouvée" });

                return Ok(company);
            }
            catch (ServiceException ex)
            {
                return StatusCode(ex.StatusCode, new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = "Erreur interne lors de la récupération de l'entreprise" });
            }
        }

        [HttpGet("by-city/{cityId}")]
        public async Task<ActionResult<IEnumerable<CompanyListDto>>> GetCompaniesByCity(int cityId)
        {
            try
            {
                var companies = await _companyService.GetCompaniesByCityAsync(cityId);
                return Ok(companies);
            }
            catch (ServiceException ex)
            {
                return StatusCode(ex.StatusCode, new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = "Erreur interne lors de la récupération des entreprises par ville" });
            }
        }

        [HttpGet("by-country/{countryId}")]
        public async Task<ActionResult<IEnumerable<CompanyListDto>>> GetCompaniesByCountry(int countryId)
        {
            try
            {
                var companies = await _companyService.GetCompaniesByCountryAsync(countryId);
                return Ok(companies);
            }
            catch (ServiceException ex)
            {
                return StatusCode(ex.StatusCode, new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = "Erreur interne lors de la récupération des entreprises par pays" });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<CompanyListDto>>> SearchCompanies([FromQuery] string searchTerm)
        {
            try
            {
                var companies = await _companyService.SearchCompaniesAsync(searchTerm);
                return Ok(companies);
            }
            catch (ServiceException ex)
            {
                return StatusCode(ex.StatusCode, new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = "Erreur interne lors de la recherche d'entreprises" });
            }
        }

        [HttpGet("form-data")]
        public async Task<ActionResult<CompanyFormDataDto>> GetFormData()
        {
            try
            {
                var formData = await _companyService.GetFormDataAsync();
                return Ok(formData);
            }
            catch (ServiceException ex)
            {
                return StatusCode(ex.StatusCode, new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = "Erreur interne lors de la récupération des données de formulaire" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<CompanyCreateResponseDto>> CreateCompany([FromBody] CompanyCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.GetUserId();

            try
            {
                var response = await _companyService.CreateCompanyAsync(dto, currentUserId);
                return CreatedAtAction(nameof(GetCompanyById), new { id = response.Company.Id }, response);
            }
            catch (ServiceException ex)
            {
                return StatusCode(ex.StatusCode, new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = "Erreur interne lors de la création de l'entreprise" });
            }
        }

        [HttpPost("create-by-expert")]
        public async Task<ActionResult<CompanyCreateResponseDto>> CreateCompanyByExpert([FromBody] CompanyCreateByExpertDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = User.GetUserId();

            try
            {
                var response = await _companyService.CreateCompanyByExpertAsync(dto, currentUserId);
                return CreatedAtAction(nameof(GetCompanyById), new { id = response.Company.Id }, response);
            }
            catch (ServiceException ex)
            {
                return StatusCode(ex.StatusCode, new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = "Erreur interne lors de la création de l'entreprise par expert" });
            }
        }

        [HttpGet("managedby/{expertCompanyId}")]
        public async Task<ActionResult<IEnumerable<CompanyListDto>>> GetCompaniesManagedBy(int expertCompanyId)
        {
            try
            {
                var companies = await _companyService.GetCompaniesManagedByAsync(expertCompanyId);
                return Ok(companies);
            }
            catch (ServiceException ex)
            {
                return StatusCode(ex.StatusCode, new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = "Erreur interne lors de la récupération des entreprises gérées" });
            }
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult<CompanyReadDto>> PatchCompany(int id, [FromBody] CompanyUpdateDto dto)
        {
            if (dto == null)
                return BadRequest(new { Message = "Données de mise à jour requises" });

            var currentUserId = User.GetUserId();

            try
            {
                var result = await _companyService.PatchCompanyAsync(id, dto, currentUserId);
                return Ok(result);
            }
            catch (ServiceException ex)
            {
                return StatusCode(ex.StatusCode, new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = "Erreur interne lors de la mise à jour de l'entreprise" });
            }
        }

        [HttpGet("{companyId}/history")]
        public async Task<IActionResult> GetCompanyHistory(int companyId)
        {
            try
            {
                var history = await _companyService.GetCompanyHistoryAsync(companyId);
                return Ok(history);
            }
            catch (ServiceException ex)
            {
                return StatusCode(ex.StatusCode, new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = "Erreur interne lors de la récupération de l'historique" });
            }
        }
    }
}
