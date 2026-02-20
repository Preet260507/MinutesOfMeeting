using Microsoft.AspNetCore.Mvc;
using MOM_Project.Models;
using MySqlConnector;
using System.Data;
using Microsoft.AspNetCore.Hosting; // Needed for saving files

namespace MOM_Project.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connString;
        private readonly IWebHostEnvironment _webHostEnvironment; // Lets us access the wwwroot folder

        // Inject IWebHostEnvironment
        public ProfileController(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
            _webHostEnvironment = webHostEnvironment;
        }

        // ---------------------------------------------------------
        // 1. INDEX: View Profile
        // ---------------------------------------------------------
        public IActionResult Index()
        {
            UserProfile profile = GetProfileData();
            return View(profile);
        }

        // ---------------------------------------------------------
        // 2. EDIT (GET): Show Edit Form
        // ---------------------------------------------------------
        public IActionResult Edit()
        {
            UserProfile profile = GetProfileData();
            return View(profile);
        }

        // ---------------------------------------------------------
        // 3. EDIT (POST): Save Profile & Upload Image
        // ---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(UserProfile model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string uniqueFileName = null;

                    // A. IMAGE UPLOAD LOGIC
                    if (model.ProfilePictureUpload != null)
                    {
                        // Create a folder path inside wwwroot/images/profiles
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profiles");
                        
                        // Create directory if it doesn't exist
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        // Give file a unique name (Guid) so we don't overwrite other files with the same name
                        uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfilePictureUpload.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Save the file to the server
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            model.ProfilePictureUpload.CopyTo(fileStream);
                        }

                        // Set the path to save in the Database
                        uniqueFileName = "/images/profiles/" + uniqueFileName;
                    }

                    // B. DATABASE SAVE LOGIC
                    using (MySqlConnection conn = new MySqlConnection(_connString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand("sp_UpdateUserProfile", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("p_UserID", 1); // Hardcoded to User 1 for now
                            cmd.Parameters.AddWithValue("p_FullName", model.FullName);
                            cmd.Parameters.AddWithValue("p_Email", model.Email);
                            cmd.Parameters.AddWithValue("p_Department", model.Department);
                            cmd.Parameters.AddWithValue("p_ImagePath", uniqueFileName ?? (object)DBNull.Value); // Pass NULL if no new image
                            
                            cmd.ExecuteNonQuery();
                        }
                    }

                    TempData["ErrorType"] = "success";
                    TempData["Message"] = "Profile updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "Error saving profile.";
                }
            }
            return View(model);
        }


        // --- Helper to Fetch Data ---
        private UserProfile GetProfileData()
        {
            UserProfile profile = new UserProfile();
            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("sp_GetUserProfile", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_UserID", 1); // Hardcoded to User 1
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            profile.UserID = Convert.ToInt32(reader["UserID"]);
                            profile.FullName = reader["FullName"].ToString();
                            profile.Email = reader["Email"].ToString();
                            profile.Department = reader["Department"].ToString();
                            profile.ProfileImagePath = reader["ProfileImagePath"]?.ToString();
                        }
                    }
                }
            }
            
            // Set a fallback image if database is empty
            if (string.IsNullOrEmpty(profile.ProfileImagePath))
                profile.ProfileImagePath = "/images/default-avatar.png";

            return profile;
        }
    }
}