namespace payzen_backend.Models.Company.Dtos
{
    /// <summary>
    /// DTO contenant toutes les donn�es n�cessaires pour le formulaire de cr�ation/modification d'entreprise
    /// </summary>
    public class CompanyFormDataDto
    {
        public List<CountryFormDto> Countries { get; set; } = new();
        public List<CityFormDto> Cities { get; set; } = new();
    }

    public class CountryFormDto
    {
        public int Id { get; set; }
        public string CountryName { get; set; } = string.Empty;
        public string? CountryNameAr { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string CountryPhoneCode { get; set; } = string.Empty;
    }

    public class CityFormDto
    {
        public int Id { get; set; }
        public string CityName { get; set; } = string.Empty;
        public int CountryId { get; set; }
        public string? CountryName { get; set; }
    }
}
