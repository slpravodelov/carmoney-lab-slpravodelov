using CarMoneyLab.Application;
using CarMoneyLab.Domain;
using CarMoneyLab.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower);
var rules = builder.Configuration.GetSection("Rules").Get<AssessmentRules>() ?? new AssessmentRules();
builder.Services.AddSingleton(rules);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ApplicationAssessment>();
builder.Services.AddSingleton<AssessmentApplicationService>();
var connectionString = builder.Configuration.GetConnectionString("MySql") ?? throw new InvalidOperationException("ConnectionStrings:MySql is required.");
builder.Services.AddSingleton<IApplicationRepository>(new MySqlApplicationRepository(connectionString));

var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "carmoney-lab" }));

app.MapPost("/api/ltv", (AssessmentRequest request, AssessmentApplicationService service) =>
{
    var errors = service.Assess(request.ToCommand(), out _, out var assessment);
    return assessment is null ? Results.UnprocessableEntity(new { errors }) : Results.Ok(assessment);
});

app.MapPost("/api/applications", async (AssessmentRequest request, AssessmentApplicationService service, IApplicationRepository repository, CancellationToken cancellationToken) =>
{
    var errors = service.Assess(request.ToCommand(), out var application, out var assessment);
    if (application is null || assessment is null) return Results.UnprocessableEntity(new { errors });
    var domainAssessment = service.Calculate(application);
    var id = await repository.SaveAsync(application, domainAssessment, cancellationToken);
    return Results.Created($"/api/applications/{id}", new { id, vehicle_age = assessment.VehicleAge, ltv = assessment.Ltv, decision = assessment.Decision, approved_limit = assessment.ApprovedLimit });
});

app.MapGet("/api/applications", async (string? status, IApplicationRepository repository, CancellationToken cancellationToken) => Results.Ok(new { items = await repository.ListAsync(status, cancellationToken) }));
app.MapGet("/api/applications/{id:int}", async (int id, IApplicationRepository repository, CancellationToken cancellationToken) =>
{
    object response = (object?)await repository.FindAsync(id, cancellationToken) ?? new { };
    return Results.Ok(response);
});
app.Run();

public sealed record AssessmentRequest(string? Vin, int Year, int Mileage, int MarketValue, int RequestedAmount, int TermMonths, string? ApplicantRef)
{
    public AssessApplicationCommand ToCommand() => new(Vin, Year, Mileage, MarketValue, RequestedAmount, TermMonths, ApplicantRef);
}

public partial class Program;
