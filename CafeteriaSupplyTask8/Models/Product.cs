using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CafeteriaSupplyTask8.Models;

public class Product
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string Description { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "1000000")] public decimal Price { get; set; }
    [Range(0, 1000000)] public int Quantity { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
