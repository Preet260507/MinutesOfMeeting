using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using System.Text.Json;
using MOM_Project.Filters;   // 🌟 Added this
using MOM_Project.Services;  // 🌟 Added this

namespace MOM_Project.Controllers
{
    [ServiceFilter(typeof(CheckAccessFilter))]
    public class HomeController : Controller
    {
        #region Iconfiguration
        private readonly IConfiguration _configuration;
        private readonly string _connString;
        private readonly AuthService _authService; 
        
        public HomeController(IConfiguration configuration, AuthService authService)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
            _authService = authService;
        }
        #endregion
        
        #region Dashboard
        public IActionResult Index()
        {
            
            ViewBag.UserName = _authService.GetUserName();
            
            var chartLabels = new List<string>();
            var chartValues = new List<int>();

            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();

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

            ViewBag.ChartLabels = JsonSerializer.Serialize(chartLabels);
            ViewBag.ChartData = JsonSerializer.Serialize(chartValues);

            return View();
        }
        #endregion

    }
}