using aiterate.energy.web.Models.Account;
using aiterate.energy.web.Models.Identity;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using aiterate.energy.web.Services;

namespace aiterate.energy.web.Controllers;

public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IHomeWizardTokenProtector homeWizardTokenProtector) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is locked. Try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            HomeWizardP1Token = homeWizardTokenProtector.Protect(model.HomeWizardP1Token)
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToLocal(model.ReturnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> HomeWizardToken()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        if (!await TryEncryptLegacyHomeWizardToken(user))
        {
            return View(new HomeWizardTokenViewModel
            {
                HasHomeWizardP1Token = false,
                TokenNeedsReplacement = true
            });
        }

        return View(new HomeWizardTokenViewModel { HasHomeWizardP1Token = !string.IsNullOrWhiteSpace(user.HomeWizardP1Token) });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HomeWizardToken(HomeWizardTokenViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        if (!string.IsNullOrWhiteSpace(model.HomeWizardP1Token))
        {
            user.HomeWizardP1Token = homeWizardTokenProtector.Protect(model.HomeWizardP1Token);
        }

        var result = await userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            ViewData["StatusMessage"] = "HomeWizard P1 token saved.";
            return View(new HomeWizardTokenViewModel { HasHomeWizardP1Token = !string.IsNullOrWhiteSpace(user.HomeWizardP1Token) });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    private async Task<bool> TryEncryptLegacyHomeWizardToken(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.HomeWizardP1Token))
        {
            return true;
        }

        if (homeWizardTokenProtector.IsProtected(user.HomeWizardP1Token))
        {
            try
            {
                _ = homeWizardTokenProtector.Unprotect(user.HomeWizardP1Token);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        user.HomeWizardP1Token = homeWizardTokenProtector.Protect(user.HomeWizardP1Token);
        await userManager.UpdateAsync(user);
        return true;
    }
}
