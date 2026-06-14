using System.ComponentModel.DataAnnotations;

namespace aiterate.energy.web.Models.Account;

public class RegisterViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "HomeWizard P1 token")]
    [DataType(DataType.Password)]
    public string? HomeWizardP1Token { get; set; }

    public string? ReturnUrl { get; set; }
}
