using MySqlConnector;

namespace CarMoneyLab.Api;

public sealed class ApplicationRepository(string connectionString)
{
    public async Task<int> SaveAsync(string applicantRef, Assessment assessment, CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var applicationId = await ExecuteInsertAsync(connection, transaction,
                "INSERT INTO applications (applicant_ref, requested_amount, term_months, status) VALUES (@ref, @amount, @term, 'decided')",
                [new("@ref", applicantRef), new("@amount", assessment.Input.RequestedAmount), new("@term", assessment.Input.TermMonths)], cancellationToken);
            await ExecuteAsync(connection, transaction,
                "INSERT INTO vehicles (application_id, vin, production_year, mileage_km, market_value) VALUES (@id, @vin, @year, @mileage, @value)",
                [new("@id", applicationId), new("@vin", assessment.Input.Vin), new("@year", assessment.Input.Year), new("@mileage", assessment.Input.Mileage), new("@value", assessment.Input.MarketValue)], cancellationToken);
            await ExecuteAsync(connection, transaction,
                "INSERT INTO decisions (application_id, ltv, decision, approved_limit) VALUES (@id, @ltv, @decision, @limit)",
                [new("@id", applicationId), new("@ltv", assessment.Ltv), new("@decision", assessment.Decision), new("@limit", assessment.ApprovedLimit)], cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return applicationId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Dictionary<string, object?>?> FindAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT a.id, a.applicant_ref, a.requested_amount, a.term_months, a.status, a.created_at, v.vin, v.production_year, v.mileage_km, v.market_value, d.ltv, d.decision, d.approved_limit FROM applications a LEFT JOIN vehicles v ON v.application_id = a.id LEFT JOIN decisions d ON d.application_id = a.id WHERE a.id = @id";
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        var row = new Dictionary<string, object?>();
        for (var i = 0; i < reader.FieldCount; i++) row[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
        return row;
    }

    public async Task<List<Dictionary<string, object?>>> ListAsync(string? status, CancellationToken cancellationToken)
    {
        const string sql = "SELECT a.id, a.applicant_ref, a.requested_amount, a.term_months, a.status, a.created_at, v.vin, v.production_year, d.ltv, d.decision, d.approved_limit FROM applications a LEFT JOIN vehicles v ON v.application_id = a.id LEFT JOIN decisions d ON d.application_id = a.id WHERE (@status IS NULL OR @status = '' OR a.status = @status) ORDER BY a.id DESC LIMIT 50";
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@status", status);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var item = new Dictionary<string, object?>
            {
                ["id"] = reader.GetInt32("id"), ["applicant_ref"] = reader.GetString("applicant_ref"), ["requested_amount"] = reader.GetInt32("requested_amount"),
                ["term_months"] = reader.GetInt16("term_months"), ["status"] = reader.GetString("status"), ["created_at"] = reader.GetDateTime("created_at"),
                ["vehicle"] = reader.IsDBNull(reader.GetOrdinal("vin")) ? null : new { vin = reader.GetString("vin"), production_year = reader.GetInt16("production_year") },
                ["decision"] = reader.IsDBNull(reader.GetOrdinal("decision")) ? null : new { ltv = reader.GetDecimal("ltv"), decision = reader.GetString("decision"), approved_limit = reader.GetInt32("approved_limit") }
            };
            items.Add(item);
        }
        return items;
    }

    private static async Task<int> ExecuteInsertAsync(MySqlConnection connection, MySqlTransaction transaction, string sql, MySqlParameter[] parameters, CancellationToken cancellationToken)
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
