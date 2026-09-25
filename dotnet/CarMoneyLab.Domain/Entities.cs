namespace CarMoneyLab.Domain;

public sealed record LoanApplication(
    string Vin,
    int ProductionYear,
    int MileageKm,
    int MarketValue,
    int RequestedAmount,
    int TermMonths,
    string ApplicantRef);

public sealed record AssessmentResult(int VehicleAge, decimal Ltv, Decision Decision, int ApprovedLimit);

public enum Decision
{
    Approve,
    Review,
    Reject
}
