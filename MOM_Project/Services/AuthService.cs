namespace MOM_Project.Services
{
    public class AuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void LoginUser(string username)
        {
            _httpContextAccessor.HttpContext?.Session.SetString("AdminUser", username);
        }

        public void LogoutUser()
        {
            _httpContextAccessor.HttpContext?.Session.Clear();
        }

        public string GetUserName()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString("AdminUser") ?? "";
        }

        public bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(GetUserName());
        }
    }
}