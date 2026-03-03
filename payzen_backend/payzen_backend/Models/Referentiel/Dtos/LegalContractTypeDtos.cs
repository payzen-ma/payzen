namespace payzen_backend.Models.Referentiel.Dtos
{
    public class LegalContractTypeCreateDtos
    {
        public int Id { get; set; }
        public required string Code { get; set; } // CDI, CDD, STAGE, FREELANCE
        public required string Name { get; set; } // Libellé
    }

    public class LegalContractTypeReadDtos
    {
        public int Id { get; set; }
        public required string Code { get; set; } // CDI, CDD, STAGE, FREELANCE
        public required string Name { get; set; } // Libellé
    }

    public class LegalContractTypeUpdateDtos
    {
        public required string Code { get; set; } // CDI, CDD, STAGE, FREELANCE
        public required string Name { get; set; } // Libellé
    }
}
