using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Payroll;
using payzen_backend.Models.Payroll.Dtos;

namespace payzen_backend.Controllers.SystemData
{
    [Route("api/salary-packages")]
    [ApiController]
    [Authorize]
    public class SalaryPackagesController : ControllerBase
    {
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "draft",
            "published",
            "deprecated"
        };
        private static readonly HashSet<string> AllowedScopes = new(StringComparer.OrdinalIgnoreCase)
        {
            "all",
            "official",
            "company"
        };

        private readonly AppDbContext _db;

        public SalaryPackagesController(AppDbContext db) => _db = db;

        /// <summary>
        /// Get salary packages with optional scope filtering.
        /// Scope values: all (default), official, company
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalaryPackageReadDto>>> GetAll(
            [FromQuery] int? companyId = null,
            [FromQuery] string? status = null,
            [FromQuery] string? scope = null)
        {
            if (companyId.HasValue && companyId.Value <= 0)
                return BadRequest(new { Message = "Company id must be valid" });

            var normalizedScope = NormalizeScope(scope, companyId);
            if (!IsScopeValid(normalizedScope))
                return BadRequest(new { Message = "Scope must be one of: all, official, company" });
            if (normalizedScope == "company" && !companyId.HasValue)
                return BadRequest(new { Message = "companyId is required when scope is company" });

            var query = _db.SalaryPackages
                .AsNoTracking()
                .Where(sp => sp.DeletedAt == null);

            if (normalizedScope == "official")
            {
                query = query.Where(sp => sp.CompanyId == null && sp.TemplateType == "OFFICIAL");
            }
            else if (normalizedScope == "company")
            {
                query = query.Where(sp => sp.CompanyId == companyId && sp.TemplateType == "COMPANY");
            }
            else if (companyId.HasValue)
            {
                // Backward-compatible mixed view when scope=all and companyId is provided.
                query = query.Where(sp =>
                    (sp.CompanyId == null && sp.TemplateType == "OFFICIAL") ||
                    (sp.CompanyId == companyId.Value && sp.TemplateType == "COMPANY"));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = NormalizeStatus(status);
                if (IsStatusValid(normalizedStatus))
                    query = query.Where(sp => sp.Status == normalizedStatus);
            }

            var packages = await query
                .Include(sp => sp.Company)
                .Include(sp => sp.BusinessSector)
                .Include(sp => sp.Items!.Where(i => i.DeletedAt == null))
                    .ThenInclude(i => i.PayComponent)
                .Include(sp => sp.Items!.Where(i => i.DeletedAt == null))
                    .ThenInclude(i => i.ReferentielElement)
                .Include(sp => sp.SourceTemplate)
                .OrderBy(sp => sp.Name)
                .ThenByDescending(sp => sp.Version)
                .ToListAsync();

            var result = packages.Select(MapToReadDto);

            return Ok(result);
        }

        /// <summary>
        /// Get global templates only (for client selection)
        /// </summary>
        [HttpGet("templates")]
        public async Task<ActionResult<IEnumerable<SalaryPackageReadDto>>> GetGlobalTemplates()
        {
            var templates = await _db.SalaryPackages
                .AsNoTracking()
                .Where(sp => sp.DeletedAt == null && sp.CompanyId == null && sp.TemplateType == "OFFICIAL" && sp.Status == "published")
                .Include(sp => sp.BusinessSector)
                .Include(sp => sp.Items!.Where(i => i.DeletedAt == null))
                    .ThenInclude(i => i.PayComponent)
                .Include(sp => sp.Items!.Where(i => i.DeletedAt == null))
                    .ThenInclude(i => i.ReferentielElement)
                .OrderBy(sp => sp.Category)
                .ThenBy(sp => sp.Name)
                .ToListAsync();

            var result = templates.Select(MapToReadDto);

            return Ok(result);
        }

        /// <summary>
        /// Get a salary package by id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SalaryPackageReadDto>> GetById(int id)
        {
            var package = await _db.SalaryPackages
                .AsNoTracking()
                .Include(sp => sp.Company)
                .Include(sp => sp.BusinessSector)
                .Include(sp => sp.Items!.Where(i => i.DeletedAt == null))
                    .ThenInclude(i => i.PayComponent)
                .Include(sp => sp.Items!.Where(i => i.DeletedAt == null))
                    .ThenInclude(i => i.ReferentielElement)
                .Include(sp => sp.SourceTemplate)
                .FirstOrDefaultAsync(sp => sp.Id == id && sp.DeletedAt == null);

            if (package == null)
                return NotFound(new { Message = "Salary package not found" });

            return Ok(MapToReadDto(package));
        }

        /// <summary>
        /// Create a salary package template
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SalaryPackageReadDto>> Create([FromBody] SalaryPackageCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Invalid data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            var normalizedStatus = NormalizeStatus(dto.Status);
            if (!IsStatusValid(normalizedStatus))
                return BadRequest(new { Message = "Status must be draft, published, or deprecated" });

            var name = dto.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { Message = "Name is required" });

