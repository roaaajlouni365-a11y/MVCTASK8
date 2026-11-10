using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeteriaSupplyTask8.Data;
using CafeteriaSupplyTask8.Models;
using CafeteriaSupplyTask8.ViewModels;

namespace CafeteriaSupplyTask8.Controllers;

[Authorize(Roles = AppRoles.SuperAdmin)]
public class AdminController(UserManager<ApplicationUser> users, ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var rows = new List<UserRoleViewModel>();
        foreach (var user in await users.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync())
            rows.Add(await MapAsync(user));
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await users.FindByIdAsync(id);
        return user == null ? NotFound() : View(await MapAsync(user));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(string id, UserRoleViewModel model)
    {
        if (id != model.Id) return BadRequest();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await users.FindByIdAsync(id);
        if (user == null) return NotFound();
        model.FullName = user.FullName; model.Email = user.Email ?? "";
        if (id == users.GetUserId(User)) ModelState.AddModelError("", "You cannot change your own role.");
        if (!AppRoles.All.Contains(model.Role)) ModelState.AddModelError("Role", "Choose a valid role.");
        if (user.ConcurrencyStamp != model.ConcurrencyStamp) ModelState.AddModelError("", "User changed. Reload this page.");
        if (await IsLastSuperAdminAsync(user) && model.Role != AppRoles.SuperAdmin)
            ModelState.AddModelError("", "The last SuperAdmin cannot be demoted.");
        if (!ModelState.IsValid) return View(model);
        var currentRoles = await users.GetRolesAsync(user);
        if (!currentRoles.SequenceEqual(new[] { model.Role }))
        {
            var result = await users.RemoveFromRolesAsync(user, currentRoles);
            if (!Check(result)) return View(model);
            result = await users.AddToRoleAsync(user, model.Role);
            if (!Check(result)) return View(model);
            // Existing login cookies lose their old permissions on the next request.
            if (!Check(await users.UpdateSecurityStampAsync(user))) return View(model);
        }
        await transaction.CommitAsync();
        TempData["Success"] = "Role updated. The user must sign in again.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await users.FindByIdAsync(id);
        if (user == null) return NotFound();
        ViewBag.RequestCount = await db.SupplyRequests.CountAsync(r => r.UserId == id);
        return View(await MapAsync(user));
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(string id, string concurrencyStamp)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await users.FindByIdAsync(id);
        if (user == null) return NotFound();
        if (id == users.GetUserId(User)) TempData["Error"] = "You cannot delete your own account.";
        else if (await IsLastSuperAdminAsync(user)) TempData["Error"] = "The last SuperAdmin cannot be deleted.";
        else if (user.ConcurrencyStamp != concurrencyStamp) TempData["Error"] = "User changed. Review before deleting.";
        else
        {
            // SQL cascade removes the user's requests and items. Previously issued stock stays issued.
            var result = await users.DeleteAsync(user);
            if (result.Succeeded)
            {
                await transaction.CommitAsync();
                TempData["Success"] = "User and their requests deleted.";
            }
            else TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> IsLastSuperAdminAsync(ApplicationUser user) =>
        await users.IsInRoleAsync(user, AppRoles.SuperAdmin)
        && (await users.GetUsersInRoleAsync(AppRoles.SuperAdmin)).Count <= 1;

    private async Task<UserRoleViewModel> MapAsync(ApplicationUser user) => new()
    {
        Id = user.Id, FullName = user.FullName, Email = user.Email ?? "",
        Role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? AppRoles.User,
        ConcurrencyStamp = user.ConcurrencyStamp ?? ""
    };

    private bool Check(IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
        return result.Succeeded;
    }
}
