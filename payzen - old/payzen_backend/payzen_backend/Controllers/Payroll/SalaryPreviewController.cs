using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using payzen_backend.Models.Payroll.Dtos;
using payzen_backend.Services;

namespace payzen_backend.Controllers.Payroll
{
    /// <summary>
    /// Salary preview calculation endpoints for real-time payroll previews
    /// Used by the template editor sidebar
    /// </summary>
    [Route("api/salary-preview")]
    [ApiController]
    [Authorize]
    public class SalaryPreviewController : ControllerBase
    {
        private readonly IMoroccanPayrollService _payrollService;

        public SalaryPreviewController(IMoroccanPayrollService payrollService)
        {
            _payrollService = payrollService;
        }

        /// <summary>
        /// Calculate full payroll preview for a salary package configuration
        /// Used by template editor sidebar for real-time calculations
        /// </summary>
        /// <param name="request">Salary configuration with base salary, items, CIMR config, and auto rules. Optional PayrollDate for rule/parameter resolution.</param>
        /// <returns>Complete payroll breakdown including net salary and employer costs</returns>
        [HttpPost("calculate")]
        public async Task<ActionResult<PayrollSummaryDto>> Calculate([FromBody] SalaryPreviewRequestDto request)
        {
            if (request.BaseSalary < 0)
            {
                return BadRequest(new { Message = "Base salary cannot be negative" });
            }

            try
            {
                var result = await _payrollService.CalculateFullPayrollAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Error calculating payroll preview", Error = ex.Message });
            }
        }

        /// <summary>
        /// Calculate seniority bonus only (for quick preview)
        /// </summary>
        [HttpGet("seniority-bonus")]
        public ActionResult<object> CalculateSeniorityBonus(
            [FromQuery] decimal baseSalary, 
            [FromQuery] int yearsOfService)
        {
            if (baseSalary < 0)
            {
                return BadRequest(new { Message = "Base salary cannot be negative" });
            }

            var bonus = _payrollService.CalculateSeniorityBonus(baseSalary, yearsOfService);
            
            return Ok(new
            {
                BaseSalary = baseSalary,
                YearsOfService = yearsOfService,
                SeniorityBonus = bonus,
                Rate = yearsOfService switch
                {
                    < 2 => 0m,
                    < 5 => 0.05m,
                    < 12 => 0.10m,
                    < 20 => 0.15m,
                    _ => 0.20m
                }
            });
        }

        /// <summary>
        /// Get CNSS calculation for a given gross salary
        /// </summary>
        [HttpGet("cnss")]
        public ActionResult<object> CalculateCnss([FromQuery] decimal grossSalary)
        {
            if (grossSalary < 0)
            {
                return BadRequest(new { Message = "Gross salary cannot be negative" });
            }

            var cnss = _payrollService.CalculateCnss(grossSalary);
            
            return Ok(new
            {
                GrossSalary = grossSalary,
                Employee = new
                {
                    PrestationsSociales = cnss.EmployeeContribution,
                    AMO = cnss.AmoEmployee,
                    Total = cnss.TotalEmployee
                },
                Employer = new
                {
                    PrestationsSociales = cnss.EmployerContribution,
                    AllocationsFamiliales = cnss.AllocationsFamiliales,
                    TaxeProfessionnelle = cnss.TaxeProfessionnelle,
                    AMO = cnss.AmoEmployer,
                    Total = cnss.TotalEmployer
                },
                PlafondCNSS = 6000m,
                SalairePlafonne = Math.Min(grossSalary, 6000m)
            });
        }

        /// <summary>
        /// Get IR calculation for a given net taxable income
        /// </summary>
        [HttpGet("ir")]
        public ActionResult<object> CalculateIncomeTax(
            [FromQuery] decimal netTaxableIncome, 
            [FromQuery] int dependents = 0)
        {
            if (netTaxableIncome < 0)
            {
                return BadRequest(new { Message = "Net taxable income cannot be negative" });
            }

            var ir = _payrollService.CalculateIncomeTax(netTaxableIncome, dependents);
            
            return Ok(new
            {
                NetTaxableIncome = netTaxableIncome,
                Dependents = dependents,
                Bracket = ir.Bracket,
                Rate = ir.Rate,
                GrossIR = ir.GrossIr,
                FamilyDeductions = ir.FamilyDeductions,
                NetIR = ir.NetIr
            });
        }

        /// <summary>
        /// Get professional expenses deduction for a given gross taxable income
        /// </summary>
        [HttpGet("professional-expenses")]
        public ActionResult<object> CalculateProfessionalExpenses([FromQuery] decimal grossTaxable)
        {
            if (grossTaxable < 0)
            {
                return BadRequest(new { Message = "Gross taxable income cannot be negative" });
            }

            var deduction = _payrollService.CalculateProfessionalExpenses(grossTaxable);
            var rate = grossTaxable < 6500m ? 0.35m : 0.25m;
            var cap = grossTaxable < 6500m ? 2916.67m : 2500m;
            
            return Ok(new
            {
                GrossTaxable = grossTaxable,
                Rate = rate,
                Cap = cap,
                Deduction = deduction
            });
        }
    }
}
