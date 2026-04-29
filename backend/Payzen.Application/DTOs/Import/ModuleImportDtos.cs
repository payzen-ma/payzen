namespace Payzen.Application.DTOs.Import;

public class ModuleImportResultDto
{
    public int TotalSheets { get; set; }
    public int ProcessedSheets { get; set; }
    public int FailedSheets { get; set; }
    public int SkippedSheets { get; set; }
    public List<ModuleImportSheetResultDto> Sheets { get; set; } = new();
}

public class ModuleImportSheetResultDto
{
    public string SheetName { get; set; } = string.Empty;
    public string SheetType { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int CreatedDepartmentsCount { get; set; }
    public int CreatedJobPositionsCount { get; set; }
    public List<NewEmployeeImportSuccessDto> AddedEmployees { get; set; } = new();
    public List<NewEmployeeImportErrorDto> Errors { get; set; } = new();
}
