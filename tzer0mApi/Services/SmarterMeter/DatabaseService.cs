using Npgsql;
using tzer0mApi.Models;

namespace tzer0mApi.Services.SmarterMeter;

/// <summary>
/// Handles all PostgreSQL database operations for meter readings.
/// </summary>
/// <remarks>
/// Initialises the service with the connection string from configuration.
/// </remarks>
public class DatabaseService(ILogger<DatabaseService> logger, IConfiguration configuration)
{
    /// <summary>
    /// Connection string
    /// </summary>
    private readonly string ConnectionString = configuration.GetConnectionString("SmarterMeter") ?? throw new InvalidOperationException("SmarterMeter connection string not configured");

    /// <summary>
    /// Configuration
    /// </summary>
    private readonly ILogger<DatabaseService> Logger = logger;

    /// <summary>
    /// Opens a connection, executes a query with the given parameters, and returns a data reader.
    /// The connection is tied to the reader's lifetime and disposed when the reader is disposed.
    /// </summary>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="parameters">The parameters to bind to the query.</param>
    private async Task<NpgsqlDataReader> ExecuteReaderAsync(string sql, params NpgsqlParameter[] parameters)
    {
        NpgsqlConnection conn = new(ConnectionString);
        await conn.OpenAsync();

        NpgsqlCommand cmd = new(sql, conn);
        cmd.Parameters.AddRange(parameters);

        // CommandBehavior.CloseConnection ensures the connection is closed when the reader is disposed
        return await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection);
    }

    /// <summary>
    /// Inserts a new meter reading into the database and returns it with the generated id and recorded_at timestamp.
    /// </summary>
    /// <param name="reading">The reading to insert.</param>
    /// <returns>The inserted <see cref="MeterReading"/> with id and recorded_at populated.</returns>
    public async Task<MeterReading> InsertReadingAsync(MeterReading reading)
    {
        // Use parameterized query to prevent SQL injection
        await using NpgsqlDataReader reader = await ExecuteReaderAsync(@"
            INSERT INTO ""Electricity"" (value, raw_text, confidence, image_path, captured_at, recorded_at)
            VALUES (@value, @rawText, @confidence, @imagePath, @capturedAt, NOW())
            RETURNING id, recorded_at",
            new NpgsqlParameter("value", reading.Value),
            new NpgsqlParameter("rawText", (object?)reading.RawText ?? DBNull.Value),
            new NpgsqlParameter("confidence", reading.Confidence),
            new NpgsqlParameter("imagePath", (object?)reading.ImagePath ?? DBNull.Value),
            new NpgsqlParameter("capturedAt", reading.CapturedAt)
        );

        // Read the generated id and recorded_at timestamp
        await reader.ReadAsync();

        // Populate the reading with the generated values
        reading.Id = reader.GetInt64(0);
        reading.RecordedAt = reader.GetDateTime(1);
        if (Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation("Saved reading {Id}: {Value} kWh", reading.Id, reading.Value);

        // Return the reading with all properties populated
        return reading;
    }

    /// <summary>
    /// Returns the most recent meter readings ordered by captured_at descending.
    /// </summary>
    /// <param name="count">The maximum number of readings to return. Defaults to 48.</param>
    public async Task<IEnumerable<MeterReading>> GetRecentReadingsAsync(int count = 48)
    {
        // Use parameterized query to prevent SQL injection
        await using NpgsqlDataReader reader = await ExecuteReaderAsync(@"
            SELECT id, value, raw_text, confidence, image_path, captured_at, recorded_at
            FROM ""Electricity""
            ORDER BY captured_at DESC
            LIMIT @count",
            new NpgsqlParameter("count", count)
        );

        // Read all results into a list
        List<MeterReading> readings = [];
        while (await reader.ReadAsync())
        {
            readings.Add(new MeterReading
            {
                Id = reader.GetInt64(0),
                Value = reader.GetDecimal(1),
                RawText = reader.IsDBNull(2) ? null : reader.GetString(2),
                Confidence = reader.GetFloat(3),
                ImagePath = reader.IsDBNull(4) ? null : reader.GetString(4),
                CapturedAt = reader.GetDateTime(5),
                RecordedAt = reader.GetDateTime(6)
            });
        }
        return readings;
    }
}