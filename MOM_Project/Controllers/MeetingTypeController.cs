using Microsoft.AspNetCore.Mvc;
using MOM_Project.Models;
using MySqlConnector;
using System.Data;
using System;
using System.Collections.Generic;

namespace MOM_Project.Controllers
{
    public class MeetingTypeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connString;

        public MeetingTypeController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
        }

        #region Index
        // ---------------------------------------------------------
        // 1. INDEX: List all types (with Remarks)
        // ---------------------------------------------------------
        public IActionResult Index()
        {
            List<MeetingType> list = new List<MeetingType>();
            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("sp_GetAllTypes", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new MeetingType
                            {
                                MeetingTypeID = Convert.ToInt32(reader["MeetingTypeID"]),
                                MeetingTypeName = reader["MeetingTypeName"].ToString(),
                                Remarks = reader["Remarks"].ToString(), 
                                Created = Convert.ToDateTime(reader["Created"]),
                                Modified = Convert.ToDateTime(reader["Modified"])
                            });
                        }
                    }
                }
            }
            return View(list);
        }
        #endregion

        #region Add/Edit Meeting Type
        // ---------------------------------------------------------
        // 2. ADD/EDIT (GET): Show form
        // ---------------------------------------------------------
        public IActionResult AddEdit(int? id)
        {
            MeetingType type = new MeetingType();

            // EDIT MODE: Fetch data
            if (id.HasValue && id > 0)
            {
                using (MySqlConnection conn = new MySqlConnection(_connString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("sp_GetTypeById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_ID", id);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                type.MeetingTypeID = Convert.ToInt32(reader["MeetingTypeID"]);
                                type.MeetingTypeName = reader["MeetingTypeName"].ToString();
                                type.Remarks = reader["Remarks"].ToString(); 
                            }
                            else
                            {
                                return NotFound();
                            }
                        }
                    }
                }
            }
            return View(type);
        }

        // ---------------------------------------------------------
        // 3. ADD/EDIT (POST): Save data
        // ---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddEdit(MeetingType type)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(_connString))
                    {
                        conn.Open();
                        MySqlCommand cmd;
                        
                        bool isNew = type.MeetingTypeID == 0;

                        if (isNew)
                        {
                            cmd = new MySqlCommand("sp_InsertMeetingType", conn);
                            cmd.CommandType = CommandType.StoredProcedure;
                        }
                        else
                        {
                            cmd = new MySqlCommand("sp_UpdateMeetingType", conn);
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("p_ID", type.MeetingTypeID);
                        }

                        cmd.Parameters.AddWithValue("p_Name", type.MeetingTypeName);
                        cmd.Parameters.AddWithValue("p_Remarks", type.Remarks ?? "");

                        cmd.ExecuteNonQuery();

                        // 🌟 SUCCESS POPUP TRIGGER 🌟
                        TempData["ErrorType"] = "success";
                        TempData["Message"] = isNew ? "Meeting Type added successfully!" : "Meeting Type updated successfully!";
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // 🚨 ERROR POPUP TRIGGER 🚨
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "An error occurred while saving the Meeting Type.";
                }
            }
            return View(type);
        }
        #endregion

        #region Delete Meeting Type
        // ---------------------------------------------------------
        // 4. DELETE (GET): Show Confirmation
        // ---------------------------------------------------------
        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();
            MeetingType model = new MeetingType();
            
            using (MySqlConnection connection = new MySqlConnection(_connString))
            {
                connection.Open();
                MySqlCommand command = connection.CreateCommand();
                
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = "sp_GetTypeById";
                command.Parameters.AddWithValue("p_ID", id);
                
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model.MeetingTypeID = reader.GetInt32("MeetingTypeID");
                        model.MeetingTypeName = reader.GetString("MeetingTypeName");
                        
                        // Safety check in case Created/Modified are null in old records
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
                    MySqlCommand command = connection.CreateCommand();
            
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "sp_DeleteMeetingType";
                    command.Parameters.AddWithValue("p_ID", id);
                    
                    command.ExecuteNonQuery();

                    // 🌟 SUCCESS POPUP TRIGGER 🌟
                    TempData["ErrorType"] = "success";
                    TempData["Message"] = "Meeting Type deleted successfully!";
                }
            }
            catch (MySqlException ex)
            {
                // Check for "Foreign Key Constraint" error (Error Code 1451)
                if (ex.Number == 1451)
                {
                    // 🚨 ERROR POPUP TRIGGER 🚨
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "Cannot delete this Meeting Type because it is currently assigned to existing Meetings. Please reassign or delete those meetings first.";
                
                    // Reload the GET Delete view to stay on the page and show the popup
                    return Delete(id); 
                }
                else
                {
                    // For any other unexpected database error
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "A database error occurred while trying to delete.";
                    return Delete(id); 
                }
            }
            
            return RedirectToAction(nameof(Index));
        }
        #endregion
    }
}