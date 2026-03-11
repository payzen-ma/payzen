using System.Collections.Generic;
using payzen_backend.Models.Referentiel.Dtos;

namespace payzen_backend.Models.Employee.Dtos
{
    /// <summary>
    /// DTO contenant toutes les donn�es n�cessaires pour le formulaire de cr�ation/modification d'employ�
    /// </summary>
    public class EmployeeFormDataDto
    {
        // Retourne maintenant les Read DTOs complets du r�f�rentiel
        public List<StatusReadDto> Statuses { get; set; } = new();
        public List<GenderReadDto> Genders { get; set; } = new();
        public List<EducationLevelReadDto> EducationLevels { get; set; } = new();
        public List<MaritalStatusReadDto> MaritalStatuses { get; set; } = new();
        public List<NationalityDto> Nationalities { get; set; } = new();

        // Donn�es g�ographiques
        public List<CountryDto> Countries { get; set; } = new();
        public List<CityDto> Cities { get; set; } = new();

        // Donn�es de l'entreprise (filtr�es par companyId)
        public List<DepartementDto> Departements { get; set; } = new();
        public List<JobPositionDto> JobPositions { get; set; } = new();
        public List<ContractTypeDto> ContractTypes { get; set; } = new();
        public List<EmployeeDto> PotentialManagers { get; set; } = new();
        public List<EmployeeCategorySimpleDto> EmployeeCategories { get; set; } = new();

    }

    // DTOs simplifi�s pour les listes (restent inchang�s)
    public class CountryDto
    {
        public int Id { get; set; }
        public string CountryName { get; set; } = string.Empty;
        public string? CountryPhoneCode { get; set; }
    }

    public class CityDto
    {
        public int Id { get; set; }
        public string CityName { get; set; } = string.Empty;
        public int CountryId { get; set; }
        public string? CountryName { get; set; }
    }

    public class DepartementDto
    {
        public int Id { get; set; }
        public string DepartementName { get; set; } = string.Empty;
        public int CompanyId { get; set; }
    }

    public class JobPositionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CompanyId { get; set; }
    }

    public class ContractTypeDto
    {
        public int Id { get; set; }
        public string ContractTypeName { get; set; } = string.Empty;
        public int CompanyId { get; set; }
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? DepartementName { get; set; }
    }

    public class NationalityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
