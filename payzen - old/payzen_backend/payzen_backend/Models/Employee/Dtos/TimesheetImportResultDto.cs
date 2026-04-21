namespace payzen_backend.Models.Employee.Dtos
{
    public class TimesheetImportResultDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string PeriodMode { get; set; } = "monthly";
        public int? Half { get; set; }
        public int TotalLines { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public int TotalSheets { get; set; } // Nombre total de feuilles lues dans le fichier

        public List<TimesheetImportErrorDto> Errors { get; set; } = new();
    }

    public class TimesheetImportErrorDto
    {
        public int Row { get; set; }
        public string? Matricule { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

