namespace CarMoneyLab.Domain;

public sealed class AssessmentRules
{
    public int VinLength { get; init; } = 17;
    public int MinVehicleYear { get; init; } = 1990;
    public int MaxVehicleAgeYears { get; init; } = 20;
    public int MaxMileageKm { get; init; } = 500_000;
    public int MinAmount { get; init; } = 50_000;
    public int MaxAmount { get; init; } = 2_000_000;
    public int MinTermMonths { get; init; } = 3;
    public int MaxTermMonths { get; init; } = 48;
    public decimal ApproveMaxLtv { get; init; } = 60m;
    public decimal ReviewMaxLtv { get; init; } = 85m;
}
