using Microsoft.AspNetCore.Mvc;
using MOM_Project.Models;
using MySqlConnector;
using System.Data;

namespace MOM_Project.Controllers
{
    public class DepartmentController : Controller
    {
        #region Iconfiguration
        private readonly IConfiguration _configuration;
        private readonly string _connString;

        public DepartmentController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
        }
        #endregion

        #region Index
        public IActionResult Index()
        {
            return GetFilteredDepartments("");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(IFormCollection form)
        {
            string searchTerm = form["searchTerm"].ToString();
            ViewBag.SearchTerm = searchTerm;
            return GetFilteredDepartments(searchTerm);
        }
        #endregion

        #region GetFilteredDepartments
        private IActionResult GetFilteredDepartments(string searchTerm)
        {
            List<Department> list = new List<Department>();

            using (MySqlConnection connection = new MySqlConnection(_connString))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand("sp_GetAllDepartments", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_SearchTerm", searchTerm ?? "");
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new Department
                            {
                                DepartmentID = reader.GetInt32("DepartmentID"),
                                DepartmentName = reader.GetString("DepartmentName"),
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

        #region AddEditDepartment
                public IActionResult AddEdit(int? id)
        {
            Department department = new Department();

            if (id.HasValue && id > 0)
            {
                using (MySqlConnection conn = new MySqlConnection(_connString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("sp_GetDepartmentById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_DepartmentID", id);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
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
                    }
                }
            }

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
                    using (MySqlConnection conn = new MySqlConnection(_connString))
                    {
                        conn.Open();
                        bool isNew = department.DepartmentID == 0;
                        string spName = isNew ? "sp_InsertDepartment" : "sp_UpdateDepartment";

                        using (MySqlCommand cmd = new MySqlCommand(spName, conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            if (!isNew)
                                cmd.Parameters.AddWithValue("p_DepartmentID", department.DepartmentID);

                            cmd.Parameters.AddWithValue("p_DepartmentName", department.DepartmentName);
                            cmd.ExecuteNonQuery();
                        }

                        TempData["ErrorType"] = "success";
                        TempData["Message"] = isNew ? "Department added successfully!" : "Department updated successfully!";
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (MySqlException)
                {
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "An error occurred while saving the department.";
                }
            }
            return View(department);
        }
        #endregion

        #region Delete
                public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();
            Department model = new Department();

            using (MySqlConnection connection = new MySqlConnection(_connString))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand("sp_GetDepartmentById", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_DepartmentID", id);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.DepartmentID = reader.GetInt32("DepartmentID");
                            model.DepartmentName = reader.GetString("DepartmentName");
                            model.Created = reader.GetDateTime("Created");
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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connString))
                {
                    connection.Open();
                    using (MySqlCommand command = new MySqlCommand("sp_DeleteDepartment", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_ID", id);
                        command.ExecuteNonQuery();

                        TempData["ErrorType"] = "success";
                        TempData["Message"] = "Department deleted successfully!";
                    }
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1451)
                {
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "Cannot delete this Department because it is currently assigned to existing Staff. Please reassign them first.";
                    return Delete(id);
                }
                throw;
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion

    }
}