namespace payzen_backend.Models.Referentiel
{
    /// <summary>
    /// Genre de l'employ� (Homme, Femme, Autre)
    /// </summary>
    public class Gender
    {
        public int Id { get; set; }

        // Code unique du genre
        public required string Code { get; set; } // Ex: 'M', 'F', 'O'
        
        // Libell� affich�
        public required string NameFr { get; set; }
        public required string NameAr { get; set; }
        public required string NameEn { get; set; }

        // Activ� / d�sactiv�
        public bool IsActive { get; set; } = true;

        // Champs d'audit
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }

        // Navigation properties
        public ICollection<Employee.Employee>? Employees { get; set; }
    }
}