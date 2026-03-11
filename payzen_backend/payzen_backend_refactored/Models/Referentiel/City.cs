namespace payzen_backend.Models.Referentiel
{
    /// <summary>
    /// Liste des villes pour localisation pr�cise
    /// </summary>
    public class City
    {
        public int Id { get; set; }
        public required string CityName { get; set; }
        public int CountryId { get; set; }

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public Country? Country { get; set; } = null!;
        public ICollection<Company.Company>? Companies { get; set; }
    }
}