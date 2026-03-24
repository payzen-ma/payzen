using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using payzen_backend.DTOs.Dashboard;
using payzen_backend.Services.Dashboard;

namespace payzen_backend.Controllers.Dashboard
{
    // [Authorize(Roles = "Employee")] // Temporarily allow anonymous for testing
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardEmployeeController : ControllerBase
    {
        private readonly IDashboardEmployeeService _dashboardEmployeeService;

        public DashboardEmployeeController(IDashboardEmployeeService dashboardEmployeeService)
        {
            _dashboardEmployeeService = dashboardEmployeeService;
        }

        [HttpGet("GetEmployeeDashboardData")]
        public async Task<ActionResult<EmployeeDashboardDataDto>> GetEmployeeDashboardData()
        {
            // Usually fetched from User Claims after token authentication
            int employeeId = 1;

            var result = await _dashboardEmployeeService.GetEmployeeDashboardDataAsync(employeeId);
            return Ok(result);
        }
    }
}
