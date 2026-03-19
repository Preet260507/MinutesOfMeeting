using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using System.Text.Json;
using MOM_Project.Filters;   // 🌟 Added this
using MOM_Project.Services;  // 🌟 Added this

namespace MOM_Project.Controllers
{
    // 🌟 1. This single tag locks down the ENTIRE dashboard!
    [ServiceFilter(typeof(CheckAccessFilter))]
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connString;
        private readonly AuthService _authService; // 🌟 2. Added AuthService

        // 🌟 3. Injected AuthService alongside your Configuration
        public HomeController(IConfiguration configuration, AuthService authService)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
            _authService = authService;
        }

        public IActionResult Index()
        {
            // 🌟 4. NOTICE WHAT'S MISSING? The manual session check and RedirectToAction are GONE!
            // The [ServiceFilter] handled it before this method even started.

            // Optional: Grab the user's name so you can say "Welcome back, {Name}" on the dashboard
            ViewBag.UserName = _authService.GetUserName();

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
                            ViewBag.TotalStaff = reader["TotalStaff"]; 
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