using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Authorization;
using MOM_Project.Services;

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
            bool hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
                .Any(em => em.GetType() == typeof(AllowAnonymousAttribute));

            if (hasAllowAnonymous) return;

            if (!_authService.IsAuthenticated())
            {
                var tempData = _tempDataFactory.GetTempData(context.HttpContext);
                tempData["ErrorType"] = "error";
                tempData["Message"] = "Please log in to access this secure page.";

                context.Result = new RedirectToActionResult("Index", "Login", null);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}