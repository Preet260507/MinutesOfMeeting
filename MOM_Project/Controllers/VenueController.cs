using Microsoft.AspNetCore.Mvc;
using MOM_Project.Models;
using MySqlConnector;
using System.Data;
using System;
using System.Collections.Generic;

namespace MOM_Project.Controllers
{
    public class VenueController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connString;

        public VenueController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
        }

        #region Get All & Search
        public IActionResult Index()
        {
            string loggedInUser = HttpContext.Session.GetString("AdminUser");
            if(loggedInUser == null) 
                return RedirectToAction("Index", "Login");
            return GetFilteredVenues(""); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(IFormCollection form)
        {
            string loggedInUser = HttpContext.Session.GetString("AdminUser");
            if(loggedInUser == null) 
                return RedirectToAction("Index", "Login");

            string searchTerm = form["searchTerm"].ToString();
            ViewBag.SearchTerm = searchTerm; 
            return GetFilteredVenues(searchTerm);
        }

        private IActionResult GetFilteredVenues(string searchTerm)
        {
            List<MeetingVenue> list = new List<MeetingVenue>();
            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("sp_GetAllVenues", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_SearchTerm", searchTerm ?? "");
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new MeetingVenue
                            {
                                MeetingVenueID = reader.GetInt32("MeetingVenueID"),
                                MeetingVenueName = reader.GetString("MeetingVenueName"),
                                Created = reader.GetDateTime("Created"),
                                Modified = reader.GetDateTime("Modified")
                            });
                        }
                    }
                }
            }
            return View("Index", list);
        }
        #endregion

        #region Add/Edit Venue
        [HttpGet]
        public IActionResult AddEdit(int? id)
        {
            string loggedInUser = HttpContext.Session.GetString("AdminUser");
            if(loggedInUser == null) 
                return RedirectToAction("Index", "Login");
            MeetingVenue model = new MeetingVenue();
            
            if (id.HasValue && id > 0)
            {
                using (MySqlConnection conn = new MySqlConnection(_connString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("sp_GetVenueById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_ID", id);
                        
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                model.MeetingVenueID = reader.GetInt32("MeetingVenueID");
                                model.MeetingVenueName = reader.GetString("MeetingVenueName");
                            }
                            else
                            {
                                return NotFound();
                            }
                        }
                    }
                }
            }
            return View(model);
        }
        
        // ---------------------------------------------------------
        // 3. ADD/EDIT (POST): Save data
        // ---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddEdit(MeetingVenue model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(_connString))
                    {
                        conn.Open();
                        MySqlCommand cmd;
                        
                        bool isNew = model.MeetingVenueID == 0;

                        if (isNew)
                        {
                            cmd = new MySqlCommand("sp_InsertVenue", conn);
                            cmd.CommandType = CommandType.StoredProcedure;
                        }
                        else
                        {
                            cmd = new MySqlCommand("sp_UpdateVenue", conn);
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("p_ID", model.MeetingVenueID);
                        }

                        cmd.Parameters.AddWithValue("p_Name", model.MeetingVenueName);
                        cmd.ExecuteNonQuery();

                        // 🌟 SUCCESS POPUP TRIGGER 🌟
                        TempData["ErrorType"] = "success";
                        TempData["Message"] = isNew ? "Venue added successfully!" : "Venue updated successfully!";
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // 🚨 ERROR POPUP TRIGGER 🚨
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "An error occurred while saving the Venue.";
                }
            }
            return View(model);
        }
        #endregion

        #region Delete Venues
        public IActionResult Delete(int? id)
        {
            string loggedInUser = HttpContext.Session.GetString("AdminUser");
            if(loggedInUser == null) 
                return RedirectToAction("Index", "Login");
            if (id == null) return NotFound();
            MeetingVenue model = new MeetingVenue();
            
            using (MySqlConnection connection = new MySqlConnection(_connString))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand("sp_GetVenueById", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_ID", id);
                    
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.MeetingVenueID = reader.GetInt32("MeetingVenueID");
                            model.MeetingVenueName = reader.GetString("MeetingVenueName");
                            
                            // Safety check for null dates
                            if (!reader.IsDBNull(reader.GetOrdinal("Created")))
                                model.Created = reader.GetDateTime("Created");
                            if (!reader.IsDBNull(reader.GetOrdinal("Modified")))
                                model.Modified = reader.GetDateTime("Modified");
                        }
                        else
                        {
                            return NotFound(); 
                        }
                    }
                }
            }
            return View(model);
        }

        // ---------------------------------------------------------
        // 5. DELETE (POST): Actually delete the record
        // ---------------------------------------------------------
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand("sp_DeleteVenue", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_ID", id);
                        
                        command.ExecuteNonQuery();

                        // 🌟 SUCCESS POPUP TRIGGER 🌟
                        TempData["ErrorType"] = "success";
                        TempData["Message"] = "Venue deleted successfully!";
                    }
                }
            }
            catch (MySqlException ex)
            {
                // Check for "Foreign Key Constraint" error (Error Code 1451)
                if (ex.Number == 1451)
                {
                    // 🚨 ERROR POPUP TRIGGER 🚨
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "Cannot delete this Venue because it is currently assigned to existing Meetings. Please reassign or delete those meetings first.";
                
                    return Delete(id); 
                }
                else
                {
                    // Catch-all for other DB errors
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "A database error occurred while trying to delete the Venue.";
                    return Delete(id); 
                }
            }
    
            return RedirectToAction(nameof(Index));
        }
        #endregion
    }
}