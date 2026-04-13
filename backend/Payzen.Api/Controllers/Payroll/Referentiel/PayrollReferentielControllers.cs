using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Application.DTOs.Referentiel;
using Payzen.Application.Interfaces;
using Payzen.Api.Extensions;

namespace Payzen.Api.Controllers.Payroll.Referentiel;

// ── ReferentielElements ───────────────────────────────────────────────────────

[ApiController]
[Route("api/payroll/referentiel-elements")]
[Authorize]
public class ReferentielElementsController : ControllerBase
    {
    private readonly IReferentielPayrollService _svc;
    private readonly IConvergenceService _convergence;

    public ReferentielElementsController(
        IReferentielPayrollService svc,
        IConvergenceService convergence)
        {
        _svc = svc;
        _convergence = convergence;
        }

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] bool? isActive)
        {
        var r = await _svc.GetElementsAsync(isActive);
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            Message = r.Error
            });
        }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
        {
        var r = await _svc.GetElementByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new
            {
            Message = r.Error
            });
        }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateReferentielElementDto dto)
        {
        var r = await _svc.CreateElementAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            Message = r.Error
            });
        }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] UpdateReferentielElementDto dto)
        {
        var r = await _svc.UpdateElementAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            Message = r.Error
            });
        }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
        {
        var r = await _svc.DeleteElementAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new
            {
            Message = r.Error
            });
        }

    [HttpPost("recalculate-convergence")]
    public async Task<ActionResult> RecalculateConvergence()
        {
        var r = await _convergence.RecalculateAllAsync();
        return r.Success ? Ok() : BadRequest(new
            {
            Message = r.Error
            });
        }

    [HttpPost("{id:int}/recalculate-convergence")]
    public async Task<ActionResult> RecalculateOne(int id)
        {
        var r = await _convergence.RecalculateElementAsync(id);
        return r.Success ? Ok() : BadRequest(new
            {
            Message = r.Error
            });
        }
    }

// ── ElementRules ──────────────────────────────────────────────────────────────

[ApiController]
[Route("api/payroll/element-rules")]
[Authorize]
public class ElementRulesController : ControllerBase
    {
    private readonly IReferentielPayrollService _svc;
    public ElementRulesController(IReferentielPayrollService svc) => _svc = svc;

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateElementRuleDto dto)
        {
        var r = await _svc.CreateRuleAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            Message = r.Error
            });
        }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] UpdateElementRuleDto dto)
        {
        var r = await _svc.UpdateRuleAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            Message = r.Error
            });
        }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
        {
        var r = await _svc.DeleteRuleAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new
            {
            Message = r.Error
            });
        }
    }

// ── LegalParameters ───────────────────────────────────────────────────────────

[ApiController]
[Route("api/payroll/legal-parameters")]
[Authorize]
public class LegalParametersController : ControllerBase
    {
    private readonly IReferentielPayrollService _svc;
    public LegalParametersController(IReferentielPayrollService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll()
        {
        var r = await _svc.GetLegalParametersAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            Message = r.Error
            });
        }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateLegalParameterDto dto)
        {
        var r = await _svc.CreateLegalParameterAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            Message = r.Error
            });
        }
    }

// ── AncienneteRateSets ────────────────────────────────────────────────────────

[ApiController]
[Route("api/payroll/anciennete-rate-sets")]
[Authorize]
public class AncienneteRateSetsController : ControllerBase
    {
    private readonly IReferentielPayrollService _svc;
    public AncienneteRateSetsController(IReferentielPayrollService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll()
        {
        var r = await _svc.GetRateSetsAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            Message = r.Error
            });
        }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateAncienneteRateSetDto dto)
        {
        var r = await _svc.CreateRateSetAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            Message = r.Error
            });
        }

    [HttpPost("customize/{companyId:int}")]
    public async Task<ActionResult> Customize(int companyId, [FromBody] CustomizeCompanyRatesDto dto)
        {
        var r = await _svc.CustomizeCompanyRatesAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            Message = r.Error
            });
        }
    }

// ── Authorities ───────────────────────────────────────────────────────────────
// Lecture seule — seed uniquement

[ApiController]
[Route("api/payroll/authorities")]
[Authorize]
public class AuthoritiesController : ControllerBase
    {
    private readonly IReferentielPayrollService _svc;
    public AuthoritiesController(IReferentielPayrollService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll()
        {
        // Authorities retournées via GetElementsAsync avec filtre — exposées pour le front
        var r = await _svc.GetElementsAsync(null);
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            Message = r.Error
            });
        }
    }

// ── ElementCategories ─────────────────────────────────────────────────────────

[ApiController]
[Route("api/payroll/element-categories")]
[Authorize]
public class ElementCategoriesController : ControllerBase
    {
    private readonly IReferentielPayrollService _svc;
    public ElementCategoriesController(IReferentielPayrollService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll()
        {
        var r = await _svc.GetElementsAsync(null);
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            Message = r.Error
            });
        }
    }

// ── EligibilityCriteria ───────────────────────────────────────────────────────

[ApiController]
[Route("api/payroll/eligibility-criteria")]
[Authorize]
public class EligibilityCriteriaController : ControllerBase
    {
    private readonly IReferentielPayrollService _svc;
    public EligibilityCriteriaController(IReferentielPayrollService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll()
        {
        var r = await _svc.GetElementsAsync(null);
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            Message = r.Error
            });
        }
    }

// ── BusinessSectors ───────────────────────────────────────────────────────────

[ApiController]
[Route("api/payroll/business-sectors")]
[Authorize]
public class BusinessSectorsController : ControllerBase
    {
    private readonly IReferentielPayrollService _svc;
    public BusinessSectorsController(IReferentielPayrollService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll()
        {
        var r = await _svc.GetElementsAsync(null);
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            Message = r.Error
            });
        }
    }
