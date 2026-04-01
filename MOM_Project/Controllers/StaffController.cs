using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MOM_Project.Models;
using MySqlConnector;
using System.Data;
using System;
using System.Collections.Generic;
using ClosedXML.Excel;
using System.IO;

namespace MOM_Project.Controllers
{
    public class StaffController : Controller
    {
        #region Iconfiguration
        private readonly IConfiguration _configuration;
        private readonly string _connString;

        public StaffController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
        }
        #endregion
        
        #region Export To Excel 
        public IActionResult ExportToExcel()
        {
            DataTable dt = new DataTable("StaffDirectory");
            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("sp_GetAllStaff", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_SearchTerm", ""); // Get ALL staff
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            dt.Load(reader); 
                        }
                    }
            }
            
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Staff Directory");
                var excelTable = worksheet.Cell(1, 1).InsertTable(dt.AsEnumerable(), "StaffTable", true);
                excelTable.Theme = XLTableTheme.None;
                
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Staff_Directory.xlsx");
                }
            }
        }
        #endregion
        
        #region Get All & Search
        public IActionResult Index(string searchTerm)
        {
            ViewBag.SearchTerm = searchTerm;
            return GetFilteredStaff(searchTerm ?? "");
        }

        private IActionResult GetFilteredStaff(string searchTerm)
        {
            List<Staff> list = new List<Staff>();
            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("sp_GetAllStaff", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_SearchTerm", searchTerm ?? "");
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Staff
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
            return View("Index", list);
        }
        #endregion

        #region Add/Edit Staff
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
                        
                        TempData["ErrorType"] = "success";
                        TempData["Message"] = isNew ? "Staff member added successfully!" : "Staff member updated successfully!";
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "An error occurred while saving the staff member.";
                    ModelState.AddModelError("", "Database Error: " + ex.Message);
                }
            }
            
            ViewBag.Departments = GetDepartmentList();
            return View(staff);
        }
        #endregion

        #region Delete Staff
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

                        TempData["ErrorType"] = "success";
                        TempData["Message"] = "Staff member deleted successfully!";
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1451)
                {
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "Cannot delete this Staff member because they are currently assigned to existing Meetings. Please remove them from the meetings first.";
                
                    return Delete(id); 
                }
                else
                {
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "A database error occurred while trying to delete the staff member.";
                    return Delete(id); 
                }
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion

        private SelectList GetDepartmentList()
        {
            List<SelectListItem> departments = new List<SelectListItem>();
            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("sp_GetAllDepartments", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_SearchTerm", ""); 

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            departments.Add(new SelectListItem 
                            { 
                                Value = reader["DepartmentID"].ToString(), 
                                Text = reader["DepartmentName"].ToString() 
                            });
                        }
                    }
                }
            }
            return new SelectList(departments, "Value", "Text");
        }
    }
}