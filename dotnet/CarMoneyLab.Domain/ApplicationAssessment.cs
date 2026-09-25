namespace CarMoneyLab.Domain;

public sealed class ApplicationAssessment(AssessmentRules rules, TimeProvider clock)
{
    public IReadOnlyDictionary<string, string> Validate(
        string? vinValue,
        int year,
        int mileage,
        int marketValue,
        int requestedAmount,
        int termMonths)
    {
        var errors = new Dictionary<string, string>();
        var vin = (vinValue ?? string.Empty).Trim().ToUpperInvariant();
        if (vin.Length != rules.VinLength || vin.Any(c => !char.IsAsciiLetterOrDigit(c)) || vin.IndexOfAny(['I', 'O', 'Q']) >= 0)
            errors["vin"] = "VIN должен состоять из 17 символов A-Z и 0-9 без букв I, O, Q";

        var age = clock.GetUtcNow().Year - year;
        if (year < rules.MinVehicleYear)
            errors["year"] = $"Год выпуска не раньше {rules.MinVehicleYear}";
        else if (age < 0)
            errors["year"] = "Год выпуска не может быть в будущем";
        else if (age > rules.MaxVehicleAgeYears)
            errors["year"] = $"Возраст авто больше {rules.MaxVehicleAgeYears} лет";
        if (mileage < 0 || mileage > rules.MaxMileageKm)
            errors["mileage"] = $"Пробег от 0 до {rules.MaxMileageKm} км";
        if (marketValue <= 0)
            errors["market_value"] = "Оценочная стоимость должна быть больше нуля";
        if (requestedAmount < rules.MinAmount || requestedAmount > rules.MaxAmount)
            errors["requested_amount"] = $"Сумма от {rules.MinAmount} до {rules.MaxAmount} рублей";
        if (termMonths < rules.MinTermMonths || termMonths > rules.MaxTermMonths)
            errors["term_months"] = $"Срок от {rules.MinTermMonths} до {rules.MaxTermMonths} месяцев";
        return errors;
    }

    public AssessmentResult Assess(LoanApplication application)
    {
        var ltv = Math.Round(application.RequestedAmount / (decimal)application.MarketValue * 100m, 2, MidpointRounding.AwayFromZero);
        // Exact 60.00 is review to preserve the existing PHP service behavior.
        var decision = ltv < rules.ApproveMaxLtv ? Decision.Approve : ltv <= rules.ReviewMaxLtv ? Decision.Review : Decision.Reject;
        return new AssessmentResult(
            clock.GetUtcNow().Year - application.ProductionYear,
            ltv,
            decision,
            decision == Decision.Approve ? application.RequestedAmount : 0);
    }
}
