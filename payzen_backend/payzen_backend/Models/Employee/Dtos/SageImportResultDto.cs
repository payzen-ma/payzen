namespace payzen_backend.Models.Employee.Dtos
{
    public class SageImportResultDto
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<SageImportCreatedItemDto> Created { get; set; } = new();
        public List<SageImportUpdatedItemDto> Updated { get; set; } = new();
        public List<SageImportErrorDto> Errors { get; set; } = new();
    }

    public class SageImportCreatedItemDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int? Matricule { get; set; }
        public string Email { get; set; } = string.Empty;
    }

    public class SageImportErrorDto
    {
        public int Row { get; set; }
        public string? FullName { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SageImportUpdatedItemDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int? Matricule { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
