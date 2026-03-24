using payzen_backend.DTOs.Dashboard;

namespace payzen_backend.Services.Dashboard
{
    public interface IDashboardEmployeeService
    {
        Task<EmployeeDashboardDataDto> GetEmployeeDashboardDataAsync(int employeeId);
    }
}
