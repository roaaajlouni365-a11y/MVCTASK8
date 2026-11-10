using System.ComponentModel.DataAnnotations;
using CafeteriaSupplyTask8.Models;

namespace CafeteriaSupplyTask8.ViewModels;

public class RegisterViewModel
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Required, Compare(nameof(Password)), DataType(DataType.Password), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    [Display(Name = "Remember me")] public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class ProductViewModel
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string Description { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "1000000")] public decimal Price { get; set; }
    [Range(0, 1000000)] public int Quantity { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public class RequestLineViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Available { get; set; }
    [Range(0, 1000000)] public int Quantity { get; set; }
}

public class RequestFormViewModel
{
    public int Id { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<RequestLineViewModel> Items { get; set; } = [];
}

public class UserRoleViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [Required] public string Role { get; set; } = AppRoles.User;
    public string ConcurrencyStamp { get; set; } = string.Empty;
}
