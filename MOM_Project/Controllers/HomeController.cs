using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using System.Text.Json;

namespace MOM_Project.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connString;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index()
        {
            // 1. Check Login
            if (HttpContext.Session.GetString("AdminUser") == null) 
                return RedirectToAction("Index", "Login");

            // Variables for the View
            var chartLabels = new List<string>();
            var chartValues = new List<int>();

            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();

                // --- A. GET DASHBOARD COUNTS ---
                using (MySqlCommand cmd = new MySqlCommand("sp_GetDashboardStats", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ViewBag.TotalMeetings = reader["TotalMeetings"];
                            ViewBag.TotalStaff = reader["TotalStaff"]; // Works now!
                            ViewBag.Venues = reader["TotalVenues"];
                            ViewBag.Upcoming = reader["Upcoming"];
                        }
                    }
                }

                // --- B. GET CHART DATA ---
                using (MySqlCommand cmd = new MySqlCommand("sp_GetChartData", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            chartLabels.Add(reader["MonthName"].ToString());
                            chartValues.Add(Convert.ToInt32(reader["MeetingCount"]));
                        }
                    }
                }
            }

            // Serialize for Chart.js
            ViewBag.ChartLabels = JsonSerializer.Serialize(chartLabels);
            ViewBag.ChartData = JsonSerializer.Serialize(chartValues);

            return View();
        }
    }
}