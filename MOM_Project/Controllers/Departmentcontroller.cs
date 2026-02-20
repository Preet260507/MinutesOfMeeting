using Microsoft.AspNetCore.Mvc;
using MOM_Project.Models;
using MySqlConnector;
using System.Data;

namespace MOM_Project.Controllers
{
    public class DepartmentController : Controller
    {
        #region Iconfigrationinjection
        private readonly IConfiguration _configuration;
        private readonly string _connString;

        public DepartmentController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
        }
        #endregion
        
        #region GetAllDepartments
        public IActionResult Index()
        {
            List<Department> list = new List<Department>();
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
                            list.Add(new Department
                            {
                                DepartmentID = Convert.ToInt32(reader["DepartmentID"]),
                                DepartmentName = reader["DepartmentName"].ToString(),
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
        
        #region AddEditDepartment
        public IActionResult AddEdit(int? id)
        {
            Department department = new Department();

            // EDIT MODE: If ID is provided, fetch existing data
            if (id.HasValue && id > 0)
            { 
                MySqlConnection conn = new MySqlConnection(_connString);
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("sp_GetDepartmentById", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_DepartmentID", id);
                MySqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    department.DepartmentID = Convert.ToInt32(reader["DepartmentID"]);
                    department.DepartmentName = reader["DepartmentName"].ToString();
                }
                else
                {
                    return NotFound();
                }
            }
            // If ID is null (Create Mode), we return an empty object
            return View(department);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddEdit(Department department)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    MySqlConnection conn = new MySqlConnection(_connString);
                    conn.Open();
                    MySqlCommand cmd;

                    bool isNew = department.DepartmentID == 0;
                    // A. Check ID to decide Insert vs Update
                    if (department.DepartmentID == 0)
                    {
                        cmd = new MySqlCommand("sp_InsertDepartment", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                    }
                    else
                    {
                        cmd = new MySqlCommand("sp_UpdateDepartment", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_DepartmentID", department.DepartmentID);
                    }

                    // B. Add Common Parameter
                    cmd.Parameters.AddWithValue("p_DepartmentName", department.DepartmentName);

                    // C. Execute
                    cmd.ExecuteNonQuery();

                    // 🌟 SUCCESS POPUP TRIGGER 🌟
                    TempData["ErrorType"] = "success";
                    TempData["Message"] = isNew ? "Department added successfully!" : "Department updated successfully!";

                    return RedirectToAction(nameof(Index));
                }
                catch (MySqlException ex)
                {
                    // 🚨 ERROR POPUP TRIGGER 🚨
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "An error occurred while saving the department.";
                    // You can use ex.Message for debugging if you want: TempData["Message"] = ex.Message;
                }
            }
            return View(department);
        }
        #endregion

        #region DeleteDepartment

// 1. GET: Show the delete confirmation page
public IActionResult Delete(int? id)
{
    if (id == null) return NotFound();
    Department model = new Department();
    string connectionString = _configuration.GetConnectionString("DefaultConnection");
    MySqlConnection connection = new MySqlConnection(connectionString);
    connection.Open();
    MySqlCommand command = connection.CreateCommand();
    
    // Use Stored Procedure to get the single record
    command.CommandType = CommandType.StoredProcedure;
    command.CommandText = "sp_GetDepartmentById";
    // Add the ID parameter
    command.Parameters.AddWithValue("p_DepartmentID", id);
    MySqlDataReader reader = command.ExecuteReader();
    
    if (reader.Read())
    {
        model.DepartmentID = reader.GetInt32("DepartmentID");
        model.DepartmentName = reader.GetString("DepartmentName");
        model.Created = reader.GetDateTime("Created");
        model.Modified = reader.GetDateTime("Modified");
    }
    else
    {
        return NotFound(); // ID not found in database
    }
    return View(model);
}

// 2. POST: Actually delete the record
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public IActionResult DeleteConfirmed(int id)
{
    string connectionString = _configuration.GetConnectionString("DefaultConnection");
    MySqlConnection connection = new MySqlConnection(connectionString);
    connection.Open();
    MySqlCommand command = connection.CreateCommand();

    command.CommandType = CommandType.StoredProcedure;
    command.CommandText = "sp_DeleteDepartment";
    // Add the ID parameter to delete
    command.Parameters.AddWithValue("p_ID", id);
    
    try
    {
        // Try to delete
        command.ExecuteNonQuery();

        // 🌟 TRIGGER SUCCESS POPUP 🌟
        TempData["ErrorType"] = "success";
        TempData["Message"] = "Department deleted successfully!";
    }
    catch (MySqlException ex)
    {
        // Check for "Foreign Key Constraint" error (Error Code 1451)
        if (ex.Number == 1451)
        {
            // 🚨 TRIGGER ERROR POPUP 🚨
            // Replaced ViewBag with TempData so SweetAlert picks it up
            TempData["ErrorType"] = "error";
            TempData["Message"] = "Cannot delete this Department because it is currently assigned to existing Staff. Please reassign them first.";
        
            // We need to reload the Department data to show the View again
            return Delete(id); 
        }
        else
        {
            // If it's some other error, throw it so we know
            throw;
        }
    }

    return RedirectToAction(nameof(Index));
}

#endregion
    }
}