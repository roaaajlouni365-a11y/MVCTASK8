using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CafeteriaSupplyTask8.Models;

public static class AppRoles
{
    public const string User = "User";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";
    public const string Managers = Admin + "," + SuperAdmin;
    public static readonly string[] All = [User, Admin, SuperAdmin];
}
