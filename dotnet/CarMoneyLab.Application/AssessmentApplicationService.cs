using CarMoneyLab.Domain;

namespace CarMoneyLab.Application;

public sealed class AssessmentApplicationService(ApplicationAssessment assessment)
{
    public IReadOnlyDictionary<string, string> Assess(AssessApplicationCommand command, out LoanApplication? application, out AssessmentResponse? response)
    {
        var errors = assessment.Validate(command.Vin, command.Year, command.Mileage, command.MarketValue, command.RequestedAmount, command.TermMonths);
        if (errors.Count > 0)
        {
            application = null;
            response = null;
            return errors;
        }

        application = new LoanApplication(
            command.Vin!.Trim().ToUpperInvariant(),
            command.Year,
            command.Mileage,
            command.MarketValue,
            command.RequestedAmount,
            command.TermMonths,
            string.IsNullOrWhiteSpace(command.ApplicantRef) ? "CL-ANON" : command.ApplicantRef);
        var result = assessment.Assess(application);
        response = new AssessmentResponse(result.VehicleAge, result.Ltv, result.Decision.ToString().ToLowerInvariant(), result.ApprovedLimit);
        return errors;
    }

    public AssessmentResult Calculate(LoanApplication application) => assessment.Assess(application);
}
