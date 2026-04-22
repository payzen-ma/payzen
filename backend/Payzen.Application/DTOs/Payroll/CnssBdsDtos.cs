namespace Payzen.Application.DTOs.Payroll;

public class CnssBdsGenerationResultDto
{
    public byte[] Content { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = [];
}
