using Payzen.Application.Common;

namespace Payzen.Tests.Unit;

/// <summary>
/// Tests du pattern ServiceResult — le contrat de retour de tous les services.
/// </summary>
public class ServiceResultTests
{
    [Fact]
    public void Ok_WithData_SuccessTrue()
    {
        var result = ServiceResult<string>.Ok("bonjour");

        result.Success.Should().BeTrue();
        result.Data.Should().Be("bonjour");
        result.Error.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Fail_WithMessage_SuccessFalse()
    {
        var result = ServiceResult<string>.Fail("Employé introuvable.");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Employé introuvable.");
        result.Data.Should().BeNull();
    }

    [Fact]
    public void OkVoid_Success()
    {
        var result = ServiceResult.Ok();

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void FailVoid_ContainsMessage()
    {
        var result = ServiceResult.Fail("Permission refusée.");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Permission refusée.");
    }
}
