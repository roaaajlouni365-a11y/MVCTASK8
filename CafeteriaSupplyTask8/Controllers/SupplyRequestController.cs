using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeteriaSupplyTask8.Data;
using CafeteriaSupplyTask8.Models;
using CafeteriaSupplyTask8.Services;
using CafeteriaSupplyTask8.ViewModels;

namespace CafeteriaSupplyTask8.Controllers;

[Authorize]
public class SupplyRequestController(ApplicationDbContext db, UserManager<ApplicationUser> users,
    SupplyRequestService service) : Controller
{
    private bool IsManager => User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.SuperAdmin);
    private string UserId => users.GetUserId(User)!;
    private IQueryable<SupplyRequest> VisibleRequests() => IsManager
        ? db.SupplyRequests : db.SupplyRequests.Where(r => r.UserId == UserId);

    [HttpGet]
    public async Task<IActionResult> Index() => View(await VisibleRequests().AsNoTracking().Include(r => r.User)
        .OrderByDescending(r => r.RequestDate).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var request = await VisibleRequests().AsNoTracking().Include(r => r.User)
            .Include(r => r.Items).ThenInclude(i => i.Product).SingleOrDefaultAsync(r => r.Id == id);
        return request == null ? NotFound() : View(request);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var form = new RequestFormViewModel(); await PopulateAsync(form); return View(form);
    }

    [HttpPost]
    public async Task<IActionResult> Create(RequestFormViewModel model)
    {
        model.Id = 0;
        return await SaveFormAsync(model, "Create");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var request = await db.SupplyRequests.AsNoTracking().Include(r => r.Items)
            .SingleOrDefaultAsync(r => r.Id == id && r.UserId == UserId);
        if (request == null) return NotFound();
        if (request.Status != RequestStatus.Pending) return Conflict("Only Pending requests can be edited.");
        var form = new RequestFormViewModel { Id = id, RowVersion = Convert.ToBase64String(request.RowVersion),
            Items = request.Items.Select(i => new RequestLineViewModel { ProductId = i.ProductId, Quantity = i.Quantity }).ToList() };
        await PopulateAsync(form); return View(form);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, RequestFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!await db.SupplyRequests.AnyAsync(r => r.Id == id && r.UserId == UserId)) return NotFound();
        return await SaveFormAsync(model, "Edit");
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var request = await db.SupplyRequests.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id && r.UserId == UserId);
        if (request == null) return NotFound();
        return request.Status != RequestStatus.Pending ? Conflict("Only Pending requests can be deleted.") : View(request);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, string rowVersion)
    {
        if (!await db.SupplyRequests.AnyAsync(r => r.Id == id && r.UserId == UserId)) return NotFound();
        try { await service.DeleteAsync(id, UserId, rowVersion); TempData["Success"] = "Request deleted."; }
        catch (RequestRuleException ex) { TempData["Error"] = ex.Message; }
        catch (DbUpdateException) { TempData["Error"] = "Request changed. Reload and try again."; }
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Managers), HttpPost]
    public Task<IActionResult> Approve(int id, string rowVersion) => DecideAsync(id, rowVersion, true);

    [Authorize(Roles = AppRoles.Managers), HttpPost]
    public Task<IActionResult> Reject(int id, string rowVersion) => DecideAsync(id, rowVersion, false);

    private async Task<IActionResult> DecideAsync(int id, string rowVersion, bool approve)
    {
        try { await service.DecideAsync(id, approve, rowVersion); TempData["Success"] = approve ? "Request approved; stock updated." : "Request rejected."; }
        catch (RequestRuleException ex) { TempData["Error"] = ex.Message; }
        catch (DbUpdateException) { TempData["Error"] = "Request or stock changed. Reload and try again."; }
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<IActionResult> SaveFormAsync(RequestFormViewModel model, string view)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var id = await service.SaveAsync(model, UserId);
                TempData["Success"] = "Request saved with Pending status.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (RequestRuleException ex) { ModelState.AddModelError("", ex.Message); }
            catch (DbUpdateException) { ModelState.AddModelError("", "Data changed while saving. Reload and try again."); }
        }
        await PopulateAsync(model);
        return View(view, model);
    }

    private async Task PopulateAsync(RequestFormViewModel form)
    {
        // Rebuild product descriptions/prices from the database, including on invalid POSTs.
        var quantities = form.Items.GroupBy(i => i.ProductId).ToDictionary(g => g.Key, g => g.First().Quantity);
        var products = await db.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
        form.Items = products.Select(p => new RequestLineViewModel { ProductId = p.Id, Name = p.Name,
            UnitPrice = p.Price, Available = p.Quantity, Quantity = quantities.GetValueOrDefault(p.Id) }).ToList();
        // Posted indices may be reordered by a forged request; do not reuse their field values.
        var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e =>
            string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid quantity or form value." : e.ErrorMessage).ToList();
        ModelState.Clear();
        foreach (var error in errors) ModelState.AddModelError("", error);
    }
}
