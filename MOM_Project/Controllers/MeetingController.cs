using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MOM_Project.Models;
using MySqlConnector;
using System.Data;
using System;
using System.Collections.Generic;

namespace MOM_Project.Controllers
{
    public class MeetingController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connString;

        public MeetingController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
        }

        #region Get All Meetings
        // ---------------------------------------------------------
        // 1. INDEX: List all scheduled meetings
        // ---------------------------------------------------------
        public IActionResult Index()
        {
            var list = new List<Meeting>();
            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("sp_GetMeetings", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Meeting
                            {
                                MeetingID = Convert.ToInt32(reader["MeetingID"]),
                                MeetingDescription = reader["MeetingDescription"].ToString(),
                                MeetingDate = Convert.ToDateTime(reader["MeetingDate"]),
        
                                // Safe checks for Start/End time to prevent casting errors
                                StartTime = reader["StartTime"] != DBNull.Value 
                                    ? (TimeSpan)reader["StartTime"] 
                                    : TimeSpan.Zero, 

                                EndTime = reader["EndTime"] != DBNull.Value 
                                    ? (TimeSpan)reader["EndTime"] 
                                    : TimeSpan.Zero, 

                                IsCancelled = Convert.ToBoolean(reader["IsCancelled"]),
                                MeetingVenueName = reader["MeetingVenueName"].ToString(),
                                MeetingTypeName = reader["MeetingTypeName"].ToString(),
                                ParticipantCount = Convert.ToInt32(reader["ParticipantCount"])
                            });
                        }
                    }
                }
            }
            return View(list);
        }
        #endregion

        #region Schedule / Edit Meeting
        // ---------------------------------------------------------
        // 2. ADD/EDIT (GET): Show form
        // ---------------------------------------------------------
        public IActionResult AddEdit(int? id)
        {
            Meeting meeting = new Meeting();
            meeting.MeetingDate = DateTime.Today;
            meeting.StartTime = new TimeSpan(10, 0, 0); // Default 10:00 AM
            meeting.EndTime = new TimeSpan(11, 0, 0);   // Default 11:00 AM

            LoadDropdowns(); 

            if (id.HasValue && id > 0)
            {
                using (MySqlConnection conn = new MySqlConnection(_connString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("sp_GetMeetingById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_ID", id);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                meeting.MeetingID = Convert.ToInt32(reader["MeetingID"]);
                                meeting.MeetingDescription = reader["MeetingDescription"].ToString();
                                meeting.MeetingDate = Convert.ToDateTime(reader["MeetingDate"]);
                                meeting.Remarks = reader["Remarks"].ToString();
                                
                                if (reader["MeetingVenueID"] != DBNull.Value)
                                    meeting.MeetingVenueID = Convert.ToInt32(reader["MeetingVenueID"]);
                                
                                if (reader["MeetingTypeID"] != DBNull.Value)
                                    meeting.MeetingTypeID = Convert.ToInt32(reader["MeetingTypeID"]);
                                
                                if (reader["StartTime"] != DBNull.Value)
                                    meeting.StartTime = (TimeSpan)reader["StartTime"];

                                if (reader["EndTime"] != DBNull.Value)
                                    meeting.EndTime = (TimeSpan)reader["EndTime"];
                            }
                        }
                    }
                }
            }
            return View(meeting);
        }

        // ---------------------------------------------------------
        // 3. ADD/EDIT (POST): Save data
        // ---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddEdit(Meeting meeting)
        {
            // --- 1. CUSTOM VALIDATION ---
            DateTime fullStartDateTime = meeting.MeetingDate.Date.Add(meeting.StartTime);

            // Rule A: Meetings cannot be in the past
            if (fullStartDateTime < DateTime.Now)
            {
                ModelState.AddModelError("MeetingDate", "You cannot schedule a meeting in the past.");
            }

            // Rule B: End time must be after Start time
            if (meeting.EndTime <= meeting.StartTime)
            {
                ModelState.AddModelError("EndTime", "End time must be later than the start time.");
            }

            // --- 2. SAVE IF VALID ---
            if (ModelState.IsValid)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(_connString))
                    {
                        conn.Open();
                        MySqlCommand cmd;
                        
                        bool isNew = meeting.MeetingID == 0;

                        if (isNew)
                        {
                            cmd = new MySqlCommand("sp_InsertMeeting", conn);
                            cmd.CommandType = CommandType.StoredProcedure;
                        }
                        else
                        {
                            cmd = new MySqlCommand("sp_UpdateMeeting", conn);
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("p_ID", meeting.MeetingID);
                        }

                        cmd.Parameters.AddWithValue("p_Desc", meeting.MeetingDescription);
                        cmd.Parameters.AddWithValue("p_Date", meeting.MeetingDate);
                        cmd.Parameters.AddWithValue("p_StartTime", meeting.StartTime);
                        cmd.Parameters.AddWithValue("p_EndTime", meeting.EndTime);
                        cmd.Parameters.AddWithValue("p_Remarks", meeting.Remarks ?? "");
                        
                        cmd.Parameters.AddWithValue("p_VenueID", meeting.MeetingVenueID ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("p_TypeID", meeting.MeetingTypeID ?? (object)DBNull.Value);
                        
                        cmd.ExecuteNonQuery();

                        // 🌟 SUCCESS POPUP TRIGGER 🌟
                        TempData["ErrorType"] = "success";
                        TempData["Message"] = isNew ? "Meeting scheduled successfully!" : "Meeting details updated successfully!";
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // 🚨 ERROR POPUP TRIGGER 🚨
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "An error occurred while scheduling the meeting.";
                    ModelState.AddModelError("", "Database Error: " + ex.Message);
                }
            }

            // --- 3. IF VALIDATION FAILS ---
            LoadDropdowns(); 
            return View(meeting); 
        }
        #endregion

        #region Cancel Meeting
        // ---------------------------------------------------------
        // 4. CANCEL MEETING: Mark meeting as cancelled
        // ---------------------------------------------------------
        public IActionResult Cancel(int id)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("sp_CancelMeeting", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_ID", id);
                        cmd.ExecuteNonQuery();

                        // 🌟 SUCCESS POPUP TRIGGER 🌟
                        TempData["ErrorType"] = "success";
                        TempData["Message"] = "Meeting has been successfully cancelled.";
                    }
                }
            }
            catch (Exception ex)
            {
                // 🚨 ERROR POPUP TRIGGER 🚨
                TempData["ErrorType"] = "error";
                TempData["Message"] = "Failed to cancel the meeting. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Helper Methods
        // ---------------------------------------------------------
        // Helper to load Venues AND Meeting Types for dropdowns
        // ---------------------------------------------------------
        private void LoadDropdowns()
        {
            var venues = new List<SelectListItem>();
            var types = new List<SelectListItem>();

            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();
                
                // 1. Venues
                using (MySqlCommand cmd = new MySqlCommand("sp_GetAllVenues", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) 
                        {
                            venues.Add(new SelectListItem 
                            { 
                                Text = reader["MeetingVenueName"].ToString(), 
                                Value = reader["MeetingVenueID"].ToString() 
                            });
                        }
                    }
                }

                // 2. Types
                using (MySqlCommand cmd = new MySqlCommand("sp_GetAllMeetingTypes", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) 
                        {
                            types.Add(new SelectListItem 
                            { 
                                Text = reader["MeetingTypeName"].ToString(), 
                                Value = reader["MeetingTypeID"].ToString() 
                            });
                        }
                    }
                }
            }
            ViewBag.Venues = venues;
            ViewBag.Types = types;
        }
        #endregion
    }
}