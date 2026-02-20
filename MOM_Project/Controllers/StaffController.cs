using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MOM_Project.Models;
using MySqlConnector;
using System.Data;
using System;
using System.Collections.Generic;

namespace MOM_Project.Controllers
{
    public class StaffController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connString;

        public StaffController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
        }

        #region Get All Staff
        // ---------------------------------------------------------
        // 1. INDEX: List all staff
        // ---------------------------------------------------------
        public IActionResult Index()
        {
            List<Staff> staffList = new List<Staff>();

            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("sp_GetAllStaff", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            staffList.Add(new Staff
                            {
                                StaffID = Convert.ToInt32(reader["StaffID"]),
                                StaffName = reader["StaffName"].ToString(),
                                EmailAddress = reader["EmailAddress"].ToString(),
                                MobileNo = reader["MobileNo"].ToString(),
                                DepartmentName = reader["DepartmentName"].ToString(),
                                Created = Convert.ToDateTime(reader["Created"]),
                                Modified = Convert.ToDateTime(reader["Modified"])
                            });
                        }
                    }
                }
            }
            return View(staffList);
        }
        #endregion

        #region Add/Edit Staff
        // ---------------------------------------------------------
        // 2. ADD/EDIT (GET): Show Form
        // ---------------------------------------------------------
        public IActionResult AddEdit(int? id)
        {
            Staff staff = new Staff();
            ViewBag.Departments = GetDepartmentList(); // Load Dropdown

            if (id.HasValue && id > 0)
            {
                using (MySqlConnection conn = new MySqlConnection(_connString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("sp_GetStaffById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_ID", id);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                staff.StaffID = Convert.ToInt32(reader["StaffID"]);
                                staff.StaffName = reader["StaffName"].ToString();
                                staff.EmailAddress = reader["EmailAddress"].ToString();
                                staff.MobileNo = reader["MobileNo"].ToString();
                                
                                if (reader["DepartmentID"] != DBNull.Value)
                                    staff.DepartmentID = Convert.ToInt32(reader["DepartmentID"]);
                            }
                            else
                            {
                                return NotFound();
                            }
                        }
                    }
                }
            }
            return View(staff);
        }

        // ---------------------------------------------------------
        // 3. ADD/EDIT (POST): Save Data
        // ---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddEdit(Staff staff)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(_connString))
                    {
                        conn.Open();
                        MySqlCommand cmd;

                        bool isNew = staff.StaffID == 0;

                        if (isNew)
                        {
                            cmd = new MySqlCommand("sp_InsertStaff", conn);
                            cmd.CommandType = CommandType.StoredProcedure;
                        }
                        else
                        {
                            cmd = new MySqlCommand("sp_UpdateStaff", conn);
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("p_ID", staff.StaffID);
                        }

                        cmd.Parameters.AddWithValue("p_Name", staff.StaffName);
                        cmd.Parameters.AddWithValue("p_EmailAddress", staff.EmailAddress ?? "");
                        cmd.Parameters.AddWithValue("p_MobileNo", staff.MobileNo ?? "");

                        if (staff.DepartmentID.HasValue)
                            cmd.Parameters.AddWithValue("p_DepartmentID", staff.DepartmentID);
                        else
                            cmd.Parameters.AddWithValue("p_DepartmentID", DBNull.Value);

                        cmd.ExecuteNonQuery();

                        // 🌟 SUCCESS POPUP TRIGGER 🌟
                        TempData["ErrorType"] = "success";
                        TempData["Message"] = isNew ? "Staff member added successfully!" : "Staff member updated successfully!";
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // 🚨 ERROR POPUP TRIGGER 🚨
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "An error occurred while saving the staff member.";
                    ModelState.AddModelError("", "Database Error: " + ex.Message);
                }
            }

            // Validation Failed? Reload Dropdown!
            ViewBag.Departments = GetDepartmentList();
            return View(staff);
        }
        #endregion

        #region Delete Staff
        // ---------------------------------------------------------
        // 4. DELETE (GET): Show Confirmation Page
        // ---------------------------------------------------------
        public IActionResult Delete(int id)
        {
            Staff staff = new Staff();
            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("sp_GetStaffById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_ID", id);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            staff.StaffID = Convert.ToInt32(reader["StaffID"]);
                            staff.StaffName = reader["StaffName"].ToString();
                            staff.EmailAddress = reader["EmailAddress"].ToString();
                            staff.MobileNo = reader["MobileNo"].ToString();
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }
            }
            return View(staff);
        }

        // ---------------------------------------------------------
        // 5. DELETE (POST): Actually Delete
        // ---------------------------------------------------------
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("sp_DeleteStaff", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_ID", id);
                        cmd.ExecuteNonQuery();

                        // 🌟 SUCCESS POPUP TRIGGER 🌟
                        TempData["ErrorType"] = "success";
                        TempData["Message"] = "Staff member deleted successfully!";
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
                    TempData["Message"] = "Cannot delete this Staff member because they are currently assigned to existing Meetings. Please remove them from the meetings first.";
                
                    // Reload the GET Delete view to stay on the page and show the popup
                    return Delete(id); 
                }
                else
                {
                    // Catch-all for other DB errors
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "A database error occurred while trying to delete the staff member.";
                    return Delete(id); 
                }
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Helper Methods
        // ---------------------------------------------------------
        // --- Helper for Dropdown ---
        // ---------------------------------------------------------
        private List<SelectListItem> GetDepartmentList()
        {
            var list = new List<SelectListItem>();
            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("sp_GetAllDepartments", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new SelectListItem
                            {
                                Text = reader["DepartmentName"].ToString(),
                                Value = reader["DepartmentID"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }
        #endregion
    }
}