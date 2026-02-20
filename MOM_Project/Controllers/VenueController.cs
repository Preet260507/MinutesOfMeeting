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

        #region Get All Venues
        // ---------------------------------------------------------
        // 1. INDEX: List all venues
        // ---------------------------------------------------------
        public IActionResult Index()
        {
            List<MeetingVenue> list = new List<MeetingVenue>();
            
            using (MySqlConnection connection = new MySqlConnection(_connString))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand("sp_GetAllVenues", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            MeetingVenue model = new MeetingVenue();
                            model.MeetingVenueID = reader.GetInt32("MeetingVenueID");
                            model.MeetingVenueName = reader.GetString("MeetingVenueName");
                            model.Created = reader.GetDateTime("Created");
                            model.Modified = reader.GetDateTime("Modified");
                            list.Add(model);
                        }
                    }
                }
            }

            return View(list);
        }
        #endregion

        #region Add/Edit Venue
        // ---------------------------------------------------------
        // 2. ADD/EDIT (GET): Show form
        // ---------------------------------------------------------
        [HttpGet]
        public IActionResult AddEdit(int? id)
        {
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
        // ---------------------------------------------------------
        // 4. DELETE (GET): Show Confirmation
        // ---------------------------------------------------------
        public IActionResult Delete(int? id)
        {
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