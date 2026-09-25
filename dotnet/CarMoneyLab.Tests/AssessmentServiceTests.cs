using CarMoneyLab.Application;
using CarMoneyLab.Domain;
using Xunit;

namespace CarMoneyLab.Tests;

public sealed class AssessmentServiceTests
{
    private readonly AssessmentApplicationService service;

    public AssessmentServiceTests()
    {
        var rules = new AssessmentRules();
        service = new AssessmentApplicationService(new ApplicationAssessment(rules, TimeProvider.System));
    }

    [Fact]
    public void Assess_ReturnsApproveAndRequestedAmountBelowApproveThreshold()
    {
        var errors = service.Assess(ValidRequest(requestedAmount: 450_000), out _, out var result);

        Assert.Empty(errors);
        Assert.NotNull(result);
        Assert.Equal(50m, result.Ltv);
        Assert.Equal("approve", result.Decision);
        Assert.Equal(450_000, result.ApprovedLimit);
    }

    [Fact]
    public void Assess_ReturnsReviewAtApproveBoundaryToMatchPhpService()
    {
        var errors = service.Assess(ValidRequest(requestedAmount: 600_000), out _, out var result);

        Assert.Empty(errors);
        Assert.NotNull(result);
        Assert.Equal("review", result.Decision);
        Assert.Equal(0, result.ApprovedLimit);
    }

    [Fact]
    public void Assess_CollectsAllValidationErrors()
    {
        var errors = service.Assess(ValidRequest(vin: "BAD", marketValue: 0, termMonths: 120), out _, out var result);

        Assert.Null(result);
        Assert.Equal(["vin", "market_value", "term_months"], errors.Keys);
    }

    private static AssessApplicationCommand ValidRequest(string vin = "XTA21099998765432", int marketValue = 900_000, int requestedAmount = 450_000, int termMonths = 24) =>
        new(vin, DateTime.UtcNow.Year - 5, 84_000, marketValue, requestedAmount, termMonths, null);
}
