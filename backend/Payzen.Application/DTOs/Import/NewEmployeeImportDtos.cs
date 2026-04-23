namespace Payzen.Application.DTOs.Import;

public class NewEmployeeImportResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<NewEmployeeImportErrorDto> Errors { get; set; } = new();
}

public class NewEmployeeImportErrorDto
{
    public int Row { get; set; }
    public string Message { get; set; } = string.Empty;
}
