using CarMoneyLab.Application;
using CarMoneyLab.Domain;
using MySqlConnector;

namespace CarMoneyLab.Infrastructure;

public sealed class MySqlApplicationRepository(string connectionString) : IApplicationRepository
{
    public async Task<int> SaveAsync(LoanApplication application, AssessmentResult assessment, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var id = await InsertAsync(connection, transaction, "INSERT INTO applications (applicant_ref, requested_amount, term_months, status) VALUES (@ref, @amount, @term, 'decided')", [new("@ref", application.ApplicantRef), new("@amount", application.RequestedAmount), new("@term", application.TermMonths)], cancellationToken);
            await ExecuteAsync(connection, transaction, "INSERT INTO vehicles (application_id, vin, production_year, mileage_km, market_value) VALUES (@id, @vin, @year, @mileage, @value)", [new("@id", id), new("@vin", application.Vin), new("@year", application.ProductionYear), new("@mileage", application.MileageKm), new("@value", application.MarketValue)], cancellationToken);
            await ExecuteAsync(connection, transaction, "INSERT INTO decisions (application_id, ltv, decision, approved_limit) VALUES (@id, @ltv, @decision, @limit)", [new("@id", id), new("@ltv", assessment.Ltv), new("@decision", assessment.Decision.ToString().ToLowerInvariant()), new("@limit", assessment.ApprovedLimit)], cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ApplicationDetails?> FindAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT a.id, a.applicant_ref, a.requested_amount, a.term_months, a.status, a.created_at, v.vin, v.production_year, v.mileage_km, v.market_value, d.ltv, d.decision, d.approved_limit FROM applications a LEFT JOIN vehicles v ON v.application_id = a.id LEFT JOIN decisions d ON d.application_id = a.id WHERE a.id = @id";
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadDetails(reader) : null;
    }

    public async Task<IReadOnlyList<ApplicationSummary>> ListAsync(string? status, CancellationToken cancellationToken)
    {
        const string sql = "SELECT a.id, a.applicant_ref, a.requested_amount, a.term_months, a.status, a.created_at, v.vin, v.production_year, d.ltv, d.decision, d.approved_limit FROM applications a LEFT JOIN vehicles v ON v.application_id = a.id LEFT JOIN decisions d ON d.application_id = a.id WHERE (@status IS NULL OR @status = '' OR a.status = @status) ORDER BY a.id DESC LIMIT 50";
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@status", status);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<ApplicationSummary>();
        while (await reader.ReadAsync(cancellationToken))
            items.Add(new ApplicationSummary(reader.GetInt32("id"), reader.GetString("applicant_ref"), reader.GetInt32("requested_amount"), reader.GetInt16("term_months"), reader.GetString("status"), reader.GetDateTime("created_at"), reader.IsDBNull(reader.GetOrdinal("vin")) ? null : new VehicleSummary(reader.GetString("vin"), reader.GetInt16("production_year")), reader.IsDBNull(reader.GetOrdinal("decision")) ? null : new DecisionSummary(reader.GetDecimal("ltv"), reader.GetString("decision"), reader.GetInt32("approved_limit"))));
        return items;
    }

    private static ApplicationDetails ReadDetails(MySqlDataReader reader) => new(reader.GetInt32("id"), reader.GetString("applicant_ref"), reader.GetInt32("requested_amount"), reader.GetInt16("term_months"), reader.GetString("status"), reader.GetDateTime("created_at"), reader.IsDBNull(reader.GetOrdinal("vin")) ? null : reader.GetString("vin"), reader.IsDBNull(reader.GetOrdinal("production_year")) ? null : reader.GetInt16("production_year"), reader.IsDBNull(reader.GetOrdinal("mileage_km")) ? null : reader.GetInt32("mileage_km"), reader.IsDBNull(reader.GetOrdinal("market_value")) ? null : reader.GetInt32("market_value"), reader.IsDBNull(reader.GetOrdinal("ltv")) ? null : reader.GetDecimal("ltv"), reader.IsDBNull(reader.GetOrdinal("decision")) ? null : reader.GetString("decision"), reader.IsDBNull(reader.GetOrdinal("approved_limit")) ? null : reader.GetInt32("approved_limit"));

    private static async Task<int> InsertAsync(MySqlConnection connection, MySqlTransaction transaction, string sql, MySqlParameter[] parameters, CancellationToken cancellationToken)
    {
        await ExecuteAsync(connection, transaction, sql, parameters, cancellationToken);
        await using var idCommand = new MySqlCommand("SELECT LAST_INSERT_ID()", connection, transaction);
        return Convert.ToInt32(await idCommand.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task ExecuteAsync(MySqlConnection connection, MySqlTransaction transaction, string sql, MySqlParameter[] parameters, CancellationToken cancellationToken)
    {
        await using var command = new MySqlCommand(sql, connection, transaction);
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
