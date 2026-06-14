using Microsoft.AspNetCore.DataProtection;

namespace aiterate.energy.web.Services;

public interface IHomeWizardTokenProtector
{
    string? Protect(string? token);
    string? Unprotect(string? protectedToken);
    bool IsProtected(string? token);
}

public class HomeWizardTokenProtector(IDataProtectionProvider dataProtectionProvider) : IHomeWizardTokenProtector
{
    private const string Prefix = "dp:v1:";
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("HomeWizardP1Token");

    public string? Protect(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return Prefix + protector.Protect(token);
    }

    public string? Unprotect(string? protectedToken)
    {
        if (string.IsNullOrWhiteSpace(protectedToken))
        {
            return null;
        }

        if (!protectedToken.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return protectedToken;
        }

        return protector.Unprotect(protectedToken[Prefix.Length..]);
    }

    public bool IsProtected(string? token)
    {
        return !string.IsNullOrWhiteSpace(token) && token.StartsWith(Prefix, StringComparison.Ordinal);
    }
}
