using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CafeteriaSupplyTask8.Data;
using CafeteriaSupplyTask8.Models;
using CafeteriaSupplyTask8.ViewModels;

namespace CafeteriaSupplyTask8.Controllers;

public class AccountController(UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn, ApplicationDbContext db) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var email = model.Email.Trim();
        var user = new ApplicationUser { FullName = model.FullName.Trim(), Email = email, UserName = email };
        // Account and automatic User membership are committed together.
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            var result = await users.CreateAsync(user, model.Password);
            if (!result.Succeeded) { AddErrors(result); return View(model); }
            result = await users.AddToRoleAsync(user, AppRoles.User);
            if (!result.Succeeded) { AddErrors(result); return View(model); }
            await transaction.CommitAsync();
        }
        await signIn.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "SupplyRequest");
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await signIn.PasswordSignInAsync(model.Email.Trim(), model.Password,
            model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
            return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl!) : RedirectToAction("Index", "Home");
        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Too many unsuccessful attempts. Try again in five minutes."
            : "Incorrect email or password.");
        return View(model);
    }

    [Authorize, HttpPost]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous, HttpGet]
    public IActionResult AccessDenied() { Response.StatusCode = 403; return View(); }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
    }
}
