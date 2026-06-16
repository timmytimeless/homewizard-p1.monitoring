using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace aiterate.energy.web.Services.HomeWizard;

public static class HomeWizardCertificateValidator
{
    public static bool IsTrustedCertificate(
        X509Certificate? certificate,
        SslPolicyErrors sslPolicyErrors,
        string? pinnedCertificateSha256)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        if (certificate is null || string.IsNullOrWhiteSpace(pinnedCertificateSha256))
        {
            return false;
        }

        var certificateBytes = certificate.GetRawCertData();
        var hash = SHA256.HashData(certificateBytes);
        var actualSha256 = Convert.ToHexString(hash);
        var expectedSha256 = pinnedCertificateSha256
            .Replace(":", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Trim();

        return string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase);
    }
}
