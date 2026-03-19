using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Authorization; // 🌟 Needed for AllowAnonymous
using MOM_Project.Services;
using System.Linq;

namespace MOM_Project.Filters
{
    public class CheckAccessFilter : IActionFilter
    {
        private readonly AuthService _authService;
        private readonly ITempDataDictionaryFactory _tempDataFactory;

        public CheckAccessFilter(AuthService authService, ITempDataDictionaryFactory tempDataFactory)
        {
            _authService = authService;
            _tempDataFactory = tempDataFactory;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // 🌟 1. Check if this page has the "VIP Pass" [AllowAnonymous] tag
            bool hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
                .Any(em => em.GetType() == typeof(AllowAnonymousAttribute));

            // If it has the pass (like the Login page), let them through immediately!
            if (hasAllowAnonymous) return;

            // 🌟 2. If no VIP pass, check if they are logged in
            if (!_authService.IsAuthenticated())
            {
                var tempData = _tempDataFactory.GetTempData(context.HttpContext);
                tempData["ErrorType"] = "error";
                tempData["Message"] = "Please log in to access this secure page.";

                // Kick them to the Login screen
                context.Result = new RedirectToActionResult("Index", "Login", null);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}