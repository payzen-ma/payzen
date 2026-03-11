using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Payroll.Referentiel;

namespace payzen_backend.Services.Convergence
{
    /// <summary>
    /// Service for analyzing convergence/divergence between CNSS and DGI rules
    /// </summary>
    public class ConvergenceAnalysisService : IConvergenceAnalysisService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ConvergenceAnalysisService> _logger;

        public ConvergenceAnalysisService(AppDbContext context, ILogger<ConvergenceAnalysisService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<ConvergenceResult> AnalyzeElementAsync(int elementId, DateOnly? asOfDate = null)
        {
            var element = await _context.ReferentielElements
                .Include(e => e.Rules)
                .ThenInclude(r => r.Authority)
                .FirstOrDefaultAsync(e => e.Id == elementId && e.IsActive);

            if (element == null)
            {
                throw new InvalidOperationException($"Element with ID {elementId} not found or inactive");
            }

            var checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            // Get active CNSS rule
            var cnssRule = element.Rules.FirstOrDefault(r =>
                r.Authority.Code == "CNSS" &&
                r.Status == ElementStatus.ACTIVE &&
                r.EffectiveFrom <= checkDate &&
                (r.EffectiveTo == null || r.EffectiveTo >= checkDate));

            // Get active DGI rule
            var dgiRule = element.Rules.FirstOrDefault(r =>
                r.Authority.Code == "DGI" &&
                r.Status == ElementStatus.ACTIVE &&
                r.EffectiveFrom <= checkDate &&
                (r.EffectiveTo == null || r.EffectiveTo >= checkDate));

            var result = new ConvergenceResult
            {
                CnssRuleId = cnssRule?.Id,
                DgiRuleId = dgiRule?.Id
            };

            if (cnssRule == null || dgiRule == null)
            {
                result.IsConvergent = false;
                result.Summary = cnssRule == null && dgiRule == null
                    ? "Both CNSS and DGI rules are missing"
                    : cnssRule == null
                        ? "CNSS rule is missing"
                        : "DGI rule is missing";
                return result;
            }

            // Compare rules
            result.IsConvergent = AreRulesConvergent(cnssRule, dgiRule);
            result.Differences = GetDivergenceDetails(cnssRule, dgiRule);
            result.Summary = result.IsConvergent
                ? "CNSS and DGI rules are convergent"
                : $"CNSS and DGI rules diverge on {result.Differences.Count(d => !d.Matches)} field(s)";

            // Update element's HasConvergence field
            if (element.HasConvergence != result.IsConvergent)
            {
                element.HasConvergence = result.IsConvergent;
                await _context.SaveChangesAsync();
            }

            return result;
        }

        /// <inheritdoc/>
        public bool AreRulesConvergent(ElementRule? cnssRule, ElementRule? dgiRule)
        {
            // Both null = not convergent (no rules)
            if (cnssRule == null || dgiRule == null)
                return false;

            // Different exemption types = divergent
            if (cnssRule.ExemptionType != dgiRule.ExemptionType)
                return false;

            // Compare JSON details based on exemption type
            try
            {
                return CompareRuleDetails(cnssRule, dgiRule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing rule details for rules {CnssRuleId} and {DgiRuleId}",
                    cnssRule.Id, dgiRule.Id);
                return false;
            }
        }

        /// <inheritdoc/>
        public List<FieldComparison> GetDivergenceDetails(ElementRule cnssRule, ElementRule dgiRule)
        {
            var comparisons = new List<FieldComparison>();

            // Compare exemption type
            comparisons.Add(new FieldComparison
            {
                FieldName = "ExemptionType",
                CnssValue = cnssRule.ExemptionType.ToString(),
                DgiValue = dgiRule.ExemptionType.ToString(),
                Matches = cnssRule.ExemptionType == dgiRule.ExemptionType
            });

            // If types don't match, no need to compare details
            if (cnssRule.ExemptionType != dgiRule.ExemptionType)
            {
                return comparisons;
            }

            // Compare JSON details based on type
            try
            {
                var cnssDetails = JsonDocument.Parse(cnssRule.RuleDetails);
                var dgiDetails = JsonDocument.Parse(dgiRule.RuleDetails);

                switch (cnssRule.ExemptionType)
                {
                    case ExemptionType.CAPPED:
                        CompareCapFields(cnssDetails, dgiDetails, comparisons);
                        break;

                    case ExemptionType.PERCENTAGE:
                        ComparePercentageFields(cnssDetails, dgiDetails, comparisons);
                        break;

                    case ExemptionType.PERCENTAGE_CAPPED:
                        ComparePercentageFields(cnssDetails, dgiDetails, comparisons);
                        CompareCapFields(cnssDetails, dgiDetails, comparisons);
                        break;

                    case ExemptionType.FORMULA:
                        CompareFormulaFields(cnssDetails, dgiDetails, comparisons);
                        break;

                    case ExemptionType.FORMULA_CAPPED:
                        CompareFormulaFields(cnssDetails, dgiDetails, comparisons);
                        CompareCapFields(cnssDetails, dgiDetails, comparisons);
                        break;

                    case ExemptionType.DUAL_CAP:
                        CompareDualCapFields(cnssDetails, dgiDetails, comparisons);
                        break;

                    case ExemptionType.TIERED:
                        CompareTieredFields(cnssDetails, dgiDetails, comparisons);
                        break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing rule details JSON");
                comparisons.Add(new FieldComparison
                {
                    FieldName = "RuleDetails",
                    CnssValue = "Invalid JSON",
                    DgiValue = "Invalid JSON",
                    Matches = false
                });
            }

            return comparisons;
        }

        /// <inheritdoc/>
        public async Task<int> RecalculateAllConvergenceAsync()
        {
            var elements = await _context.ReferentielElements
                .Include(e => e.Rules)
                .ThenInclude(r => r.Authority)
                .Where(e => e.IsActive && e.Status == ElementStatus.ACTIVE)
                .ToListAsync();

            int updated = 0;

            foreach (var element in elements)
            {
                try
                {
                    var result = await AnalyzeElementAsync(element.Id);
                    if (element.HasConvergence != result.IsConvergent)
                    {
                        updated++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error recalculating convergence for element {ElementId}", element.Id);
                }
            }

            return updated;
        }

        /// <inheritdoc/>
        public async Task<bool> RecalculateElementConvergenceAsync(int elementId)
        {
            var element = await _context.ReferentielElements
                .FirstOrDefaultAsync(e => e.Id == elementId && e.IsActive);

            if (element == null)
                return false;

            try
            {
                var result = await AnalyzeElementAsync(elementId);
                var changed = element.HasConvergence != result.IsConvergent;

                if (changed)
                {
                    element.HasConvergence = result.IsConvergent;
                    await _context.SaveChangesAsync();
                }

                return changed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating convergence for element {ElementId}", elementId);
                return false;
            }
        }

        #region Private Helper Methods

        private bool CompareRuleDetails(ElementRule cnssRule, ElementRule dgiRule)
        {
            // For FULLY_EXEMPT and FULLY_SUBJECT, no details to compare
            if (cnssRule.ExemptionType == ExemptionType.FULLY_EXEMPT ||
                cnssRule.ExemptionType == ExemptionType.FULLY_SUBJECT)
            {
                return true;
            }

            try
            {
                var cnssJson = JsonDocument.Parse(cnssRule.RuleDetails);
                var dgiJson = JsonDocument.Parse(dgiRule.RuleDetails);

                return cnssRule.ExemptionType switch
                {
                    ExemptionType.CAPPED => CompareCapDetails(cnssJson, dgiJson),
                    ExemptionType.PERCENTAGE => ComparePercentageDetails(cnssJson, dgiJson),
                    ExemptionType.PERCENTAGE_CAPPED => ComparePercentageDetails(cnssJson, dgiJson) && CompareCapDetails(cnssJson, dgiJson),
                    ExemptionType.FORMULA => CompareFormulaDetails(cnssJson, dgiJson),
                    ExemptionType.FORMULA_CAPPED => CompareFormulaDetails(cnssJson, dgiJson) && CompareCapDetails(cnssJson, dgiJson),
                    ExemptionType.DUAL_CAP => CompareDualCapDetails(cnssJson, dgiJson),
                    ExemptionType.TIERED => CompareTieredDetails(cnssJson, dgiJson),
                    _ => false
                };
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private bool CompareCapDetails(JsonDocument cnss, JsonDocument dgi)
        {
            var cnssAmount = cnss.RootElement.TryGetProperty("capAmount", out var ca) ? ca.GetDecimal() : 0;
            var dgiAmount = dgi.RootElement.TryGetProperty("capAmount", out var da) ? da.GetDecimal() : 0;

            var cnssUnit = cnss.RootElement.TryGetProperty("capUnit", out var cu) ? cu.GetString() : "";
            var dgiUnit = dgi.RootElement.TryGetProperty("capUnit", out var du) ? du.GetString() : "";

            return cnssAmount == dgiAmount && cnssUnit == dgiUnit;
        }

        private bool ComparePercentageDetails(JsonDocument cnss, JsonDocument dgi)
        {
            var cnssPercent = cnss.RootElement.TryGetProperty("percentage", out var cp) ? cp.GetDecimal() : 0;
            var dgiPercent = dgi.RootElement.TryGetProperty("percentage", out var dp) ? dp.GetDecimal() : 0;

            var cnssBase = cnss.RootElement.TryGetProperty("baseReference", out var cb) ? cb.GetString() : "";
            var dgiBase = dgi.RootElement.TryGetProperty("baseReference", out var db) ? db.GetString() : "";

            return cnssPercent == dgiPercent && cnssBase == dgiBase;
        }

        private bool CompareFormulaDetails(JsonDocument cnss, JsonDocument dgi)
        {
            var cnssMultiplier = cnss.RootElement.TryGetProperty("multiplier", out var cm) ? cm.GetDecimal() : 0;
            var dgiMultiplier = dgi.RootElement.TryGetProperty("multiplier", out var dm) ? dm.GetDecimal() : 0;

            var cnssParam = cnss.RootElement.TryGetProperty("parameterCode", out var cp) ? cp.GetString() : "";
            var dgiParam = dgi.RootElement.TryGetProperty("parameterCode", out var dp) ? dp.GetString() : "";

            return cnssMultiplier == dgiMultiplier && cnssParam == dgiParam;
        }

        private bool CompareDualCapDetails(JsonDocument cnss, JsonDocument dgi)
        {
            var cnssFixed = cnss.RootElement.TryGetProperty("fixedCapAmount", out var cf) ? cf.GetDecimal() : 0;
            var dgiFixed = dgi.RootElement.TryGetProperty("fixedCapAmount", out var df) ? df.GetDecimal() : 0;

            var cnssPercent = cnss.RootElement.TryGetProperty("percentageCap", out var cp) ? cp.GetDecimal() : 0;
            var dgiPercent = dgi.RootElement.TryGetProperty("percentageCap", out var dp) ? dp.GetDecimal() : 0;

            var cnssLogic = cnss.RootElement.TryGetProperty("logic", out var cl) ? cl.GetString() : "";
            var dgiLogic = dgi.RootElement.TryGetProperty("logic", out var dl) ? dl.GetString() : "";

            return cnssFixed == dgiFixed && cnssPercent == dgiPercent && cnssLogic == dgiLogic;
        }

        private bool CompareTieredDetails(JsonDocument cnss, JsonDocument dgi)
        {
            if (!cnss.RootElement.TryGetProperty("tiers", out var cnssTiers) ||
                !dgi.RootElement.TryGetProperty("tiers", out var dgiTiers))
            {
                return false;
            }

            var cnssArray = cnssTiers.EnumerateArray().ToList();
            var dgiArray = dgiTiers.EnumerateArray().ToList();

            if (cnssArray.Count != dgiArray.Count)
                return false;

            for (int i = 0; i < cnssArray.Count; i++)
            {
                var cnssTier = cnssArray[i];
                var dgiTier = dgiArray[i];

                var cnssFrom = cnssTier.TryGetProperty("fromAmount", out var cf) ? cf.GetDecimal() : 0;
                var dgiFrom = dgiTier.TryGetProperty("fromAmount", out var df) ? df.GetDecimal() : 0;

                var cnssTo = cnssTier.TryGetProperty("toAmount", out var ct) ? (ct.ValueKind == JsonValueKind.Null ? (decimal?)null : ct.GetDecimal()) : null;
                var dgiTo = dgiTier.TryGetProperty("toAmount", out var dt) ? (dt.ValueKind == JsonValueKind.Null ? (decimal?)null : dt.GetDecimal()) : null;

                var cnssPercent = cnssTier.TryGetProperty("exemptPercent", out var cep) ? cep.GetDecimal() : 0;
                var dgiPercent = dgiTier.TryGetProperty("exemptPercent", out var dep) ? dep.GetDecimal() : 0;

                if (cnssFrom != dgiFrom || cnssTo != dgiTo || cnssPercent != dgiPercent)
                    return false;
            }

            return true;
        }

        private void CompareCapFields(JsonDocument cnss, JsonDocument dgi, List<FieldComparison> comparisons)
        {
            var cnssAmount = cnss.RootElement.TryGetProperty("capAmount", out var ca) ? ca.GetDecimal().ToString() : "N/A";
            var dgiAmount = dgi.RootElement.TryGetProperty("capAmount", out var da) ? da.GetDecimal().ToString() : "N/A";

            comparisons.Add(new FieldComparison
            {
                FieldName = "CapAmount",
                CnssValue = cnssAmount,
                DgiValue = dgiAmount,
                Matches = cnssAmount == dgiAmount
            });

            var cnssUnit = cnss.RootElement.TryGetProperty("capUnit", out var cu) ? cu.GetString() : "N/A";
            var dgiUnit = dgi.RootElement.TryGetProperty("capUnit", out var du) ? du.GetString() : "N/A";

            comparisons.Add(new FieldComparison
            {
                FieldName = "CapUnit",
                CnssValue = cnssUnit,
                DgiValue = dgiUnit,
                Matches = cnssUnit == dgiUnit
            });
        }

        private void ComparePercentageFields(JsonDocument cnss, JsonDocument dgi, List<FieldComparison> comparisons)
        {
            var cnssPercent = cnss.RootElement.TryGetProperty("percentage", out var cp) ? cp.GetDecimal().ToString() : "N/A";
            var dgiPercent = dgi.RootElement.TryGetProperty("percentage", out var dp) ? dp.GetDecimal().ToString() : "N/A";

            comparisons.Add(new FieldComparison
            {
                FieldName = "Percentage",
                CnssValue = cnssPercent,
                DgiValue = dgiPercent,
                Matches = cnssPercent == dgiPercent
            });

            var cnssBase = cnss.RootElement.TryGetProperty("baseReference", out var cb) ? cb.GetString() : "N/A";
            var dgiBase = dgi.RootElement.TryGetProperty("baseReference", out var db) ? db.GetString() : "N/A";

            comparisons.Add(new FieldComparison
            {
                FieldName = "BaseReference",
                CnssValue = cnssBase,
                DgiValue = dgiBase,
                Matches = cnssBase == dgiBase
            });
        }

        private void CompareFormulaFields(JsonDocument cnss, JsonDocument dgi, List<FieldComparison> comparisons)
        {
            var cnssMultiplier = cnss.RootElement.TryGetProperty("multiplier", out var cm) ? cm.GetDecimal().ToString() : "N/A";
            var dgiMultiplier = dgi.RootElement.TryGetProperty("multiplier", out var dm) ? dm.GetDecimal().ToString() : "N/A";

            comparisons.Add(new FieldComparison
            {
                FieldName = "Multiplier",
                CnssValue = cnssMultiplier,
                DgiValue = dgiMultiplier,
                Matches = cnssMultiplier == dgiMultiplier
            });

            var cnssParam = cnss.RootElement.TryGetProperty("parameterCode", out var cp) ? cp.GetString() : "N/A";
            var dgiParam = dgi.RootElement.TryGetProperty("parameterCode", out var dp) ? dp.GetString() : "N/A";

            comparisons.Add(new FieldComparison
            {
                FieldName = "ParameterCode",
                CnssValue = cnssParam,
                DgiValue = dgiParam,
                Matches = cnssParam == dgiParam
            });
        }

        private void CompareDualCapFields(JsonDocument cnss, JsonDocument dgi, List<FieldComparison> comparisons)
        {
            var cnssFixed = cnss.RootElement.TryGetProperty("fixedCapAmount", out var cf) ? cf.GetDecimal().ToString() : "N/A";
            var dgiFixed = dgi.RootElement.TryGetProperty("fixedCapAmount", out var df) ? df.GetDecimal().ToString() : "N/A";

            comparisons.Add(new FieldComparison
            {
                FieldName = "FixedCapAmount",
                CnssValue = cnssFixed,
                DgiValue = dgiFixed,
                Matches = cnssFixed == dgiFixed
            });

            var cnssPercent = cnss.RootElement.TryGetProperty("percentageCap", out var cp) ? cp.GetDecimal().ToString() : "N/A";
            var dgiPercent = dgi.RootElement.TryGetProperty("percentageCap", out var dp) ? dp.GetDecimal().ToString() : "N/A";

            comparisons.Add(new FieldComparison
            {
                FieldName = "PercentageCap",
                CnssValue = cnssPercent,
                DgiValue = dgiPercent,
                Matches = cnssPercent == dgiPercent
            });

            var cnssLogic = cnss.RootElement.TryGetProperty("logic", out var cl) ? cl.GetString() : "N/A";
            var dgiLogic = dgi.RootElement.TryGetProperty("logic", out var dl) ? dl.GetString() : "N/A";

            comparisons.Add(new FieldComparison
            {
                FieldName = "Logic",
                CnssValue = cnssLogic,
                DgiValue = dgiLogic,
                Matches = cnssLogic == dgiLogic
            });
        }

        private void CompareTieredFields(JsonDocument cnss, JsonDocument dgi, List<FieldComparison> comparisons)
        {
            if (!cnss.RootElement.TryGetProperty("tiers", out var cnssTiers) ||
                !dgi.RootElement.TryGetProperty("tiers", out var dgiTiers))
            {
                comparisons.Add(new FieldComparison
                {
                    FieldName = "Tiers",
                    CnssValue = "Missing",
                    DgiValue = "Missing",
                    Matches = false
                });
                return;
            }

            var cnssArray = cnssTiers.EnumerateArray().ToList();
            var dgiArray = dgiTiers.EnumerateArray().ToList();

            comparisons.Add(new FieldComparison
            {
                FieldName = "TierCount",
                CnssValue = cnssArray.Count.ToString(),
                DgiValue = dgiArray.Count.ToString(),
                Matches = cnssArray.Count == dgiArray.Count
            });

            // Compare each tier
            int maxCount = Math.Max(cnssArray.Count, dgiArray.Count);
            for (int i = 0; i < maxCount; i++)
            {
                var cnssTier = i < cnssArray.Count ? cnssArray[i] : default;
                var dgiTier = i < dgiArray.Count ? dgiArray[i] : default;

                comparisons.Add(new FieldComparison
                {
                    FieldName = $"Tier {i + 1}",
                    CnssValue = FormatTier(cnssTier),
                    DgiValue = FormatTier(dgiTier),
                    Matches = CompareTierJson(cnssTier, dgiTier)
                });
            }
        }

        private string FormatTier(JsonElement tier)
        {
            if (tier.ValueKind == JsonValueKind.Undefined)
                return "N/A";

            var from = tier.TryGetProperty("fromAmount", out var f) ? f.GetDecimal().ToString() : "0";
            var to = tier.TryGetProperty("toAmount", out var t) && t.ValueKind != JsonValueKind.Null ? t.GetDecimal().ToString() : "∞";
            var percent = tier.TryGetProperty("exemptPercent", out var p) ? p.GetDecimal().ToString() : "0";

            return $"{from} → {to} ({percent}%)";
        }

        private bool CompareTierJson(JsonElement cnss, JsonElement dgi)
        {
            if (cnss.ValueKind == JsonValueKind.Undefined || dgi.ValueKind == JsonValueKind.Undefined)
                return false;

            var cnssFrom = cnss.TryGetProperty("fromAmount", out var cf) ? cf.GetDecimal() : 0;
            var dgiFrom = dgi.TryGetProperty("fromAmount", out var df) ? df.GetDecimal() : 0;

            var cnssTo = cnss.TryGetProperty("toAmount", out var ct) && ct.ValueKind != JsonValueKind.Null ? ct.GetDecimal() : (decimal?)null;
            var dgiTo = dgi.TryGetProperty("toAmount", out var dt) && dt.ValueKind != JsonValueKind.Null ? dt.GetDecimal() : (decimal?)null;

            var cnssPercent = cnss.TryGetProperty("exemptPercent", out var cp) ? cp.GetDecimal() : 0;
            var dgiPercent = dgi.TryGetProperty("exemptPercent", out var dp) ? dp.GetDecimal() : 0;

            return cnssFrom == dgiFrom && cnssTo == dgiTo && cnssPercent == dgiPercent;
        }

        #endregion
    }
}
