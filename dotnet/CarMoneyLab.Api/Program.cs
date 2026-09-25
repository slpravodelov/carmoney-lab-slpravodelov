using CarMoneyLab.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower);
var rules = builder.Configuration.GetSection("Rules").Get<AssessmentRules>() ?? new AssessmentRules();
builder.Services.AddSingleton(rules);
builder.Services.AddSingleton<ApplicationValidator>();
builder.Services.AddSingleton<AssessmentService>();
var connectionString = builder.Configuration.GetConnectionString("MySql") ?? throw new InvalidOperationException("ConnectionStrings:MySql is required.");
builder.Services.AddSingleton(new ApplicationRepository(connectionString));

var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "carmoney-lab" }));

app.MapPost("/api/ltv", (ApplicationRequest request, AssessmentService service) =>
{
    var errors = service.Assess(request, out var assessment);
    return assessment is null ? Results.UnprocessableEntity(new { errors }) : Results.Ok(ToResponse(assessment));
});

app.MapPost("/api/applications", async (ApplicationRequest request, AssessmentService service, ApplicationRepository repository, CancellationToken cancellationToken) =>
{
    var errors = service.Assess(request, out var assessment);
    if (assessment is null) return Results.UnprocessableEntity(new { errors });
    var id = await repository.SaveAsync(string.IsNullOrWhiteSpace(request.ApplicantRef) ? "CL-ANON" : request.ApplicantRef, assessment, cancellationToken);
    return Results.Created($"/api/applications/{id}", new { id, vehicle_age = assessment.VehicleAge, ltv = assessment.Ltv, decision = assessment.Decision, approved_limit = assessment.ApprovedLimit });
});

app.MapGet("/api/applications", async (string? status, ApplicationRepository repository, CancellationToken cancellationToken) => Results.Ok(new { items = await repository.ListAsync(status, cancellationToken) }));
app.MapGet("/api/applications/{id:int}", async (int id, ApplicationRepository repository, CancellationToken cancellationToken) =>
{
    object response = (object?)await repository.FindAsync(id, cancellationToken) ?? new { };
    return Results.Ok(response);
});
app.Run();

static object ToResponse(Assessment assessment) => new { vehicle_age = assessment.VehicleAge, ltv = assessment.Ltv, decision = assessment.Decision, approved_limit = assessment.ApprovedLimit };

public partial class Program;
