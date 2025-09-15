using System;
using System.Security.Claims;

namespace UserService.Services.Helpers;

public static class CanModifyUserHelper
{
    public static bool CanModifyUser(ClaimsPrincipal user, Guid targetUserId)
    {
        var currentUserIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var currentUserRole = user.FindFirst(ClaimTypes.Role)?.Value;

        if (currentUserRole == "Admin")
        {
            return true;
        }
        Console.WriteLine($"{currentUserIdClaim} == {targetUserId}");
        if (Guid.TryParse(currentUserIdClaim, out var currentUserId))
        {
            return currentUserId == targetUserId;
        }

        return false;
    }
}
