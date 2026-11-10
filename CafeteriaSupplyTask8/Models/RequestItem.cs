using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CafeteriaSupplyTask8.Models;

public class RequestItem
{
    public int Id { get; set; }
    public int SupplyRequestId { get; set; }
    public SupplyRequest SupplyRequest { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    // Snapshot of the unit price when the request was created/edited.
    public decimal Price { get; set; }
}
