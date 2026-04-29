namespace Payzen.Application.DTOs.Import;

public class NewEmployeeImportResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int CreatedDepartmentsCount { get; set; }
    public int CreatedJobPositionsCount { get; set; }
    public List<NewEmployeeImportSuccessDto> AddedEmployees { get; set; } = new();
    public List<NewEmployeeImportErrorDto> Errors { get; set; } = new();
}

public class NewEmployeeImportSuccessDto
{
    public int Row { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class NewEmployeeImportErrorDto
{
    public int Row { get; set; }
    public string Message { get; set; } = string.Empty;
}
