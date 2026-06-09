using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Npgsql;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Get args
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: KeyGenerator <key-name>");
            return;
        }
        string keyName = args[0];

        // Load configuration
        IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false).Build();
        string connectionString = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Postgres connection string not configured");

        // Generate key
        Console.WriteLine("Generating key...");
        Guid guid = Guid.NewGuid();

        // Hash key
        Console.WriteLine("Hashing key...");
        byte[] keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(guid.ToString()));
        string hashedKey = Convert.ToBase64String(keyBytes);

        // Save to database
        Console.WriteLine("Saving key to database...");
        await using NpgsqlConnection conn = new(connectionString);
        await conn.OpenAsync();
        await using NpgsqlCommand cmd = new(@"
            INSERT INTO ""ApiKeys"" (name, hashed_key)
            VALUES (@name, @hashedKey)", conn);
        cmd.Parameters.AddWithValue("name", keyName);
        cmd.Parameters.AddWithValue("hashedKey", hashedKey);
        await cmd.ExecuteNonQueryAsync();

        // Output key to console
        Console.WriteLine("Key generated and saved to database.");
        Console.WriteLine($"Name: {keyName}");
        Console.WriteLine($"Key:  {guid}");
        Console.WriteLine("Store this key securely — it cannot be retrieved again.");
    }
}