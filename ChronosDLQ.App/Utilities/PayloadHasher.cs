using System.Security.Cryptography;
using System.Text;

namespace ChronosDLQ.App.Utilities;

public static class PayloadHasher
{
    public static string Sha256(string payload)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        var hashedBytes = SHA256.HashData(bytes);

        return Convert.ToHexString(hashedBytes).ToLowerInvariant();
    }
}
