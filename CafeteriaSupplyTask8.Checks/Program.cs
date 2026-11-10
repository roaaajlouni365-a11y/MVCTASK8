using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CafeteriaSupplyTask8.Controllers;
using CafeteriaSupplyTask8.Data;
using CafeteriaSupplyTask8.Models;
using CafeteriaSupplyTask8.Services;
using CafeteriaSupplyTask8.ViewModels;

var passed = 0;
void Check(bool value, string name)
{
    if (!value) throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name); passed++;
}
void Rejected(Action action, string name)
{
    try { action(); } catch (RequestRuleException) { Check(true, name); return; }
    throw new Exception("FAIL: " + name);
}
var catalogue = new Dictionary<int, Product>
{
    [1] = new() { Id = 1, Name = "Coffee", Price = 5.50m, Quantity = 10 },
    [2] = new() { Id = 2, Name = "Milk", Price = 1.50m, Quantity = 20 }
};
var lines = new List<RequestLineViewModel>
{
    new() { ProductId = 1, Quantity = 2, UnitPrice = 0.01m },
    new() { ProductId = 2, Quantity = 3, UnitPrice = 0.01m }
};
var items = RequestRules.BuildItems(catalogue, lines);
Check(RequestRules.Total(items) == 15.50m, "Two-product total uses server prices, ignores forged prices");
catalogue[1].Price = 9m;
Check(items[0].Price == 5.50m, "Saved item unit price remains a snapshot");
Check(RequestRules.BuildItems(catalogue, lines)[0].Price == 9m, "Editing uses current catalogue prices");
Check(catalogue[1].Quantity == 10, "Creating request does not reduce stock");
var approved = new SupplyRequest { Items = items };
foreach (var item in items) item.Product = catalogue[item.ProductId];
RequestRules.Decide(approved, true);
Check(approved.Status == RequestStatus.Approved && catalogue[1].Quantity == 8 && catalogue[2].Quantity == 17,
    "Approval reduces each product stock by the correct quantity");
Rejected(() => RequestRules.Decide(approved, true), "Repeated approval rejected");
Check(catalogue[1].Quantity == 8, "Repeated approval does not deduct stock twice");
Rejected(() => RequestRules.Decide(approved, false), "Approved request cannot be rejected");
var insufficient = new SupplyRequest { Items = [new() { Product = catalogue[1], Quantity = 1 }, new() { Product = catalogue[2], Quantity = 99 }] };
Rejected(() => RequestRules.Decide(insufficient, true), "Approval rechecks available stock");
Check(catalogue[1].Quantity == 8 && insufficient.Status == RequestStatus.Pending, "Failed approval leaves all stock and status unchanged");
RequestRules.Decide(insufficient, false);
Check(insufficient.Status == RequestStatus.Rejected && catalogue[1].Quantity == 8, "Rejection changes status without deducting stock");
Rejected(() => RequestRules.Decide(insufficient, true), "Rejected request cannot be approved");
Rejected(() => RequestRules.BuildItems(catalogue, []), "Empty request rejected");
Rejected(() => RequestRules.BuildItems(catalogue, [new() { ProductId = 1, Quantity = 0 }]), "All-zero request rejected");
Rejected(() => RequestRules.BuildItems(catalogue, [new() { ProductId = 1, Quantity = -1 }]), "Negative quantity rejected");
Rejected(() => RequestRules.BuildItems(catalogue, [new() { ProductId = 1, Quantity = 11 }]), "Insufficient stock rejected");
Rejected(() => RequestRules.BuildItems(catalogue, [new() { ProductId = 99, Quantity = 1 }]), "Unknown product rejected");
Rejected(() => RequestRules.BuildItems(catalogue, [new() { ProductId = 1, Quantity = 1 }, new() { ProductId = 1, Quantity = 1 }]), "Duplicate product rejected");
Rejected(() => RequestRules.BuildItems(catalogue, [new() { ProductId = 1, Quantity = 1000001 }]), "Excessive quantity rejected");
Rejected(() => SupplyRequestService.RequireVersion([1, 2], Convert.ToBase64String([1, 3])), "Stale row version rejected");
SupplyRequestService.RequireVersion([1, 2], Convert.ToBase64String([1, 2]));
Check(true, "Current row version accepted");
var validation = new List<ValidationResult>();
var invalidRegistration = new RegisterViewModel { FullName = " ", Email = "invalid", Password = "short", ConfirmPassword = "different" };
Check(!Validator.TryValidateObject(invalidRegistration,
    new ValidationContext(invalidRegistration), validation, true), "Invalid registration fields rejected");
Check(typeof(AdminController).GetCustomAttribute<AuthorizeAttribute>()?.Roles == AppRoles.SuperAdmin, "User management restricted to SuperAdmin");
Check(typeof(SupplyRequestController).IsDefined(typeof(AuthorizeAttribute)), "Requests require authentication");
Check(typeof(ProductController).IsDefined(typeof(AuthorizeAttribute)), "Products require authentication");
foreach (var method in typeof(ProductController).GetMethods().Where(m => new[] { "Create", "Edit", "Delete", "DeleteConfirmed" }.Contains(m.Name)))
    Check(method.GetCustomAttribute<AuthorizeAttribute>()?.Roles == AppRoles.Managers, $"Product {method.Name}/{(method.IsDefined(typeof(HttpPostAttribute)) ? "POST" : "GET")} restricted to managers");
foreach (var name in new[] { "Approve", "Reject" })
{
    var method = typeof(SupplyRequestController).GetMethod(name)!;
    Check(method.GetCustomAttribute<AuthorizeAttribute>()?.Roles == AppRoles.Managers && method.IsDefined(typeof(HttpPostAttribute)), name + " is manager-only POST");
}
Check(typeof(AccountController).GetMethod("Logout")!.IsDefined(typeof(HttpPostAttribute)), "Logout uses POST");
using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=SchemaCheck;Trusted_Connection=True").Options);
Check(db.Model.FindEntityType(typeof(ApplicationUser))!.GetTableName() == "AspNetUsers", "ApplicationUser maps to AspNetUsers");
Check(db.Model.FindEntityType(typeof(IdentityRole))!.GetTableName() == "AspNetRoles", "Roles use AspNetRoles");
Check(db.Model.FindEntityType(typeof(IdentityUserRole<string>))!.GetTableName() == "AspNetUserRoles", "Membership uses AspNetUserRoles");
Check(db.Model.FindEntityType(typeof(RequestItem))!.GetForeignKeys().Single(f => f.PrincipalEntityType.ClrType == typeof(Product)).DeleteBehavior == DeleteBehavior.Restrict,
    "Products referenced by requests cannot be cascade-deleted");
Check(db.Model.FindEntityType(typeof(SupplyRequest))!.FindProperty("RowVersion")!.IsConcurrencyToken, "Request row version enables concurrency checks");
Check(db.Model.FindEntityType(typeof(Product))!.FindProperty("Price")!.GetPrecision() == 18, "Prices have explicit decimal precision");
Check(db.Database.GetMigrations().Count() == 2, "Original Identity migration and cafeteria migration are included");
Console.WriteLine($"\n{passed} checks passed. These checks do not connect to SQL Server or exercise live HTTP sessions.");
