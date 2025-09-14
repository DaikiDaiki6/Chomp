using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using UserService.Models;

namespace UserService.Attributes;

public class AccountStatusFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;
            
            if (user.Identity?.IsAuthenticated == true)
            {
                var accountStatusClaim = user.FindFirst("AccountStatus")?.Value;
                
                if (accountStatusClaim != null && 
                    Enum.TryParse<AccountStatus>(accountStatusClaim, out var status))
                {
                    if (status == AccountStatus.Banned)
                    {
                        context.Result = new ObjectResult(new { errorMessage = "Account is banned." })
                        {
                            StatusCode = 403
                        };
                        return;
                    }
                    
                    if (status == AccountStatus.PendingDeletion)
                    {
                        context.Result = new ObjectResult(new { errorMessage = "Account is scheduled for deletion." })
                        {
                            StatusCode = 403
                        };
                        return;
                    }
                }
            }
            
            base.OnActionExecuting(context);
        }
    }
