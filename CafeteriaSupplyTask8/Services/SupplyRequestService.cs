using System.Data;
using Microsoft.EntityFrameworkCore;
using CafeteriaSupplyTask8.Data;
using CafeteriaSupplyTask8.Models;
using CafeteriaSupplyTask8.ViewModels;

namespace CafeteriaSupplyTask8.Services;

public class RequestRuleException(string message) : Exception(message);

public class SupplyRequestService(ApplicationDbContext db)
{
    public async Task<int> SaveAsync(RequestFormViewModel form, string userId)
    {
        // Serializes edits with approval; the row version also catches stale browser forms.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        SupplyRequest request;
        if (form.Id == 0)
        {
            request = new SupplyRequest { UserId = userId };
            db.SupplyRequests.Add(request);
        }
        else
        {
            request = await db.SupplyRequests.Include(r => r.Items)
                .SingleOrDefaultAsync(r => r.Id == form.Id && r.UserId == userId)
                ?? throw new RequestRuleException("Request was not found or is not yours.");
            RequirePending(request);
            RequireVersion(request.RowVersion, form.RowVersion);
        }

        if (form.Items.Count > 1000) throw new RequestRuleException("Too many product lines.");
        var ids = form.Items.Where(i => i.Quantity > 0).Select(i => i.ProductId).ToList();
        var products = await db.Products.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        // Never accept prices, owner, total or status from the submitted form.
        var selected = RequestRules.BuildItems(products, form.Items);
        var remove = request.Items.Where(i => !ids.Contains(i.ProductId)).ToList();
        db.RequestItems.RemoveRange(remove);
        foreach (var item in remove) request.Items.Remove(item);
        foreach (var line in selected)
        {
            var item = request.Items.SingleOrDefault(i => i.ProductId == line.ProductId);
            if (item == null)
            {
                item = new RequestItem { ProductId = line.ProductId };
                request.Items.Add(item);
            }
            item.Quantity = line.Quantity;
            item.Price = products[line.ProductId].Price;
        }
        request.TotalPrice = RequestRules.Total(request.Items);
        // Force a row-version update even if quantities change but total remains equal.
        if (form.Id != 0) db.Entry(request).Property(r => r.TotalPrice).IsModified = true;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return request.Id;
    }

    public async Task DecideAsync(int id, bool approve, string rowVersion)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var request = await db.SupplyRequests.Include(r => r.Items).ThenInclude(i => i.Product)
            .SingleOrDefaultAsync(r => r.Id == id) ?? throw new RequestRuleException("Request not found.");
        RequirePending(request);
        RequireVersion(request.RowVersion, rowVersion);
        RequestRules.Decide(request, approve);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task DeleteAsync(int id, string userId, string rowVersion)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var request = await db.SupplyRequests.SingleOrDefaultAsync(r => r.Id == id && r.UserId == userId)
            ?? throw new RequestRuleException("Request was not found or is not yours.");
        RequirePending(request);
        RequireVersion(request.RowVersion, rowVersion);
        db.SupplyRequests.Remove(request);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static void RequirePending(SupplyRequest request)
    {
        if (request.Status != RequestStatus.Pending)
            throw new RequestRuleException("Only Pending requests can be changed.");
    }

    public static void RequireVersion(byte[] actual, string supplied)
    {
        if (Convert.ToBase64String(actual) != supplied)
            throw new RequestRuleException("This record changed. Reload the page and try again.");
    }
}
