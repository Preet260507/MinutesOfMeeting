using Microsoft.AspNetCore.Mvc;

namespace MOM_Project.Controllers
{
    public class LoginController : Controller
    {
        // GET: Show Login Page
        [HttpGet]
        public IActionResult Index() // Or Login()
        {
            // This silently empties the backpack so old messages don't survive!
            TempData.Clear(); 
    
            return View();
        }

        // POST: Process Login
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            // Hardcoded Admin Credentials
            if (username == "preet" && password == "Preet@1719")
            {
                TempData["ErrorType"] = "success";
                TempData["Message"] = "Login successful! Welcome back.";
                HttpContext.Session.SetString("AdminUser", "Preet");
                return RedirectToAction("Index", "Home");
                
            }
            else
            {
                ViewBag.Error = "Invalid ID or Password";
                return View("Index");
            }
        }

        // Logout Action
        // Add this inside your LoginController
        public IActionResult Logout()
        {
            // 1. Clear the session so the user is actually logged out
            HttpContext.Session.Clear(); 

            // 2. Trigger the green SweetAlert popup
            TempData["ErrorType"] = "success";
            TempData["Message"] = "You have been logged out securely.";

            // 3. Redirect to your Login page. 
            // If your login screen is the Index() method, use "Index". 
            // If it's called Login(), change this to RedirectToAction("Login");
            return RedirectToAction("Index"); 
        }
    }
}