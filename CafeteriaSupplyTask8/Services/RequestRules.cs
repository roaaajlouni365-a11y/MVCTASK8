using CafeteriaSupplyTask8.Models;
using CafeteriaSupplyTask8.ViewModels;

namespace CafeteriaSupplyTask8.Services;

// Pure business rules shared by the application and the executable checks project.
public static class RequestRules
{
    public static List<RequestItem> BuildItems(IReadOnlyDictionary<int, Product> products,
        IReadOnlyList<RequestLineViewModel> lines)
    {
        if (lines.Count > 1000 || lines.Any(i => i.Quantity < 0 || i.Quantity > 1000000))
            throw new RequestRuleException("Quantities must be whole numbers between 0 and 1,000,000.");
        var selected = lines.Where(i => i.Quantity > 0).ToList();
        if (selected.Count == 0) throw new RequestRuleException("Select at least one product with a positive quantity.");
        if (selected.Select(i => i.ProductId).Distinct().Count() != selected.Count)
            throw new RequestRuleException("A product cannot appear more than once.");
        var result = new List<RequestItem>();
        foreach (var line in selected)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                throw new RequestRuleException("One of the selected products no longer exists.");
            if (line.Quantity > product.Quantity)
                throw new RequestRuleException($"Not enough stock for {product.Name}. Available: {product.Quantity}.");
            result.Add(new RequestItem { ProductId = product.Id, Quantity = line.Quantity, Price = product.Price });
        }
        return result;
    }

    public static decimal Total(IEnumerable<RequestItem> items) => items.Sum(i => i.Quantity * i.Price);

    public static void Decide(SupplyRequest request, bool approve)
    {
        if (request.Status != RequestStatus.Pending)
            throw new RequestRuleException("Only Pending requests can be changed.");
        if (approve)
        {
            // Validate every item before changing any stock.
            foreach (var item in request.Items)
                if (item.Product.Quantity < item.Quantity)
                    throw new RequestRuleException($"Approval failed: insufficient stock for {item.Product.Name}.");
            foreach (var item in request.Items) item.Product.Quantity -= item.Quantity;
        }
        request.Status = approve ? RequestStatus.Approved : RequestStatus.Rejected;
    }
}