            var category = dto.Category.Trim();
            if (string.IsNullOrWhiteSpace(category))
                return BadRequest(new { Message = "Category is required" });

            if (dto.CompanyId.HasValue)
            {
                var companyExists = await _db.Companies
                    .AnyAsync(c => c.Id == dto.CompanyId.Value && c.DeletedAt == null);
                if (!companyExists)
                    return NotFound(new { Message = "Company not found" });
            }

            // Validate business sector exists and is active
            /*var sectorExists = await _db.BusinessSectors
                .AnyAsync(bs => bs.Id == dto.BusinessSectorId && bs.DeletedAt == null);

            if (!sectorExists)
            {
                return BadRequest(new { Message = "Secteur d'activité invalide" });
            }*/

            var nameExists = await _db.SalaryPackages
                .AnyAsync(sp => sp.Name == name && sp.CompanyId == dto.CompanyId && sp.DeletedAt == null);

            if (nameExists)
                return Conflict(new { Message = "A salary package with this name already exists" });

            // Validate CIMR rate if provided
            if (dto.CimrRate.HasValue && (dto.CimrRate.Value < 0 || dto.CimrRate.Value > 0.12m))
                return BadRequest(new { Message = "CIMR rate must be between 0 and 12%" });

            var items = new List<SalaryPackageItem>();
            var nextSortOrder = 1;
            foreach (var item in dto.Items ?? new List<SalaryPackageItemWriteDto>())
            {
                var label = item.Label?.Trim();
                if (string.IsNullOrWhiteSpace(label))
                    return BadRequest(new { Message = "Item label is required" });

                if (item.DefaultValue < 0)
                    return BadRequest(new { Message = "Item default value must be positive" });

                // Validate PayComponentId if provided
                if (item.PayComponentId.HasValue)
                {
                    var componentExists = await _db.PayComponents
                        .AnyAsync(pc => pc.Id == item.PayComponentId.Value && pc.DeletedAt == null && pc.IsActive);
                    if (!componentExists)
                        return NotFound(new { Message = $"Pay component with id {item.PayComponentId.Value} not found" });
                }

                // Validate ReferentielElementId if provided
                if (item.ReferentielElementId.HasValue)
                {
                    var elementExists = await _db.ReferentielElements
                        .AnyAsync(e => e.Id == item.ReferentielElementId.Value && e.DeletedAt == null && e.IsActive);
                    if (!elementExists)
                        return BadRequest(new { Message = $"Referentiel element with id {item.ReferentielElementId.Value} not found or inactive" });
                }

                var sortOrder = item.SortOrder ?? nextSortOrder++;
                items.Add(new SalaryPackageItem
                {
                    PayComponentId = item.PayComponentId,
                    ReferentielElementId = item.ReferentielElementId,
                    Label = label,
                    DefaultValue = item.DefaultValue,
                    SortOrder = sortOrder,
                    Type = item.Type,
                    IsTaxable = item.IsTaxable,
                    IsSocial = item.IsSocial,
                    IsCIMR = item.IsCIMR,
                    IsVariable = item.IsVariable,
                    ExemptionLimit = item.ExemptionLimit,
                    CreatedAt = now,
                    CreatedBy = userId
                });
            }

