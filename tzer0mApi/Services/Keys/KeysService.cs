using Npgsql;

namespace tzer0mApi.Services.Keys;

/// <summary>
/// Handles API key validation against the database.
/// </summary>
public class KeysService(IConfiguration configuration)
{
    /// <summary>
    /// Database connection string
    /// </summary>
    private readonly string ConnectionString = configuration.GetConnectionString("Robert1") ?? throw new InvalidOperationException("Robert1 connection string not configured");

    /// <summary>
    /// Checks if the given hashed key exists and is active in the database.
    /// </summary>
    /// <param name="hashedKey">The SHA256 hashed key to validate.</param>
    /// <returns>True if the key is valid and active, otherwise false.</returns>
    public async Task<bool> IsValidKeyAsync(string hashedKey)
    {
        // Connect to the database
        await using NpgsqlConnection conn = new(ConnectionString);
        await conn.OpenAsync();

        // Query to check if the hashed key exists and is active
        await using NpgsqlCommand cmd = new(@"
            SELECT COUNT(1) FROM ""ApiKeys""
            WHERE hashed_key = @hashedKey
            AND is_active = TRUE", conn);
        cmd.Parameters.AddWithValue("hashedKey", hashedKey);

        // Execute the query and check if any rows are returned
        object? result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result) > 0;
    }
}