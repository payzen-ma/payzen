namespace payzen_backend.Models.Employee.Dtos
{
    /// <summary>
    /// Represent une ligne du fichier CSV exporté depuis Sage Paie
    /// Colonnes attendues : Prenom, Nom, CIN, DateNaissance, Telephone, Email, CNSS, Salaire, DateEntree, Matricule, Genre
    /// </summary>
    public class SageImportRowDto
    {
        public string? Matricule { get; set; }
        public string? Prenom { get; set; }
        public string? Nom { get; set; }
        public string? CIN { get; set; }
        public string? DateNaissance { get; set; }
        public string? Telephone { get; set; }
        public string? Email { get; set; }
        public string? CNSS { get; set; }
        public string? Salaire { get; set; }
        public string? DateEntree { get; set; }
        public string? Genre { get; set; }
        public string? Adresse { get; set; }
        public string? SituationFamiliale { get; set; }
        public string? EmploiOccupe { get; set; }
        public string? TauxAnc { get; set; }
        public string? Anct { get; set; }
        public string? TauxHoraire { get; set; }
    }
}
