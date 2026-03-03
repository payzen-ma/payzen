namespace payzen_backend.Models.Referentiel.Dtos
{
    public class CityReadDto
    {
        public int Id { get; set; }
        public string CityName { get; set; } = string.Empty;
        public int CountryId { get; set; }
        public string CountryName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}