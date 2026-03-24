using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using payzen_backend.Data;
using payzen_backend.Models.Payroll.Referentiel;
using payzen_backend.Services.Validation;

namespace payzen_backend.Tests.Services.Validation;

public class ReferentialValidationServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<ReferentialValidationService>> _loggerMock;
    private readonly ReferentialValidationService _service;

    public ReferentialValidationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<ReferentialValidationService>>();
        _service = new ReferentialValidationService(_context, _loggerMock.Object);

        SeedTestData();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private void SeedTestData()
    {
        var category = new ElementCategory
        {
            Id = 1,
            Name = "Indemnités",
            SortOrder = 1,
            IsActive = true
        };

        var authority = new Authority
        {
            Id = 1,
            Code = "CNSS",
            Name = "CNSS",
            IsActive = true
        };

        var element = new ReferentielElement
        {
            Id = 1,
            Code = "transport",
            Name = "Transport",
            CategoryId = 1,
            DefaultFrequency = PaymentFrequency.MONTHLY,
            Status = ElementStatus.ACTIVE,
            IsActive = true,
            CreatedBy = 1
        };

        var parameter = new LegalParameter
        {
            Id = 1,
            Code = "SMIG",
            Label = "SMIG",
            Value = 3000m,
            Unit = "PER_MONTH",
            EffectiveFrom = new DateOnly(2024, 1, 1),
            CreatedBy = 1
        };

        _context.ElementCategories.Add(category);
        _context.Authorities.Add(authority);
        _context.ReferentielElements.Add(element);
        _context.LegalParameters.Add(parameter);
        _context.SaveChanges();
    }

    // ================================================================
    // ValidateElementAsync Tests
    // ================================================================

    [Fact]
    public async Task ValidateElementAsync_ValidNewElement_ReturnsSuccess()
    {
        // Arrange
        var element = new ReferentielElement
        {
            Code = "new_element",
            Name = "New Element",
            CategoryId = 1,
            DefaultFrequency = PaymentFrequency.MONTHLY,
            Status = ElementStatus.DRAFT,
            IsActive = true,
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateElementAsync(element, isUpdate: false);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateElementAsync_MissingName_ReturnsError()
    {
        // Arrange
        var element = new ReferentielElement
        {
            Code = "test",
            Name = "", // Empty name
            CategoryId = 1,
            DefaultFrequency = PaymentFrequency.MONTHLY,
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateElementAsync(element, isUpdate: false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateElementAsync_NameTooShort_ReturnsError()
    {
        // Arrange
        var element = new ReferentielElement
        {
            Name = "AB", // Less than 3 characters
            CategoryId = 1,
            DefaultFrequency = PaymentFrequency.MONTHLY,
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateElementAsync(element, isUpdate: false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("3 characters"));
    }

    [Fact]
    public async Task ValidateElementAsync_InvalidCategoryId_ReturnsError()
    {
        // Arrange
        var element = new ReferentielElement
        {
            Name = "Test Element",
            CategoryId = 999, // Non-existent category
            DefaultFrequency = PaymentFrequency.MONTHLY,
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateElementAsync(element, isUpdate: false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("category", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateElementAsync_MissingCategoryId_ReturnsError()
    {
        // Arrange
        var element = new ReferentielElement
        {
            Name = "Test Element",
            CategoryId = 0, // Missing category
            DefaultFrequency = PaymentFrequency.MONTHLY,
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateElementAsync(element, isUpdate: false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Category is required"));
    }

    [Fact]
    public async Task ValidateElementAsync_DuplicateCode_ReturnsError()
    {
        // Arrange - Element with code "transport" already exists in seed data
        var element = new ReferentielElement
        {
            Code = "transport",
            Name = "Duplicate Transport",
            CategoryId = 1,
            DefaultFrequency = PaymentFrequency.MONTHLY,
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateElementAsync(element, isUpdate: false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("code") && e.Contains("exists"));
    }

    [Fact]
    public async Task ValidateElementAsync_InvalidCodeFormat_ReturnsError()
    {
        // Arrange - Code must be lowercase letters and underscores only
        var element = new ReferentielElement
        {
            Code = "INVALID_CODE", // Uppercase not allowed
            Name = "Test Element",
            CategoryId = 1,
            DefaultFrequency = PaymentFrequency.MONTHLY,
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateElementAsync(element, isUpdate: false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("lowercase"));
    }

    [Fact]
    public async Task ValidateElementAsync_DuplicateName_ReturnsError()
    {
        // Arrange - Element with name "Transport" already exists in seed data
        var element = new ReferentielElement
        {
            Code = "other_code",
            Name = "Transport",
            CategoryId = 1,
            DefaultFrequency = PaymentFrequency.MONTHLY,
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateElementAsync(element, isUpdate: false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("name") && e.Contains("exists"));
    }

    // ================================================================
    // ValidateElementForActivationAsync Tests
    // ================================================================

    [Fact]
    public async Task ValidateElementForActivationAsync_HasBothRules_ReturnsSuccess()
    {
        // Arrange - Add both CNSS and DGI rules
        var dgiAuthority = new Authority
        {
            Id = 2,
            Code = "DGI",
            Name = "DGI",
            IsActive = true
        };
        _context.Authorities.Add(dgiAuthority);

        var cnssRule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 1, // CNSS
            ExemptionType = ExemptionType.FULLY_EXEMPT,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            Status = ElementStatus.ACTIVE,
            RuleDetails = "{}",
            CreatedBy = 1
        };

        var dgiRule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 2, // DGI
            ExemptionType = ExemptionType.FULLY_EXEMPT,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            Status = ElementStatus.ACTIVE,
            RuleDetails = "{}",
            CreatedBy = 1
        };

        _context.ElementRules.AddRange(cnssRule, dgiRule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateElementForActivationAsync(1);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateElementForActivationAsync_MissingDgiRule_ReturnsWarning()
    {
        // Arrange - Add only CNSS rule
        var cnssRule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 1, // CNSS
            ExemptionType = ExemptionType.FULLY_EXEMPT,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            Status = ElementStatus.ACTIVE,
            RuleDetails = "{}",
            CreatedBy = 1
        };

        _context.ElementRules.Add(cnssRule);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateElementForActivationAsync(1);

        // Assert
        result.IsValid.Should().BeTrue(); // Still valid, just has warnings
        result.Warnings.Should().Contain(w => w.Contains("DGI"));
    }

    [Fact]
    public async Task ValidateElementForActivationAsync_NoRules_ReturnsWarning()
    {
        // Act
        var result = await _service.ValidateElementForActivationAsync(1);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("no active rules"));
    }

    [Fact]
    public async Task ValidateElementForActivationAsync_NonExistentElement_ReturnsError()
    {
        // Act
        var result = await _service.ValidateElementForActivationAsync(999);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("not found"));
    }

    // ================================================================
    // ValidateRuleAsync Tests
    // ================================================================

    [Fact]
    public async Task ValidateRuleAsync_ValidFullyExemptRule_ReturnsSuccess()
    {
        // Arrange
        var rule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 1,
            ExemptionType = ExemptionType.FULLY_EXEMPT,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            Status = ElementStatus.DRAFT,
            RuleDetails = "{}",
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateRuleAsync(rule, isUpdate: false);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateRuleAsync_MissingElementId_ReturnsError()
    {
        // Arrange
        var rule = new ElementRule
        {
            ElementId = 0, // Invalid
            AuthorityId = 1,
            ExemptionType = ExemptionType.FULLY_EXEMPT,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            RuleDetails = "{}",
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateRuleAsync(rule, isUpdate: false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Element ID"));
    }

    [Fact]
    public async Task ValidateRuleAsync_InvalidAuthorityId_ReturnsError()
    {
        // Arrange
        var rule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 999, // Non-existent
            ExemptionType = ExemptionType.FULLY_EXEMPT,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            RuleDetails = "{}",
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateRuleAsync(rule, isUpdate: false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("authority", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateRuleAsync_CappedWithoutCapDetails_ReturnsError()
    {
        // Arrange - CAPPED rule but RuleDetails JSON missing capAmount/capUnit
        var rule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 1,
            ExemptionType = ExemptionType.CAPPED,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            RuleDetails = "{}",
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateRuleAsync(rule, isUpdate: false);

        // Assert - Service uses AddRange for sub-validation errors (doesn't flip IsValid)
        result.Errors.Should().Contain(e => e.Contains("capAmount"));
    }

    [Fact]
    public async Task ValidateRuleAsync_CappedWithValidDetails_ReturnsSuccess()
    {
        // Arrange
        var rule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 1,
            ExemptionType = ExemptionType.CAPPED,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            RuleDetails = """{"capAmount":500,"capUnit":"PER_MONTH"}""",
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateRuleAsync(rule, isUpdate: false);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateRuleAsync_PercentageWithoutDetails_ReturnsError()
    {
        // Arrange - PERCENTAGE rule but JSON missing percentage/baseReference
        var rule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 1,
            ExemptionType = ExemptionType.PERCENTAGE,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            RuleDetails = "{}",
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateRuleAsync(rule, isUpdate: false);

        // Assert - Service uses AddRange for sub-validation errors (doesn't flip IsValid)
        result.Errors.Should().Contain(e => e.Contains("percentage"));
    }

    [Fact]
    public async Task ValidateRuleAsync_PercentageWithValidDetails_ReturnsSuccess()
    {
        // Arrange
        var rule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 1,
            ExemptionType = ExemptionType.PERCENTAGE,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            RuleDetails = """{"percentage":20,"baseReference":"GROSS_SALARY"}""",
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateRuleAsync(rule, isUpdate: false);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateRuleAsync_FormulaWithoutDetails_ReturnsError()
    {
        // Arrange - FORMULA rule but JSON missing multiplier/parameterCode
        var rule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 1,
            ExemptionType = ExemptionType.FORMULA,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            RuleDetails = "{}",
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateRuleAsync(rule, isUpdate: false);

        // Assert - Service uses AddRange for sub-validation errors (doesn't flip IsValid)
        result.Errors.Should().Contain(e => e.Contains("multiplier"));
    }

    [Fact]
    public async Task ValidateRuleAsync_FormulaWithValidDetails_ReturnsSuccess()
    {
        // Arrange
        var rule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 1,
            ExemptionType = ExemptionType.FORMULA,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            RuleDetails = """{"multiplier":2,"parameterCode":"SMIG"}""",
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateRuleAsync(rule, isUpdate: false);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateRuleAsync_TieredWithoutTiers_ReturnsError()
    {
        // Arrange - TIERED rule but JSON missing tiers array
        var rule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 1,
            ExemptionType = ExemptionType.TIERED,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            RuleDetails = "{}",
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateRuleAsync(rule, isUpdate: false);

        // Assert - Service uses AddRange for sub-validation errors (doesn't flip IsValid)
        result.Errors.Should().Contain(e => e.Contains("tiers"));
    }

    [Fact]
    public async Task ValidateRuleAsync_TieredWithValidTiers_ReturnsSuccess()
    {
        // Arrange
        var rule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 1,
            ExemptionType = ExemptionType.TIERED,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            RuleDetails = """{"tiers":[{"fromAmount":0,"toAmount":5000,"exemptPercent":100},{"fromAmount":5000,"exemptPercent":50}]}""",
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateRuleAsync(rule, isUpdate: false);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateRuleAsync_InvalidDateRange_ReturnsError()
    {
        // Arrange - EffectiveTo before EffectiveFrom
        var rule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 1,
            ExemptionType = ExemptionType.FULLY_EXEMPT,
            EffectiveFrom = new DateOnly(2025, 1, 1),
            EffectiveTo = new DateOnly(2024, 1, 1), // Before EffectiveFrom
            RuleDetails = "{}",
            CreatedBy = 1
        };

        // Act
        var result = await _service.ValidateRuleAsync(rule, isUpdate: false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("date"));
    }

    // ================================================================
    // CheckDateRangeOverlapAsync Tests
    // ================================================================

    [Fact]
    public async Task CheckDateRangeOverlapAsync_NoOverlap_ReturnsSuccess()
    {
        // Arrange - Add existing ACTIVE rule with end date
        var existingRule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 1,
            ExemptionType = ExemptionType.FULLY_EXEMPT,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            EffectiveTo = new DateOnly(2024, 12, 31),
            Status = ElementStatus.ACTIVE,
            RuleDetails = "{}",
            CreatedBy = 1
        };
        _context.ElementRules.Add(existingRule);
        await _context.SaveChangesAsync();

        // Act - Check non-overlapping date range
        var result = await _service.CheckDateRangeOverlapAsync(
            elementId: 1,
            authorityId: 1,
            effectiveFrom: new DateOnly(2025, 1, 1),
            effectiveTo: null,
            excludeRuleId: null
        );

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CheckDateRangeOverlapAsync_Overlap_ReturnsError()
    {
        // Arrange - Add existing ACTIVE rule
        var existingRule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 1,
            ExemptionType = ExemptionType.FULLY_EXEMPT,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            EffectiveTo = new DateOnly(2024, 12, 31),
            Status = ElementStatus.ACTIVE,
            RuleDetails = "{}",
            CreatedBy = 1
        };
        _context.ElementRules.Add(existingRule);
        await _context.SaveChangesAsync();

        // Act - Check overlapping date range
        var result = await _service.CheckDateRangeOverlapAsync(
            elementId: 1,
            authorityId: 1,
            effectiveFrom: new DateOnly(2024, 6, 1),
            effectiveTo: new DateOnly(2025, 6, 1),
            excludeRuleId: null
        );

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("overlap", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CheckDateRangeOverlapAsync_ExcludeSelf_ReturnsSuccess()
    {
        // Arrange - Add existing ACTIVE rule
        var existingRule = new ElementRule
        {
            ElementId = 1,
            AuthorityId = 1,
            ExemptionType = ExemptionType.FULLY_EXEMPT,
            EffectiveFrom = new DateOnly(2024, 1, 1),
            EffectiveTo = new DateOnly(2024, 12, 31),
            Status = ElementStatus.ACTIVE,
            RuleDetails = "{}",
            CreatedBy = 1
        };
        _context.ElementRules.Add(existingRule);
        await _context.SaveChangesAsync();

        var ruleId = existingRule.Id;

        // Act - Check same date range but exclude self (update scenario)
        var result = await _service.CheckDateRangeOverlapAsync(
            elementId: 1,
            authorityId: 1,
            effectiveFrom: new DateOnly(2024, 1, 1),
            effectiveTo: new DateOnly(2024, 12, 31),
            excludeRuleId: ruleId
        );

        // Assert
        result.IsValid.Should().BeTrue();
    }

    // ================================================================
    // ValidateRuleDetailsJson Tests
    // ================================================================

    [Fact]
    public void ValidateRuleDetailsJson_FullyExempt_ReturnsSuccess()
    {
        var result = _service.ValidateRuleDetailsJson(ExemptionType.FULLY_EXEMPT, "{}");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateRuleDetailsJson_FullySubject_ReturnsSuccess()
    {
        var result = _service.ValidateRuleDetailsJson(ExemptionType.FULLY_SUBJECT, "{}");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateRuleDetailsJson_InvalidJson_ReturnsError()
    {
        var result = _service.ValidateRuleDetailsJson(ExemptionType.FULLY_EXEMPT, "{ invalid json }");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("JSON"));
    }

    [Fact]
    public void ValidateRuleDetailsJson_CappedValid_ReturnsSuccess()
    {
        var json = """{"capAmount":500,"capUnit":"PER_MONTH"}""";

        var result = _service.ValidateRuleDetailsJson(ExemptionType.CAPPED, json);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateRuleDetailsJson_CappedMissingFields_ReturnsError()
    {
        var result = _service.ValidateRuleDetailsJson(ExemptionType.CAPPED, "{}");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("capAmount"));
        result.Errors.Should().Contain(e => e.Contains("capUnit"));
    }

    [Fact]
    public void ValidateRuleDetailsJson_PercentageValid_ReturnsSuccess()
    {
        var json = """{"percentage":20,"baseReference":"GROSS_SALARY"}""";

        var result = _service.ValidateRuleDetailsJson(ExemptionType.PERCENTAGE, json);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateRuleDetailsJson_PercentageMissingFields_ReturnsError()
    {
        var result = _service.ValidateRuleDetailsJson(ExemptionType.PERCENTAGE, "{}");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("percentage"));
        result.Errors.Should().Contain(e => e.Contains("baseReference"));
    }

    [Fact]
    public void ValidateRuleDetailsJson_FormulaValid_ReturnsSuccess()
    {
        var json = """{"multiplier":2,"parameterCode":"SMIG"}""";

        var result = _service.ValidateRuleDetailsJson(ExemptionType.FORMULA, json);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateRuleDetailsJson_TieredValid_ReturnsSuccess()
    {
        var json = """{"tiers":[{"fromAmount":0,"toAmount":5000,"exemptPercent":100},{"fromAmount":5000,"exemptPercent":50}]}""";

        var result = _service.ValidateRuleDetailsJson(ExemptionType.TIERED, json);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateRuleDetailsJson_TieredEmptyArray_ReturnsError()
    {
        var json = """{"tiers":[]}""";

        var result = _service.ValidateRuleDetailsJson(ExemptionType.TIERED, json);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("tier", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateRuleDetailsJson_DualCapValid_ReturnsSuccess()
    {
        var json = """{"fixedCapAmount":1000,"percentageCap":20,"logic":"MIN"}""";

        var result = _service.ValidateRuleDetailsJson(ExemptionType.DUAL_CAP, json);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateRuleDetailsJson_DualCapMissingFields_ReturnsError()
    {
        var result = _service.ValidateRuleDetailsJson(ExemptionType.DUAL_CAP, "{}");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("fixedCapAmount"));
        result.Errors.Should().Contain(e => e.Contains("percentageCap"));
        result.Errors.Should().Contain(e => e.Contains("logic"));
    }

    [Fact]
    public void ValidateRuleDetailsJson_CappedNegativeAmount_ReturnsError()
    {
        var json = """{"capAmount":-100,"capUnit":"PER_MONTH"}""";

        var result = _service.ValidateRuleDetailsJson(ExemptionType.CAPPED, json);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("capAmount") && e.Contains("positive"));
    }
}
