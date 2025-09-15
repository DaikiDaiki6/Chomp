using System;
using System.Security.Claims;

namespace OrderService.Services.Helper;

public class GetCurrentUserInfo
{
    public static(string? userId, string? userRole, bool isAdmin) GetUserInfo(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        var isAdmin = userRole == "Admin";
        return (userId, userRole, isAdmin);
    }
}
