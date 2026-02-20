using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MOM_Project.Models;
using MySqlConnector;
using System.Data;

namespace MOM_Project.Controllers
{
    public class MeetingMemberController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connString;

        public MeetingMemberController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
        }

        // ---------------------------------------------------------
        // 1. INDEX: Grid of All Meetings
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
                                MeetingVenueName = reader["MeetingVenueName"].ToString(),
                                IsCancelled = Convert.ToBoolean(reader["IsCancelled"]),
                                ParticipantCount = Convert.ToInt32(reader["ParticipantCount"])
                            });
                        }
                    }
                }
            }
            return View(list);
        }

        // ---------------------------------------------------------
        // 2. MANAGE (GET): Show Members + Add Form
        // ---------------------------------------------------------
        public IActionResult Manage(int id)
        {
            ViewBag.MeetingID = id;
            var currentMembers = new List<Staff>();
            var availableStaff = new List<SelectListItem>();
            
            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();

                // A. Get Meeting Details (To check if Editable)
                using (MySqlCommand cmd = new MySqlCommand("sp_GetMeetingById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_ID", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ViewBag.MeetingTitle = reader["MeetingDescription"].ToString();
                            ViewBag.MeetingDate = Convert.ToDateTime(reader["MeetingDate"]);
                            
                            bool isCancelled = Convert.ToBoolean(reader["IsCancelled"]);
                            DateTime date = Convert.ToDateTime(reader["MeetingDate"]);

                            // Logic: Editable ONLY if Upcoming AND Not Cancelled
                            ViewBag.IsEditable = !isCancelled && date > DateTime.Now;
                        }
                        else return NotFound();
                    }
                }

                // B. Get Current Members (For the List)
                using (MySqlCommand cmd = new MySqlCommand("sp_GetMeetingMembers", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_MeetingID", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            currentMembers.Add(new Staff
                            {
                                StaffID = Convert.ToInt32(reader["StaffID"]),
                                StaffName = reader["StaffName"].ToString(),
                                EmailAddress = reader["EmailAddress"].ToString(),
                                DepartmentName = reader["DepartmentName"].ToString()
                            });
                        }
                    }
                }

                // C. Get Available Staff (For the Dropdown) - Only if Editable
                if (ViewBag.IsEditable)
                {
                    using (MySqlCommand cmd = new MySqlCommand("sp_GetAvailableStaff", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_MeetingID", id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                availableStaff.Add(new SelectListItem
                                {
                                    Text = reader["StaffName"].ToString(),
                                    Value = reader["StaffID"].ToString()
                                });
                            }
                        }
                    }
                }
            }

            ViewBag.AvailableStaff = availableStaff;
            return View(currentMembers);
        }

        // ---------------------------------------------------------
        // 3. ADD MEMBER (POST)
        // ---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMember(int meetingId, int staffId)
        {
            if (staffId > 0)
            {
                using (MySqlConnection conn = new MySqlConnection(_connString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("sp_AddMeetingMember", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_MeetingID", meetingId);
                        cmd.Parameters.AddWithValue("p_StaffID", staffId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            return RedirectToAction("Manage", new { id = meetingId });
        }

        // ---------------------------------------------------------
        // 4. REMOVE MEMBER (POST)
        // ---------------------------------------------------------
        #region Remove Member
        // ---------------------------------------------------------
        // 4. REMOVE (GET): Show Confirmation Page
        // ---------------------------------------------------------
        public IActionResult RemoveMember(int meetingId, int staffId)
        {
            Staff staff = new Staff();
            ViewBag.MeetingID = meetingId;

            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();

                // A. Get Meeting Details (for the warning message)
                using (MySqlCommand cmd = new MySqlCommand("sp_GetMeetingById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_ID", meetingId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ViewBag.MeetingTitle = reader["MeetingDescription"].ToString();
                            ViewBag.MeetingDate = Convert.ToDateTime(reader["MeetingDate"]).ToString("MMM dd, yyyy");
                        }
                    }
                }

                // B. Get Staff Details (to show who we are removing)
                using (MySqlCommand cmd = new MySqlCommand("sp_GetStaffById", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_ID", staffId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            staff.StaffID = Convert.ToInt32(reader["StaffID"]);
                            staff.StaffName = reader["StaffName"].ToString();
                            staff.EmailAddress = reader["EmailAddress"].ToString();
                            // If your sp_GetStaffById joins the department table, you can fetch it here.
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
        // 5. REMOVE (POST): Actually Remove the Member
        // ---------------------------------------------------------
        [HttpPost, ActionName("RemoveMember")]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveMemberConfirmed(int meetingId, int staffId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(_connString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("sp_RemoveMember", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_MeetingID", meetingId);
                        cmd.Parameters.AddWithValue("p_StaffID", staffId);
                        cmd.ExecuteNonQuery();
                    }
                }

                // 🌟 SUCCESS POPUP TRIGGER 🌟
                TempData["ErrorType"] = "success";
                TempData["Message"] = "Participant removed successfully!";
            }
            catch (Exception ex)
            {
                // 🚨 ERROR POPUP TRIGGER 🚨
                TempData["ErrorType"] = "error";
                TempData["Message"] = "An error occurred while removing the participant.";
            }

            // Go back to the Manage page for this specific meeting
            return RedirectToAction("Manage", new { id = meetingId });
        }
        #endregion
    }
}