using Microsoft.AspNetCore.Mvc;
using MOM_Project.Models; // Make sure this is here!
using MySqlConnector;
using System.Data;
using System;

namespace MOM_Project.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connString;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
        }

        // ---------------------------------------------------------
        // GET: Show Login Page
        // ---------------------------------------------------------
        [HttpGet]
        public IActionResult Index() 
        {
            TempData.Clear(); 
            // We pass an empty UserModel to the view so it can bind to it
            return View(new UserModel());
        }

        // ---------------------------------------------------------
        // POST: Process Login via UserModel
        // ---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken] // Security measure
        public IActionResult Login(UserModel model)
        {
            // 1. Check if the model passes our [Required] validation
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            bool isValidUser = false;
            string loggedInUser = "";

            // 2. Connect to the database to verify credentials
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("sp_VerifyUserLogin", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        
                        // Pull the data directly from our UserModel!
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

            // 3. Handle the result
            if (isValidUser)
            {
                TempData["ErrorType"] = "success";
                TempData["Message"] = $"Login successful! Welcome back, {loggedInUser}.";

                if (loggedInUser != null) HttpContext.Session.SetString("AdminUser", loggedInUser);

                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.Error = "Invalid Username or Password";
                // Return the model back so the username they typed stays in the box!
                return View("Index", model);
            }
        }

        // ---------------------------------------------------------
        // GET: Logout Action
        // ---------------------------------------------------------
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); 
            TempData["ErrorType"] = "success";
            TempData["Message"] = "You have been logged out securely.";
            return RedirectToAction("Index"); 
        }
    }
}