            // Serialize AutoRules to JSON
            string? autoRulesJson = null;
            if (dto.AutoRules != null)
            {
                autoRulesJson = JsonSerializer.Serialize(dto.AutoRules, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
            }

            // Serialize CimrConfig to JSON
            string? cimrConfigJson = null;
            if (dto.CimrConfig != null)
            {
                cimrConfigJson = JsonSerializer.Serialize(dto.CimrConfig, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
            }

            var package = new SalaryPackage
            {
                Name = name,
                Category = category,
                Description = dto.Description?.Trim(),
                BaseSalary = dto.BaseSalary,
                Status = normalizedStatus,
                CompanyId = dto.CompanyId,
                BusinessSectorId = dto.BusinessSectorId,
                // New fields
                TemplateType = ResolveTemplateTypeForCompany(dto.CompanyId),
                RegulationVersion = dto.RegulationVersion ?? "MA_2025",
                AutoRulesJson = autoRulesJson,
                CimrConfigJson = cimrConfigJson,
                // Legacy fields
                CimrRate = dto.CimrRate,
                HasPrivateInsurance = dto.HasPrivateInsurance,
                Version = 1,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
                IsLocked = false,
                CreatedAt = now,
                CreatedBy = userId,
                Items = items
            };

            _db.SalaryPackages.Add(package);
            await _db.SaveChangesAsync();

            var created = await _db.SalaryPackages
                .AsNoTracking()
                .Include(sp => sp.Company)
                .Include(sp => sp.BusinessSector)
                .Include(sp => sp.Items!)
                    .ThenInclude(i => i.ReferentielElement)
                .Include(sp => sp.SourceTemplate)
                .FirstAsync(sp => sp.Id == package.Id);

            return CreatedAtAction(nameof(GetById), new { id = package.Id }, MapToReadDto(created));
        }

        /// <summary>
        /// Clone a global template into a tenant-owned package
        /// </summary>
        [HttpPost("{id}/clone")]
        public async Task<ActionResult<SalaryPackageReadDto>> Clone(int id, [FromBody] SalaryPackageCloneDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Invalid data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var sourcePackage = await _db.SalaryPackages
                .AsNoTracking()
                .Include(sp => sp.Items!.Where(i => i.DeletedAt == null))
                .FirstOrDefaultAsync(sp => sp.Id == id && sp.DeletedAt == null);

            if (sourcePackage == null)
                return NotFound(new { Message = "Source salary package not found" });

            // Only published templates can be cloned
            if (!string.Equals(sourcePackage.Status, "published", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { Message = "Only published templates can be cloned" });

            var companyExists = await _db.Companies
                .AnyAsync(c => c.Id == dto.CompanyId && c.DeletedAt == null);
            if (!companyExists)
                return NotFound(new { Message = "Company not found" });

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            var clonedName = dto.Name?.Trim() ?? sourcePackage.Name;

            // Check for duplicate name in target company
            var nameExists = await _db.SalaryPackages
                .AnyAsync(sp => sp.Name == clonedName && sp.CompanyId == dto.CompanyId && sp.DeletedAt == null);

            if (nameExists)
                return Conflict(new { Message = "A salary package with this name already exists in the target company" });

            // Clone items
            var clonedItems = sourcePackage.Items?.Select(item => new SalaryPackageItem
            {
                PayComponentId = item.PayComponentId,
                ReferentielElementId = item.ReferentielElementId,
                Label = item.Label,
                DefaultValue = item.DefaultValue,
                SortOrder = item.SortOrder,
                Type = item.Type,
                IsTaxable = item.IsTaxable,
                IsSocial = item.IsSocial,
                IsCIMR = item.IsCIMR,
                IsVariable = item.IsVariable,
                ExemptionLimit = item.ExemptionLimit,
                CreatedAt = now,
                CreatedBy = userId
            }).ToList() ?? new List<SalaryPackageItem>();

            var clonedPackage = new SalaryPackage
            {
                Name = clonedName,
                Category = sourcePackage.Category,
                Description = sourcePackage.Description,
                BaseSalary = sourcePackage.BaseSalary,
                Status = "draft", // Cloned packages start as draft
                CompanyId = dto.CompanyId,
                BusinessSectorId = sourcePackage.BusinessSectorId,
                // New fields - template becomes COMPANY when cloned to a company
                TemplateType = "COMPANY",
                RegulationVersion = sourcePackage.RegulationVersion,
                AutoRulesJson = sourcePackage.AutoRulesJson,
                CimrConfigJson = sourcePackage.CimrConfigJson,
                // Origin tracking
                OriginType = "COPIED_FROM_OFFICIAL",
                SourceTemplateNameSnapshot = sourcePackage.Name,
                CopiedAt = DateTime.UtcNow,
                // Legacy fields
                CimrRate = sourcePackage.CimrRate,
                HasPrivateInsurance = sourcePackage.HasPrivateInsurance,
                Version = 1,
                SourceTemplateId = sourcePackage.Id,
                SourceTemplateVersion = sourcePackage.Version,
                ValidFrom = dto.ValidFrom,
                ValidTo = null,
                IsLocked = false,
                CreatedAt = now,
                CreatedBy = userId,
                Items = clonedItems
            };

            _db.SalaryPackages.Add(clonedPackage);
            await _db.SaveChangesAsync();

            var created = await _db.SalaryPackages
                .AsNoTracking()
                .Include(sp => sp.Company)
                .Include(sp => sp.BusinessSector)
                .Include(sp => sp.Items!)
                    .ThenInclude(i => i.ReferentielElement)
                .Include(sp => sp.SourceTemplate)
                .FirstAsync(sp => sp.Id == clonedPackage.Id);

            return CreatedAtAction(nameof(GetById), new { id = clonedPackage.Id }, MapToReadDto(created));
        }

        /// <summary>
        /// Create a new version of an existing package (for locked packages)
        /// </summary>
        [HttpPost("{id}/new-version")]
        public async Task<ActionResult<SalaryPackageReadDto>> CreateNewVersion(int id)
        {
            var sourcePackage = await _db.SalaryPackages
                .Include(sp => sp.Items!.Where(i => i.DeletedAt == null))
                .FirstOrDefaultAsync(sp => sp.Id == id && sp.DeletedAt == null);

            if (sourcePackage == null)
                return NotFound(new { Message = "Salary package not found" });

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            // Get the next version number for this package lineage
            var maxVersion = await _db.SalaryPackages
                .Where(sp => sp.Name == sourcePackage.Name && sp.CompanyId == sourcePackage.CompanyId && sp.DeletedAt == null)
                .MaxAsync(sp => sp.Version);

            // Clone items for the new version
            var clonedItems = sourcePackage.Items?.Select(item => new SalaryPackageItem
            {
                PayComponentId = item.PayComponentId,
                ReferentielElementId = item.ReferentielElementId,
                Label = item.Label,
                DefaultValue = item.DefaultValue,
                SortOrder = item.SortOrder,
                Type = item.Type,
                IsTaxable = item.IsTaxable,
                IsSocial = item.IsSocial,
                IsCIMR = item.IsCIMR,
                IsVariable = item.IsVariable,
                ExemptionLimit = item.ExemptionLimit,
                CreatedAt = now,
                CreatedBy = userId
            }).ToList() ?? new List<SalaryPackageItem>();

            var newVersion = new SalaryPackage
            {
                Name = sourcePackage.Name,
                Category = sourcePackage.Category,
                Description = sourcePackage.Description,
                BaseSalary = sourcePackage.BaseSalary,
                Status = "draft", // New versions start as draft
                CompanyId = sourcePackage.CompanyId,
                BusinessSectorId = sourcePackage.BusinessSectorId,
                TemplateType = ResolveTemplateTypeForCompany(sourcePackage.CompanyId),
                RegulationVersion = sourcePackage.RegulationVersion,
                AutoRulesJson = sourcePackage.AutoRulesJson,
                CimrConfigJson = sourcePackage.CimrConfigJson,
                OriginType = sourcePackage.OriginType,
                SourceTemplateNameSnapshot = sourcePackage.SourceTemplateNameSnapshot,
                CopiedAt = sourcePackage.CopiedAt,
                CimrRate = sourcePackage.CimrRate,
                HasPrivateInsurance = sourcePackage.HasPrivateInsurance,
                Version = maxVersion + 1,
                SourceTemplateId = sourcePackage.SourceTemplateId,
                SourceTemplateVersion = sourcePackage.SourceTemplateVersion,
                ValidFrom = null, // To be set when publishing
                ValidTo = null,
                IsLocked = false,
                CreatedAt = now,
                CreatedBy = userId,
                Items = clonedItems
            };

            // Mark the source package as deprecated if it's published
            if (string.Equals(sourcePackage.Status, "published", StringComparison.OrdinalIgnoreCase))
            {
                sourcePackage.Status = "deprecated";
                sourcePackage.ValidTo = DateTime.UtcNow;
                sourcePackage.ModifiedAt = now;
                sourcePackage.ModifiedBy = userId;
            }

            _db.SalaryPackages.Add(newVersion);
            await _db.SaveChangesAsync();

            var created = await _db.SalaryPackages
                .AsNoTracking()
                .Include(sp => sp.Company)
                .Include(sp => sp.BusinessSector)
                .Include(sp => sp.Items!)
                    .ThenInclude(i => i.ReferentielElement)
                .Include(sp => sp.SourceTemplate)
                .FirstAsync(sp => sp.Id == newVersion.Id);

            return CreatedAtAction(nameof(GetById), new { id = newVersion.Id }, MapToReadDto(created));
        }

        /// <summary>
        /// Publish a draft template (transitions status from 'draft' to 'published')
        /// Published templates become read-only and available to clients
        /// </summary>
        [HttpPost("{id}/publish")]
        public async Task<ActionResult<SalaryPackageReadDto>> Publish(int id)
        {
            var package = await _db.SalaryPackages
                .Include(sp => sp.Items)
                .FirstOrDefaultAsync(sp => sp.Id == id && sp.DeletedAt == null);

            if (package == null)
                return NotFound(new { Message = "Salary package not found" });

            // Only draft packages can be published
            if (!string.Equals(package.Status, "draft", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { Message = "Only draft templates can be published" });

            // Validate required fields before publishing
            if (string.IsNullOrWhiteSpace(package.Name))
                return BadRequest(new { Message = "Template name is required for publishing" });
            if (package.BaseSalary <= 0)
                return BadRequest(new { Message = "Base salary must be greater than 0 for publishing" });

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            package.Status = "published";
            package.ValidFrom ??= DateTime.UtcNow;
            package.ModifiedAt = now;
            package.ModifiedBy = userId;

            await _db.SaveChangesAsync();

            var updated = await _db.SalaryPackages
                .AsNoTracking()
                .Include(sp => sp.Company)
                .Include(sp => sp.BusinessSector)
                .Include(sp => sp.Items!.Where(i => i.DeletedAt == null))
                    .ThenInclude(i => i.PayComponent)
                .Include(sp => sp.Items!.Where(i => i.DeletedAt == null))
                    .ThenInclude(i => i.ReferentielElement)
                .Include(sp => sp.SourceTemplate)
                .FirstAsync(sp => sp.Id == id);

            return Ok(MapToReadDto(updated));
        }

        /// <summary>
        /// Deprecate a published template (transitions status from 'published' to 'deprecated')
        /// Deprecated templates are no longer available for new assignments
        /// </summary>
        [HttpPost("{id}/deprecate")]
        public async Task<ActionResult<SalaryPackageReadDto>> Deprecate(int id)
        {
            var package = await _db.SalaryPackages
                .Include(sp => sp.Items)
                .FirstOrDefaultAsync(sp => sp.Id == id && sp.DeletedAt == null);

            if (package == null)
                return NotFound(new { Message = "Salary package not found" });

            // Only published packages can be deprecated
            if (!string.Equals(package.Status, "published", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { Message = "Only published templates can be deprecated" });

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            package.Status = "deprecated";
            package.ValidTo = DateTime.UtcNow;
            package.ModifiedAt = now;
            package.ModifiedBy = userId;

            await _db.SaveChangesAsync();

            var updated = await _db.SalaryPackages
                .AsNoTracking()
                .Include(sp => sp.Company)
                .Include(sp => sp.BusinessSector)
                .Include(sp => sp.Items!.Where(i => i.DeletedAt == null))
                    .ThenInclude(i => i.PayComponent)
                .Include(sp => sp.Items!.Where(i => i.DeletedAt == null))
                    .ThenInclude(i => i.ReferentielElement)
                .Include(sp => sp.SourceTemplate)
                .FirstAsync(sp => sp.Id == id);

            return Ok(MapToReadDto(updated));
        }

        /// <summary>
        /// Duplicate an existing template (creates a new draft copy)
        /// </summary>
        [HttpPost("{id}/duplicate")]
        public async Task<ActionResult<SalaryPackageReadDto>> Duplicate(int id, [FromBody] SalaryPackageDuplicateDto? dto)
        {
            var sourcePackage = await _db.SalaryPackages
                .AsNoTracking()
                .Include(sp => sp.Items!.Where(i => i.DeletedAt == null))
                .FirstOrDefaultAsync(sp => sp.Id == id && sp.DeletedAt == null);

            if (sourcePackage == null)
                return NotFound(new { Message = "Source salary package not found" });

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            // Generate duplicate name
            var baseName = dto?.Name?.Trim();
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = $"{sourcePackage.Name} (Copy)";

            // Check for duplicate name in same scope
            var nameExists = await _db.SalaryPackages
                .AnyAsync(sp => sp.Name == baseName && sp.CompanyId == sourcePackage.CompanyId && sp.DeletedAt == null);

            if (nameExists)
            {
                // Add number suffix
                var counter = 2;
                var originalBaseName = baseName.EndsWith(" (Copy)") ? baseName[..^7] : baseName;
                while (nameExists)
                {
                    baseName = $"{originalBaseName} (Copy {counter})";
                    nameExists = await _db.SalaryPackages
                        .AnyAsync(sp => sp.Name == baseName && sp.CompanyId == sourcePackage.CompanyId && sp.DeletedAt == null);
                    counter++;
                }
            }

            // Clone items
            var clonedItems = sourcePackage.Items?.Select(item => new SalaryPackageItem
            {
                PayComponentId = item.PayComponentId,
                ReferentielElementId = item.ReferentielElementId,
                Label = item.Label,
                DefaultValue = item.DefaultValue,
                SortOrder = item.SortOrder,
                Type = item.Type,
                IsTaxable = item.IsTaxable,
                IsSocial = item.IsSocial,
                IsCIMR = item.IsCIMR,
                IsVariable = item.IsVariable,
                ExemptionLimit = item.ExemptionLimit,
                CreatedAt = now,
                CreatedBy = userId
            }).ToList() ?? new List<SalaryPackageItem>();

            var duplicatedPackage = new SalaryPackage
            {
                Name = baseName,
                Category = sourcePackage.Category,
                Description = sourcePackage.Description,
                BaseSalary = sourcePackage.BaseSalary,
                Status = "draft", // Duplicated packages start as draft
                CompanyId = sourcePackage.CompanyId,
                BusinessSectorId = sourcePackage.BusinessSectorId,
                TemplateType = sourcePackage.TemplateType,
                RegulationVersion = sourcePackage.RegulationVersion,
                AutoRulesJson = sourcePackage.AutoRulesJson,
                CimrConfigJson = sourcePackage.CimrConfigJson,
                CimrRate = sourcePackage.CimrRate,
                HasPrivateInsurance = sourcePackage.HasPrivateInsurance,
                Version = 1,
                SourceTemplateId = sourcePackage.SourceTemplateId,
                SourceTemplateVersion = sourcePackage.SourceTemplateVersion,
                ValidFrom = null,
                ValidTo = null,
                IsLocked = false,
                CreatedAt = now,
                CreatedBy = userId,
                Items = clonedItems
            };

            _db.SalaryPackages.Add(duplicatedPackage);
            await _db.SaveChangesAsync();

            var created = await _db.SalaryPackages
                .AsNoTracking()
                .Include(sp => sp.Company)
                .Include(sp => sp.BusinessSector)
                .Include(sp => sp.Items!.Where(i => i.DeletedAt == null))
                    .ThenInclude(i => i.PayComponent)
                .Include(sp => sp.Items!.Where(i => i.DeletedAt == null))
                    .ThenInclude(i => i.ReferentielElement)
                .Include(sp => sp.SourceTemplate)
                .FirstAsync(sp => sp.Id == duplicatedPackage.Id);

            return CreatedAtAction(nameof(GetById), new { id = duplicatedPackage.Id }, MapToReadDto(created));
        }

        /// <summary>
        /// Update a salary package template
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<SalaryPackageReadDto>> Update(int id, [FromBody] SalaryPackageUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Invalid data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var package = await _db.SalaryPackages
                .Include(sp => sp.Items)
                .FirstOrDefaultAsync(sp => sp.Id == id && sp.DeletedAt == null);

            if (package == null)
                return NotFound(new { Message = "Salary package not found" });

            // Enforce immutability rules for locked/published packages
            if (package.IsLocked)
                return BadRequest(new { Message = "Cannot modify a locked package. Create a new version instead." });

            if (string.Equals(package.Status, "published", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { Message = "Cannot modify a published package. Create a new version instead." });

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            if (dto.Name != null)
            {
                var name = dto.Name.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest(new { Message = "Name is required" });
                if (!string.Equals(name, package.Name, StringComparison.OrdinalIgnoreCase))
                {
                    var nameExists = await _db.SalaryPackages
                        .AnyAsync(sp => sp.Name == name && sp.CompanyId == package.CompanyId && sp.Id != id && sp.DeletedAt == null);
                    if (nameExists)
                        return Conflict(new { Message = "A salary package with this name already exists" });

                    package.Name = name;
                }
            }

            if (dto.Category != null)
            {
                var category = dto.Category.Trim();
                if (string.IsNullOrWhiteSpace(category))
                    return BadRequest(new { Message = "Category is required" });
                package.Category = category;
            }

            if (dto.Description != null)
                package.Description = dto.Description.Trim();

            if (dto.BaseSalary.HasValue)
            {
                if (dto.BaseSalary.Value < 0)
                    return BadRequest(new { Message = "Base salary must be positive" });
                package.BaseSalary = dto.BaseSalary.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                var normalizedStatus = NormalizeStatus(dto.Status);
                if (!IsStatusValid(normalizedStatus))
                    return BadRequest(new { Message = "Status must be draft, published, or deprecated" });
                
                // Validate status transitions
                var currentStatus = package.Status.ToLowerInvariant();
                if (currentStatus == "draft" && normalizedStatus == "deprecated")
                    return BadRequest(new { Message = "Cannot deprecate a draft package. Publish it first." });
                
                // When publishing, set ValidFrom if not already set
                if (normalizedStatus == "published" && !package.ValidFrom.HasValue)
                    package.ValidFrom = DateTime.UtcNow;

                package.Status = normalizedStatus;
            }

            // TemplateType is derived from ownership and cannot be set independently.
            package.TemplateType = ResolveTemplateTypeForCompany(package.CompanyId);

            // Update regulation version
            if (!string.IsNullOrWhiteSpace(dto.RegulationVersion))
                package.RegulationVersion = dto.RegulationVersion.Trim();

            // Update AutoRules
            if (dto.AutoRules != null)
            {
                package.AutoRulesJson = JsonSerializer.Serialize(dto.AutoRules, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
            }

            // Update CimrConfig
            if (dto.CimrConfig != null)
            {
                package.CimrConfigJson = JsonSerializer.Serialize(dto.CimrConfig, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
            }

            // Update CIMR and insurance fields (legacy)
            if (dto.CimrRate.HasValue)
            {
                if (dto.CimrRate.Value < 0 || dto.CimrRate.Value > 0.12m)
                    return BadRequest(new { Message = "CIMR rate must be between 0 and 12%" });
                package.CimrRate = dto.CimrRate.Value;
            }

            if (dto.HasPrivateInsurance.HasValue)
                package.HasPrivateInsurance = dto.HasPrivateInsurance.Value;

            // Update effective dates
            if (dto.ValidFrom.HasValue)
                package.ValidFrom = dto.ValidFrom.Value;
            if (dto.ValidTo.HasValue)
                package.ValidTo = dto.ValidTo.Value;

            // Validate business sector exists and is active
            var sectorExists = await _db.BusinessSectors
                .AnyAsync(bs => bs.Id == dto.BusinessSectorId && bs.DeletedAt == null);

            if (!sectorExists)
            {
                return BadRequest(new { Message = "Secteur d'activité invalide" });
            }

            package.BusinessSectorId = dto.BusinessSectorId;

            if (dto.CompanyId.HasValue && dto.CompanyId != package.CompanyId)
            {
                if (dto.CompanyId.Value <= 0)
                    return BadRequest(new { Message = "Company id must be valid" });

                var hasAssignments = await _db.SalaryPackageAssignments
                    .AnyAsync(a => a.SalaryPackageId == id && a.DeletedAt == null);
                if (hasAssignments)
                    return BadRequest(new { Message = "Cannot change company for an assigned package" });

                var companyExists = await _db.Companies
                    .AnyAsync(c => c.Id == dto.CompanyId.Value && c.DeletedAt == null);
                if (!companyExists)
                    return NotFound(new { Message = "Company not found" });

                package.CompanyId = dto.CompanyId;
                package.TemplateType = ResolveTemplateTypeForCompany(package.CompanyId);
            }

            if (dto.Items != null)
            {
                var currentItems = package.Items?.Where(i => i.DeletedAt == null).ToList() ?? new List<SalaryPackageItem>();
                var incoming = dto.Items.ToList();

                var incomingIds = incoming
                    .Where(i => i.Id.HasValue && i.Id.Value > 0)
                    .Select(i => i.Id!.Value)
                    .ToHashSet();

                foreach (var existing in currentItems)
                {
                    if (!incomingIds.Contains(existing.Id))
                    {
                        existing.DeletedAt = now;
                        existing.DeletedBy = userId;
                    }
                }

                var nextSortOrder = 1;
                foreach (var item in incoming)
                {
                    var label = item.Label?.Trim();
                    if (string.IsNullOrWhiteSpace(label))
                        return BadRequest(new { Message = "Item label is required" });

                    if (item.DefaultValue < 0)
                        return BadRequest(new { Message = "Item default value must be positive" });

                    // Validate PayComponentId if provided
                    if (item.PayComponentId.HasValue)
                    {
                        var componentExists = await _db.PayComponents
                            .AnyAsync(pc => pc.Id == item.PayComponentId.Value && pc.DeletedAt == null && pc.IsActive);
                        if (!componentExists)
                            return NotFound(new { Message = $"Pay component with id {item.PayComponentId.Value} not found" });
                    }

                    // Validate ReferentielElementId if provided
                    if (item.ReferentielElementId.HasValue)
                    {
                        var elementExists = await _db.ReferentielElements
                            .AnyAsync(e => e.Id == item.ReferentielElementId.Value && e.DeletedAt == null && e.IsActive);
                        if (!elementExists)
                            return BadRequest(new { Message = $"Referentiel element with id {item.ReferentielElementId.Value} not found or inactive" });
                    }

                    var sortOrder = item.SortOrder ?? nextSortOrder++;

                    if (item.Id.HasValue && item.Id.Value > 0)
                    {
                        var existing = currentItems.FirstOrDefault(i => i.Id == item.Id.Value);
                        if (existing == null)
                            return BadRequest(new { Message = "Item does not belong to this package" });

                        existing.PayComponentId = item.PayComponentId;
                        existing.ReferentielElementId = item.ReferentielElementId;
                        existing.Label = label;
                        existing.DefaultValue = item.DefaultValue;
                        existing.SortOrder = sortOrder;
                        existing.Type = item.Type;
                        existing.IsTaxable = item.IsTaxable;
                        existing.IsSocial = item.IsSocial;
                        existing.IsCIMR = item.IsCIMR;
                        existing.IsVariable = item.IsVariable;
                        existing.ExemptionLimit = item.ExemptionLimit;
                        existing.ModifiedAt = now;
                        existing.ModifiedBy = userId;
                    }
                    else
                    {
                        package.Items ??= new List<SalaryPackageItem>();
                        package.Items.Add(new SalaryPackageItem
                        {
                            PayComponentId = item.PayComponentId,
                            ReferentielElementId = item.ReferentielElementId,
                            Label = label,
                            DefaultValue = item.DefaultValue,
                            SortOrder = sortOrder,
                            Type = item.Type,
                            IsTaxable = item.IsTaxable,
                            IsSocial = item.IsSocial,
                            IsCIMR = item.IsCIMR,
                            IsVariable = item.IsVariable,
                            ExemptionLimit = item.ExemptionLimit,
                            CreatedAt = now,
                            CreatedBy = userId
                        });
                    }
                }
            }

            package.ModifiedAt = now;
            package.ModifiedBy = userId;
            package.TemplateType = ResolveTemplateTypeForCompany(package.CompanyId);

            await _db.SaveChangesAsync();

            var updated = await _db.SalaryPackages
                .AsNoTracking()
                .Include(sp => sp.Company)
                .Include(sp => sp.BusinessSector)
                .Include(sp => sp.Items!)
                    .ThenInclude(i => i.ReferentielElement)
                .Include(sp => sp.SourceTemplate)
                .FirstAsync(sp => sp.Id == id);

            return Ok(MapToReadDto(updated));
        }

        /// <summary>
        /// Soft delete a salary package template
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var package = await _db.SalaryPackages
                .Include(sp => sp.Items)
                .FirstOrDefaultAsync(sp => sp.Id == id && sp.DeletedAt == null);

            if (package == null)
                return NotFound(new { Message = "Salary package not found" });

            var hasAssignments = await _db.SalaryPackageAssignments
                .AnyAsync(a => a.SalaryPackageId == id && a.DeletedAt == null);
            if (hasAssignments)
                return BadRequest(new { Message = "Cannot delete a salary package with assignments" });

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            package.DeletedAt = now;
            package.DeletedBy = userId;

            foreach (var item in package.Items?.Where(i => i.DeletedAt == null) ?? Enumerable.Empty<SalaryPackageItem>())
            {
                item.DeletedAt = now;
                item.DeletedBy = userId;
            }

            await _db.SaveChangesAsync();

            return NoContent();
        }

        private static bool IsStatusValid(string status)
            => AllowedStatuses.Contains(status);

        private static bool IsScopeValid(string scope)
            => AllowedScopes.Contains(scope);

        private static string NormalizeStatus(string? status)
            => (status ?? string.Empty).Trim().ToLowerInvariant();

        private static string NormalizeScope(string? scope, int? companyId)
        {
            if (!string.IsNullOrWhiteSpace(scope))
                return scope.Trim().ToLowerInvariant();

            // Backward-compatible default for company pages:
            // when companyId is provided and no scope is passed, treat as company scope.
            return companyId.HasValue ? "company" : "all";
        }

        private static string ResolveTemplateTypeForCompany(int? companyId)
            => companyId.HasValue ? "COMPANY" : "OFFICIAL";

        private static SalaryPackageReadDto MapToReadDto(SalaryPackage package)
        {
            var items = package.Items?
                .Where(i => i.DeletedAt == null)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .Select(i => new SalaryPackageItemReadDto
                {
                    Id = i.Id,
                    PayComponentId = i.PayComponentId,
                    PayComponentCode = i.PayComponent?.Code,
                    ReferentielElementId = i.ReferentielElementId,
                    ReferentielElementName = i.ReferentielElement != null ? i.ReferentielElement.Name : null,
                    Label = i.Label,
                    DefaultValue = i.DefaultValue,
                    SortOrder = i.SortOrder,
                    Type = i.Type,
                    IsTaxable = i.IsTaxable,
                    IsSocial = i.IsSocial,
                    IsCIMR = i.IsCIMR,
                    IsVariable = i.IsVariable,
                    ExemptionLimit = i.ExemptionLimit
                })
                .ToList() ?? new List<SalaryPackageItemReadDto>();

            var updatedAt = (package.ModifiedAt ?? package.CreatedAt).DateTime;

            // Parse AutoRules from JSON
            AutoRulesDto? autoRules = null;
            if (!string.IsNullOrEmpty(package.AutoRulesJson))
            {
                try
                {
                    autoRules = JsonSerializer.Deserialize<AutoRulesDto>(package.AutoRulesJson, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                }
                catch { /* Use defaults if parsing fails */ }
            }
            autoRules ??= new AutoRulesDto { SeniorityBonusEnabled = true, RuleVersion = "MA_2025" };

            // Parse CimrConfig from JSON
            CimrConfigDto? cimrConfig = null;
            if (!string.IsNullOrEmpty(package.CimrConfigJson))
            {
                try
                {
                    cimrConfig = JsonSerializer.Deserialize<CimrConfigDto>(package.CimrConfigJson, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                }
                catch { /* Use defaults if parsing fails */ }
            }

            return new SalaryPackageReadDto
            {
                Id = package.Id,
                Name = package.Name,
                Category = package.Category,
                Description = package.Description,
                BaseSalary = package.BaseSalary,
                Status = package.Status,
                CompanyId = package.CompanyId,
                CompanyName = package.Company?.CompanyName,
                BusinessSectorId = package.BusinessSectorId,
                BusinessSectorName = package.BusinessSector != null ? package.BusinessSector.Name : null,
                // New fields
                TemplateType = package.TemplateType,
                RegulationVersion = package.RegulationVersion,
                AutoRules = autoRules,
                CimrConfig = cimrConfig,
                OriginType = package.OriginType,
                SourceTemplateNameSnapshot = package.SourceTemplateNameSnapshot,
                CopiedAt = package.CopiedAt,
                // Legacy fields
                CimrRate = package.CimrRate,
                HasPrivateInsurance = package.HasPrivateInsurance,
                // Versioning
                Version = package.Version,
                SourceTemplateId = package.SourceTemplateId,
                SourceTemplateName = package.SourceTemplate?.Name,
                SourceTemplateVersion = package.SourceTemplateVersion,
                ValidFrom = package.ValidFrom,
                ValidTo = package.ValidTo,
                IsLocked = package.IsLocked,
                Items = items,
                UpdatedAt = updatedAt,
                CreatedAt = package.CreatedAt.DateTime
            };
        }
    }
}
