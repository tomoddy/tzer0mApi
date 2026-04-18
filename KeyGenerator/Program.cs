using System.Security.Cryptography;
using System.Text;

internal class Program
{
    private static void Main(string[] args)
    {
        // Generate key
        Console.WriteLine("Generating key...");
        Guid guid = Guid.NewGuid();

        // Hash key
        Console.WriteLine("Hashing key...");
        byte[] keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(guid.ToString()));
        string key = Convert.ToBase64String(keyBytes);

        // Write hashed key to file
        Console.WriteLine("Saving key to file...");
        File.AppendAllLines(args[0], [key]);

        // Output key to console
        Console.WriteLine("Key generated and saved to file.");
        Console.WriteLine("Key: " + guid.ToString());
    }
}