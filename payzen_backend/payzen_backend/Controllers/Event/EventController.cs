using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;

namespace payzen_backend.Controllers.Event
{
    [ApiController]
    [Route("api/events")]
    [Authorize]
    public class EventController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EventController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Retourne tous les événements (company + employee) fusionnés et triés par date (desc).
        /// Inclut : CompanyName, EmployeeFullName (employee lié à l'événement) et CreatorFullName (nom de l'employé lié à createdBy).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            var companyEvents = await _db.CompanyEventLogs
                .AsNoTracking()
                .Select(e => new
                {
                    Source = "company",
                    Id = e.Id,
                    EventName = e.eventName,
                    OldValue = e.oldValue,
                    OldValueId = e.oldValueId,
                    NewValue = e.newValue,
                    NewValueId = e.newValueId,
                    CreatedAt = e.createdAt,
                    CreatedBy = e.createdBy,
                    CompanyId = (int?)e.companyId,
                    EmployeeId = e.employeeId,
                    CompanyName = _db.Companies
                        .Where(c => c.Id == e.companyId)
                        .Select(c => c.CompanyName)
                        .FirstOrDefault(),
                    EmployeeFullName = _db.Employees
                        .Where(emp => emp.Id == e.employeeId)
                        .Select(emp => emp.FirstName + " " + emp.LastName)
                        .FirstOrDefault(),
                    CreatorFullName = _db.Users
                        .Where(u => u.Id == e.createdBy)
                        .Select(u => u.Employee != null
                            ? u.Employee.FirstName + " " + u.Employee.LastName
                            : null)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var employeeEvents = await _db.EmployeeEventLogs
                .AsNoTracking()
                .Select(e => new
                {
                    Source = "employee",
                    Id = e.Id,
                    EventName = e.eventName,
                    OldValue = e.oldValue,
                    OldValueId = e.oldValueId,
                    NewValue = e.newValue,
                    NewValueId = e.newValueId,
                    CreatedAt = e.createdAt,
                    CreatedBy = e.createdBy,
                    CompanyId = _db.Employees
                        .Where(emp => emp.Id == e.employeeId)
                        .Select(emp => (int?)emp.CompanyId)
                        .FirstOrDefault(),
                    EmployeeId = e.employeeId,
                    CompanyName = _db.Employees
                        .Where(emp => emp.Id == e.employeeId)
                        .Select(emp => _db.Companies
                            .Where(c => c.Id == emp.CompanyId)
                            .Select(c => c.CompanyName)
                            .FirstOrDefault())
                        .FirstOrDefault(),
                    EmployeeFullName = _db.Employees
                        .Where(emp => emp.Id == e.employeeId)
                        .Select(emp => emp.FirstName + " " + emp.LastName)
                        .FirstOrDefault(),
                    CreatorFullName = _db.Users
                        .Where(u => u.Id == e.createdBy)
                        .Select(u => u.Employee != null
                            ? u.Employee.FirstName + " " + u.Employee.LastName
                            : null)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var merged = companyEvents
                .Concat(employeeEvents)
                .OrderByDescending(e => e.CreatedAt)
                .ToList();

            return Ok(new { Count = merged.Count, Items = merged });
        }
    }
}