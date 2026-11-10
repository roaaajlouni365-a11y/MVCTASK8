using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeteriaSupplyTask8.Data;
using CafeteriaSupplyTask8.Models;
using CafeteriaSupplyTask8.ViewModels;

namespace CafeteriaSupplyTask8.Controllers;

[Authorize]
public class ProductController(ApplicationDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await db.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
        return product == null ? NotFound() : View(product);
    }

    [Authorize(Roles = AppRoles.Managers), HttpGet]
    public IActionResult Create() => View(new ProductViewModel());

    [Authorize(Roles = AppRoles.Managers), HttpPost]
    public async Task<IActionResult> Create(ProductViewModel model)
    {
        ValidatePrice(model.Price);
        if (!ModelState.IsValid) return View(model);
        db.Products.Add(new Product { Name = model.Name.Trim(), Description = model.Description?.Trim() ?? "",
            Price = model.Price, Quantity = model.Quantity });
        await db.SaveChangesAsync();
        TempData["Success"] = "Product created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Managers), HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var p = await db.Products.FindAsync(id);
        return p == null ? NotFound() : View(new ProductViewModel { Id = p.Id, Name = p.Name,
            Description = p.Description, Price = p.Price, Quantity = p.Quantity,
            RowVersion = Convert.ToBase64String(p.RowVersion) });
    }

    [Authorize(Roles = AppRoles.Managers), HttpPost]
    public async Task<IActionResult> Edit(int id, ProductViewModel model)
    {
        if (id != model.Id) return BadRequest();
        ValidatePrice(model.Price);
        if (!ModelState.IsValid) return View(model);
        var p = await db.Products.FindAsync(id);
        if (p == null) return NotFound();
        if (Convert.ToBase64String(p.RowVersion) != model.RowVersion)
        {
            ModelState.AddModelError("", "This product changed. Reload Edit before saving.");
            return View(model);
        }
        p.Name = model.Name.Trim(); p.Description = model.Description?.Trim() ?? "";
        p.Price = model.Price; p.Quantity = model.Quantity;
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError("", "The product changed while saving. Reload Edit and try again.");
            return View(model);
        }
        TempData["Success"] = "Product updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Managers), HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
        return p == null ? NotFound() : View(p);
    }

    [Authorize(Roles = AppRoles.Managers), HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, string rowVersion)
    {
        var p = await db.Products.FindAsync(id);
        if (p == null) return NotFound();
        if (Convert.ToBase64String(p.RowVersion) != rowVersion)
            TempData["Error"] = "Product changed. Review it before deleting.";
        else if (await db.RequestItems.AnyAsync(i => i.ProductId == id))
            TempData["Error"] = "This product is used in a request and cannot be deleted. Set its quantity to zero to make it unavailable.";
        else
        {
            db.Products.Remove(p);
            try { await db.SaveChangesAsync(); TempData["Success"] = "Product deleted."; }
            catch (DbUpdateException) { TempData["Error"] = "Product changed or is now used in a request. It was not deleted."; }
        }
        return RedirectToAction(nameof(Index));
    }

    private void ValidatePrice(decimal price)
    {
        if (decimal.Round(price, 2) != price) ModelState.AddModelError("Price", "Use at most two decimal places.");
    }
}
