using Microsoft.AspNetCore.Http;

namespace MOM_Project.Services
{
    public class AuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // 1. Log the user in
        public void LoginUser(string username)
        {
            _httpContextAccessor.HttpContext.Session.SetString("AdminUser", username);
        }

        // 2. Log the user out
        public void LogoutUser()
        {
            _httpContextAccessor.HttpContext.Session.Clear();
        }

        // 3. Get the current user's name
        public string GetUserName()
        {
            return _httpContextAccessor.HttpContext.Session.GetString("AdminUser");
        }

        // 4. Check if someone is currently logged in
        public bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(GetUserName());
        }
    }
}