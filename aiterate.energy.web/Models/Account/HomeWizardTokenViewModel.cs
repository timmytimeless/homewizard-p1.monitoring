using System.ComponentModel.DataAnnotations;

namespace aiterate.energy.web.Models.Account;

public class HomeWizardTokenViewModel
{
    [Display(Name = "HomeWizard P1 token")]
    [DataType(DataType.Password)]
    public string? HomeWizardP1Token { get; set; }

    public bool HasHomeWizardP1Token { get; set; }

    public bool TokenNeedsReplacement { get; set; }
}
