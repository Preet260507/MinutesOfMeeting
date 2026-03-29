using Microsoft.AspNetCore.Mvc;
using MOM_Project.Models;
using MySqlConnector;
using System.Data;

namespace MOM_Project.Controllers
{
    public class VenueController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string _connString;

        public VenueController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index(string searchTerm)
        {
            ViewBag.SearchTerm = searchTerm;
            return GetFilteredVenues(searchTerm ?? "");
        }

        private IActionResult GetFilteredVenues(string searchTerm)
        {
            List<MeetingVenue> list = new List<MeetingVenue>();
            using (MySqlConnection conn = new MySqlConnection(_connString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("sp_GetAllVenues", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("p_SearchTerm", searchTerm ?? "");
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new MeetingVenue
                            {
                                MeetingVenueID = reader.GetInt32("MeetingVenueID"),
                                MeetingVenueName = reader.GetString("MeetingVenueName"),
                                Created = reader.GetDateTime("Created"),
                                Modified = reader.GetDateTime("Modified")
                            });
                        }
                    }
                }
            }
            return View("Index", list);
        }

        [HttpGet]
        public IActionResult AddEdit(int? id)
        {
            MeetingVenue model = new MeetingVenue();

            if (id.HasValue && id > 0)
            {
                using (MySqlConnection conn = new MySqlConnection(_connString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("sp_GetVenueById", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("p_ID", id);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                model.MeetingVenueID = reader.GetInt32("MeetingVenueID");
                                model.MeetingVenueName = reader.GetString("MeetingVenueName");
                            }
                            else
                            {
                                return NotFound();
                            }
                        }
                    }
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddEdit(MeetingVenue model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(_connString))
                    {
                        conn.Open();
                        bool isNew = model.MeetingVenueID == 0;
                        string spName = isNew ? "sp_InsertVenue" : "sp_UpdateVenue";

                        using (MySqlCommand cmd = new MySqlCommand(spName, conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;

                            if (!isNew)
                                cmd.Parameters.AddWithValue("p_ID", model.MeetingVenueID);

                            cmd.Parameters.AddWithValue("p_Name", model.MeetingVenueName);
                            cmd.ExecuteNonQuery();
                        }

                        TempData["ErrorType"] = "success";
                        TempData["Message"] = isNew ? "Venue added successfully!" : "Venue updated successfully!";
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    TempData["ErrorType"] = "error";
                    TempData["Message"] = "An error occurred while saving the Venue.";
                }
            }
            return View(model);
        }

        public IActionResult Delete(int? id)
        {
            if (id == null) return NotFound();
            MeetingVenue model = new MeetingVenue();

            using (MySqlConnection connection = new MySqlConnection(_connString))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand("sp_GetVenueById", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("p_ID", id);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.MeetingVenueID = reader.GetInt32("MeetingVenueID");
                            model.MeetingVenueName = reader.GetString("MeetingVenueName");

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
                    using (MySqlCommand command = new MySqlCommand("sp_DeleteVenue", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("p_ID", id);
                        command.ExecuteNonQuery();

                        TempData["ErrorType"] = "success";
                        TempData["Message"] = "Venue deleted successfully!";
                    }
                }
            }
            catch (MySqlException ex)
            {
                TempData["ErrorType"] = "error";
                if (ex.Number == 1451)
                {
                    TempData["Message"] = "Cannot delete this Venue because it is currently assigned to existing Meetings. Please reassign or delete those meetings first.";
                }
                else
                {
                    TempData["Message"] = "A database error occurred while trying to delete the Venue.";
                }
                return Delete(id);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}