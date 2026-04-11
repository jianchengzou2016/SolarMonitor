using System.Security.Cryptography;
using System.Text;

namespace SolarMonitor.FoxEss;

public static class FoxEssSignatureBuilder
{
    public static string CreateSignature(string signaturePath, string apiKey, string timestamp)
    {
        var payload = $"{signaturePath}\\r\\n{apiKey}\\r\\n{timestamp}";
        var bytes = Encoding.UTF8.GetBytes(payload);
        var hash = MD5.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
