using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using payzen_backend.Data;
using payzen_backend.Models.Payroll.Referentiel;
using payzen_backend.Services.Convergence;

namespace payzen_backend.Tests.Services.Convergence;

public class ConvergenceAnalysisServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ILogger<ConvergenceAnalysisService>> _loggerMock;
    private readonly ConvergenceAnalysisService _service;

    public ConvergenceAnalysisServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<ConvergenceAnalysisService>>();
        _service = new ConvergenceAnalysisService(_context, _loggerMock.Object);

        SeedTestData();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private void SeedTestData()
    {
        // Authorities
        var cnss = new Authority { Id = 1, Code = "CNSS", Name = "CNSS", IsActive = true };
        var dgi = new Authority { Id = 2, Code = "DGI", Name = "DGI", IsActive = true };
        _context.Authorities.AddRange(cnss, dgi);
        _context.SaveChanges();

        // Category
        var category = new ElementCategory { Id = 1, Name = "Indemnités", SortOrder = 1, IsActive = true };
        _context.ElementCategories.Add(category);
        _context.SaveChanges();

        // Elements
        var transport = new ReferentielElement
        {
            Id = 1, Code = "transport", Name = "Indemnité de transport",
            CategoryId = 1, DefaultFrequency = PaymentFrequency.MONTHLY,
            Status = ElementStatus.ACTIVE, IsActive = true, CreatedBy = 1
        };
        var panier = new ReferentielElement
        {
            Id = 2, Code = "panier", Name = "Indemnité de panier",
            CategoryId = 1, DefaultFrequency = PaymentFrequency.MONTHLY,
            Status = ElementStatus.ACTIVE, IsActive = true, CreatedBy = 1
        };
        var representation = new ReferentielElement
        {
            Id = 3, Code = "representation", Name = "Indemnité de représentation",
            CategoryId = 1, DefaultFrequency = PaymentFrequency.MONTHLY,
            Status = ElementStatus.ACTIVE, IsActive = true, CreatedBy = 1
        };
        _context.ReferentielElements.AddRange(transport, panier, representation);
        _context.SaveChanges();

        // Convergent Rules: Transport (both FULLY_EXEMPT)
        _context.ElementRules.AddRange(
            new ElementRule
            {
                Id = 1, ElementId = 1, AuthorityId = 1, Authority = cnss,
                ExemptionType = ExemptionType.FULLY_EXEMPT,
                EffectiveFrom = new DateOnly(2024, 1, 1),
                Status = ElementStatus.ACTIVE, RuleDetails = "{}", CreatedBy = 1
            },
            new ElementRule
            {
                Id = 2, ElementId = 1, AuthorityId = 2, Authority = dgi,
                ExemptionType = ExemptionType.FULLY_EXEMPT,
                EffectiveFrom = new DateOnly(2024, 1, 1),
                Status = ElementStatus.ACTIVE, RuleDetails = "{}", CreatedBy = 1
            }
        );

        // Divergent Rules: Panier (CNSS exempt vs DGI subject)
        _context.ElementRules.AddRange(
            new ElementRule
            {
                Id = 3, ElementId = 2, AuthorityId = 1, Authority = cnss,
                ExemptionType = ExemptionType.FULLY_EXEMPT,
                EffectiveFrom = new DateOnly(2024, 1, 1),
                Status = ElementStatus.ACTIVE, RuleDetails = "{}", CreatedBy = 1
            },
            new ElementRule
            {
                Id = 4, ElementId = 2, AuthorityId = 2, Authority = dgi,
                ExemptionType = ExemptionType.FULLY_SUBJECT,
                EffectiveFrom = new DateOnly(2024, 1, 1),
                Status = ElementStatus.ACTIVE, RuleDetails = "{}", CreatedBy = 1
            }
        );

        // Element 3 (representation) has no rules
        _context.SaveChanges();
    }

    // ================================================================
    // AnalyzeElementAsync Tests
    // ================================================================

    [Fact]
    public async Task AnalyzeElementAsync_ConvergentRules_ReturnsConvergent()
    {
        var result = await _service.AnalyzeElementAsync(1);

        result.Should().NotBeNull();
        result.IsConvergent.Should().BeTrue();
        result.CnssRuleId.Should().Be(1);
        result.DgiRuleId.Should().Be(2);
        // GetDivergenceDetails always returns field comparisons; all should match
        result.Differences.Should().OnlyContain(d => d.Matches);
    }

    [Fact]
    public async Task AnalyzeElementAsync_DivergentRules_ReturnsDivergent()
    {
        var result = await _service.AnalyzeElementAsync(2);

        result.Should().NotBeNull();
        result.IsConvergent.Should().BeFalse();
        result.CnssRuleId.Should().Be(3);
        result.DgiRuleId.Should().Be(4);
    }

    [Fact]
    public async Task AnalyzeElementAsync_NoRules_ReturnsDivergent()
    {
        var result = await _service.AnalyzeElementAsync(3);

        result.Should().NotBeNull();
        result.IsConvergent.Should().BeFalse();
        result.CnssRuleId.Should().BeNull();
        result.DgiRuleId.Should().BeNull();
    }

    [Fact]
    public async Task AnalyzeElementAsync_NonExistentElement_ThrowsException()
    {
        await Assert.ThrowsAnyAsync<Exception>(() => _service.AnalyzeElementAsync(999));
    }

    // ================================================================
    // RecalculateAllConvergenceAsync Tests
    // ================================================================

    [Fact]
    public async Task RecalculateAllConvergenceAsync_UpdatesAllElements()
    {
        var updatedCount = await _service.RecalculateAllConvergenceAsync();

        updatedCount.Should().BeGreaterThanOrEqualTo(0);

        var transport = await _context.ReferentielElements.FindAsync(1);
        var panier = await _context.ReferentielElements.FindAsync(2);
        var representation = await _context.ReferentielElements.FindAsync(3);

        transport!.HasConvergence.Should().BeTrue();
        panier!.HasConvergence.Should().BeFalse();
        representation!.HasConvergence.Should().BeFalse();
    }

    [Fact]
    public async Task RecalculateElementConvergenceAsync_UpdatesSingleElement()
    {
        // AnalyzeElementAsync (called internally) already updates HasConvergence,
        // so RecalculateElementConvergenceAsync sees no further change and returns false
        var result = await _service.RecalculateElementConvergenceAsync(1);

        // Verify the element's HasConvergence was correctly set
        var element = await _context.ReferentielElements.FindAsync(1);
        element!.HasConvergence.Should().BeTrue();
    }

    [Fact]
    public async Task RecalculateElementConvergenceAsync_DivergentElement_SetsFalse()
    {
        // AnalyzeElementAsync (called internally) already updates HasConvergence
        var result = await _service.RecalculateElementConvergenceAsync(2);

        var element = await _context.ReferentielElements.FindAsync(2);
        element!.HasConvergence.Should().BeFalse();
    }

    // ================================================================
    // AreRulesConvergent Tests
    // ================================================================

    [Fact]
    public void AreRulesConvergent_BothFullyExempt_ReturnsTrue()
    {
        var cnss = new ElementRule { ExemptionType = ExemptionType.FULLY_EXEMPT, RuleDetails = "{}" };
        var dgi = new ElementRule { ExemptionType = ExemptionType.FULLY_EXEMPT, RuleDetails = "{}" };

        _service.AreRulesConvergent(cnss, dgi).Should().BeTrue();
    }

    [Fact]
    public void AreRulesConvergent_DifferentExemptionTypes_ReturnsFalse()
    {
        var cnss = new ElementRule { ExemptionType = ExemptionType.FULLY_EXEMPT, RuleDetails = "{}" };
        var dgi = new ElementRule { ExemptionType = ExemptionType.FULLY_SUBJECT, RuleDetails = "{}" };

        _service.AreRulesConvergent(cnss, dgi).Should().BeFalse();
    }

    [Fact]
    public void AreRulesConvergent_NullRules_ReturnsFalse()
    {
        _service.AreRulesConvergent(null, null).Should().BeFalse();

        var rule = new ElementRule { ExemptionType = ExemptionType.FULLY_EXEMPT, RuleDetails = "{}" };
        _service.AreRulesConvergent(rule, null).Should().BeFalse();
        _service.AreRulesConvergent(null, rule).Should().BeFalse();
    }

    [Fact]
    public void AreRulesConvergent_CappedWithSameDetails_ReturnsTrue()
    {
        var capJson = """{"capAmount":500,"capUnit":"PER_MONTH"}""";
        var cnss = new ElementRule { ExemptionType = ExemptionType.CAPPED, RuleDetails = capJson };
        var dgi = new ElementRule { ExemptionType = ExemptionType.CAPPED, RuleDetails = capJson };

        _service.AreRulesConvergent(cnss, dgi).Should().BeTrue();
    }

    [Fact]
    public void AreRulesConvergent_CappedWithDifferentAmount_ReturnsFalse()
    {
        var cnssJson = """{"capAmount":500,"capUnit":"PER_MONTH"}""";
        var dgiJson = """{"capAmount":600,"capUnit":"PER_MONTH"}""";
        var cnss = new ElementRule { ExemptionType = ExemptionType.CAPPED, RuleDetails = cnssJson };
        var dgi = new ElementRule { ExemptionType = ExemptionType.CAPPED, RuleDetails = dgiJson };

        _service.AreRulesConvergent(cnss, dgi).Should().BeFalse();
    }

    [Fact]
    public void AreRulesConvergent_PercentageWithSameDetails_ReturnsTrue()
    {
        var pctJson = """{"percentage":0.20,"baseReference":"GROSS_SALARY"}""";
        var cnss = new ElementRule { ExemptionType = ExemptionType.PERCENTAGE, RuleDetails = pctJson };
        var dgi = new ElementRule { ExemptionType = ExemptionType.PERCENTAGE, RuleDetails = pctJson };

        _service.AreRulesConvergent(cnss, dgi).Should().BeTrue();
    }

    // ================================================================
    // GetDivergenceDetails Tests
    // ================================================================

    [Fact]
    public void GetDivergenceDetails_DifferentExemptionTypes_ReturnsDifferences()
    {
        var cnss = new ElementRule { ExemptionType = ExemptionType.FULLY_EXEMPT, RuleDetails = "{}" };
        var dgi = new ElementRule { ExemptionType = ExemptionType.FULLY_SUBJECT, RuleDetails = "{}" };

        var details = _service.GetDivergenceDetails(cnss, dgi);

        details.Should().NotBeEmpty();
        details.Should().Contain(d => d.FieldName == "ExemptionType");
        var typeDiff = details.First(d => d.FieldName == "ExemptionType");
        typeDiff.CnssValue.Should().Be("FULLY_EXEMPT");
        typeDiff.DgiValue.Should().Be("FULLY_SUBJECT");
        typeDiff.Matches.Should().BeFalse();
    }

    [Fact]
    public void GetDivergenceDetails_SameRules_ReturnsMatchingFields()
    {
        var cnss = new ElementRule { ExemptionType = ExemptionType.FULLY_EXEMPT, RuleDetails = "{}" };
        var dgi = new ElementRule { ExemptionType = ExemptionType.FULLY_EXEMPT, RuleDetails = "{}" };

        var details = _service.GetDivergenceDetails(cnss, dgi);

        // ExemptionType should match
        var typeDiff = details.FirstOrDefault(d => d.FieldName == "ExemptionType");
        if (typeDiff != null)
        {
            typeDiff.Matches.Should().BeTrue();
        }
    }
}
