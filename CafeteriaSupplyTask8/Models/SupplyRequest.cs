using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CafeteriaSupplyTask8.Models;

public class SupplyRequest
{
    public int Id { get; set; }
    public DateTime RequestDate { get; set; } = DateTime.UtcNow;
    public decimal TotalPrice { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public List<RequestItem> Items { get; set; } = [];
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
