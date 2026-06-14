using Microsoft.AspNetCore.Identity;

namespace aiterate.energy.web.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string? HomeWizardP1Token { get; set; }
}
