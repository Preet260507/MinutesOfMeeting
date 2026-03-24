using Microsoft.AspNetCore.Mvc;
using MOM_Project.Models; 
using MOM_Project.Services; 
using MySqlConnector;
using System.Data;
using System;
using Microsoft.AspNetCore.Authorization;

namespace MOM_Project.Controllers
{    
    [AllowAnonymous]
    public class LoginController : Controller
    {
        #region Iconfiguration
        private readonly IConfiguration _configuration;
        private readonly string _connString;
        private readonly AuthService _authService;
        
        public LoginController(IConfiguration configuration, AuthService authService)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
            _authService = authService;
        }
        #endregion
        
        #region LoginPage
        [HttpGet]
        public IActionResult Index() 
        {
            TempData.Clear(); 
            return View(new UserModel());
        }
        #endregion

        #region Login
        [HttpPost]
        [ValidateAntiForgeryToken] 
        public IActionResult Login(UserModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            bool isValidUser = false;
            string loggedInUser = "";

            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("sp_VerifyUserLogin", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        
                        cmd.Parameters.AddWithValue("p_UserName", model.UserName);
                        cmd.Parameters.AddWithValue("p_Password", model.Password);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                isValidUser = true;
                                loggedInUser = reader["UserName"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Database connection error. Please try again later.";
                return View("Index", model);
            }

            if (isValidUser)
            {
                TempData["ErrorType"] = "success";
                TempData["Message"] = $"Login successful! Welcome back, {loggedInUser}.";
 
                _authService.LoginUser(loggedInUser);
                
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Error = "Invalid Username or Password";
                return View("Index", model);
            }
        }
        #endregion
        
        #region Logout
        public IActionResult Logout()
        {
            _authService.LogoutUser(); 
            
            TempData["ErrorType"] = "success";
            TempData["Message"] = "You have been logged out securely.";
            
            return RedirectToAction("Index"); 
        }
        #endregion
    }
}