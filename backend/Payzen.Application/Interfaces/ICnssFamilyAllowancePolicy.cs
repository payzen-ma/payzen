using Payzen.Domain.Entities.Payroll;

namespace Payzen.Application.Interfaces;

public interface ICnssFamilyAllowancePolicy
{
    int ResolveDependentChildren(PayrollResult payroll);
    long ComputeFamilyAllowanceToPayCentimes(PayrollResult payroll, int dependentChildren);
    long ComputeFamilyAllowanceToDeductCentimes(PayrollResult payroll);
    string ResolveSituation(PayrollResult payroll);
    long ComputeFamilyAllowanceToReverseCentimes(string situation, long afNetCentimes);
}
