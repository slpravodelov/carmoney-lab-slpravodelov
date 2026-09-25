namespace CarMoneyLab.Api;

public sealed class AssessmentRules
{
    public int VinLength { get; init; } = 17;
    public int MinVehicleYear { get; init; } = 1990;
    public int MaxVehicleAgeYears { get; init; } = 20;
    public int MaxMileageKm { get; init; } = 500000;
    public int MinAmount { get; init; } = 50_000;
    public int MaxAmount { get; init; } = 2_000_000;
    public int MinTermMonths { get; init; } = 3;
    public int MaxTermMonths { get; init; } = 48;
    public decimal ApproveMaxLtv { get; init; } = 60m;
    public decimal ReviewMaxLtv { get; init; } = 85m;
}

public sealed record ApplicationRequest(string? Vin, int Year, int Mileage, int MarketValue, int RequestedAmount, int TermMonths, string? ApplicantRef);
public sealed record ValidatedApplication(string Vin, int Year, int Mileage, int MarketValue, int RequestedAmount, int TermMonths);
public sealed record Assessment(int VehicleAge, decimal Ltv, string Decision, int ApprovedLimit, ValidatedApplication Input);

public sealed class ApplicationValidator(AssessmentRules rules)
{
    public Dictionary<string, string> Validate(ApplicationRequest request, out ValidatedApplication? application)
    {
        var errors = new Dictionary<string, string>();
        var vin = (request.Vin ?? string.Empty).Trim().ToUpperInvariant();
        if (vin.Length != rules.VinLength || vin.Any(c => !char.IsAsciiLetterOrDigit(c)) || vin.IndexOfAny(['I', 'O', 'Q']) >= 0)
            errors["vin"] = "VIN должен состоять из 17 символов A-Z и 0-9 без букв I, O, Q";

        var age = DateTime.UtcNow.Year - request.Year;
        if (request.Year < rules.MinVehicleYear)
            errors["year"] = $"Год выпуска не раньше {rules.MinVehicleYear}";
        else if (age < 0)
            errors["year"] = "Год выпуска не может быть в будущем";
        else if (age > rules.MaxVehicleAgeYears)
            errors["year"] = $"Возраст авто больше {rules.MaxVehicleAgeYears} лет";

        if (request.Mileage < 0 || request.Mileage > rules.MaxMileageKm)
            errors["mileage"] = $"Пробег от 0 до {rules.MaxMileageKm} км";
        if (request.MarketValue <= 0)
            errors["market_value"] = "Оценочная стоимость должна быть больше нуля";
        if (request.RequestedAmount < rules.MinAmount || request.RequestedAmount > rules.MaxAmount)
            errors["requested_amount"] = $"Сумма от {rules.MinAmount} до {rules.MaxAmount} рублей";
        if (request.TermMonths < rules.MinTermMonths || request.TermMonths > rules.MaxTermMonths)
            errors["term_months"] = $"Срок от {rules.MinTermMonths} до {rules.MaxTermMonths} месяцев";

        application = errors.Count == 0
            ? new ValidatedApplication(vin, request.Year, request.Mileage, request.MarketValue, request.RequestedAmount, request.TermMonths)
            : null;
        return errors;
    }
}

public sealed class AssessmentService(ApplicationValidator validator, AssessmentRules rules)
{
    public Dictionary<string, string> Assess(ApplicationRequest request, out Assessment? assessment)
    {
        var errors = validator.Validate(request, out var input);
        if (input is null)
        {
            assessment = null;
            return errors;
        }

        var ltv = Math.Round(input.RequestedAmount / (decimal)input.MarketValue * 100m, 2, MidpointRounding.AwayFromZero);
        // This intentionally preserves the PHP behavior: exactly 60.00 is review.
        var decision = ltv < rules.ApproveMaxLtv ? "approve" : ltv <= rules.ReviewMaxLtv ? "review" : "reject";
        assessment = new Assessment(DateTime.UtcNow.Year - input.Year, ltv, decision, decision == "approve" ? input.RequestedAmount : 0, input);
        return errors;
    }
}
