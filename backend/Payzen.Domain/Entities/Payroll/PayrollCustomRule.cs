using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Payroll;

public class PayrollCustomRule : BaseEntity
{
    public int CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DslSnippet { get; set; } = string.Empty;
    public string GeneratedFilePath { get; set; } = string.Empty;
}
