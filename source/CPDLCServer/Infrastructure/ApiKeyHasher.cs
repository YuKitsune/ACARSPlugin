using System.Security.Cryptography;
using System.Text;

namespace CPDLCServer.Infrastructure;

public static class ApiKeyHasher
{
    public static string HashApiKey(string apiKey)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hashBytes);
    }
}
