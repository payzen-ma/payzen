namespace Payzen.Domain.Entities.Auth;

public class LoginResponse
{
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt
    {
        get; set;
    }
    public UserInfo User { get; set; } = null!;
}

public class UserInfo
{
    public int Id
    {
        get; set;
    }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public bool IsCabinetExpert
    {
        get; set;
    }
    public int? EmployeeId
    {
        get; set;
    }
    public int? EmployeeCategoryId
    {
        get; set;
    }
    public string? Mode
    {
        get; set;
    }
    public int companyId
    {
        get; set;
    }
}
