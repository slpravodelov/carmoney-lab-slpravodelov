using CarMoneyLab.Domain;

namespace CarMoneyLab.Application;

public sealed record AssessApplicationCommand(string? Vin, int Year, int Mileage, int MarketValue, int RequestedAmount, int TermMonths, string? ApplicantRef);
public sealed record AssessmentResponse(int VehicleAge, decimal Ltv, string Decision, int ApprovedLimit);
public sealed record ApplicationSummary(int Id, string ApplicantRef, int RequestedAmount, int TermMonths, string Status, DateTime CreatedAt, VehicleSummary? Vehicle, DecisionSummary? Decision);
public sealed record ApplicationDetails(int Id, string ApplicantRef, int RequestedAmount, int TermMonths, string Status, DateTime CreatedAt, string? Vin, int? ProductionYear, int? MileageKm, int? MarketValue, decimal? Ltv, string? Decision, int? ApprovedLimit);
public sealed record VehicleSummary(string Vin, int ProductionYear);
public sealed record DecisionSummary(decimal Ltv, string Decision, int ApprovedLimit);

public interface IApplicationRepository
{
    Task<int> SaveAsync(LoanApplication application, AssessmentResult assessment, CancellationToken cancellationToken);
    Task<ApplicationDetails?> FindAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ApplicationSummary>> ListAsync(string? status, CancellationToken cancellationToken);
}
