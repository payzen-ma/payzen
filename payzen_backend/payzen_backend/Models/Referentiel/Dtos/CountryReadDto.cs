namespace payzen_backend.Models.Referentiel.Dtos
{
    public class CountryReadDto
    {
        public int Id { get; set; }
        public string CountryName { get; set; } = string.Empty;
        public string? CountryNameAr { get; set; }
        public string CountryCode { get; set; } = string.Empty;
        public string CountryPhoneCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}