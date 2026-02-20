using System.ComponentModel.DataAnnotations;

namespace MOM_Project.Models
{
    public class Meeting
    {
        public int MeetingID { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        [Display(Name = "Meeting Subject")]
        public string MeetingDescription { get; set; }

        [Required(ErrorMessage = "Date is required")]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)] // <--- ADD THIS LINE
        public DateTime MeetingDate { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Venue")]
        public int? MeetingVenueID { get; set; }

        [Display(Name = "Meeting Type")]
        public int? MeetingTypeID { get; set; }

        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        // Display Properties
        public string? MeetingVenueName { get; set; }
        public string? MeetingTypeName { get; set; }
        public bool IsCancelled { get; set; }
        public int ParticipantCount { get; set; }

        // Checkbox Helper
        public List<int> SelectedStaffIds { get; set; } = new List<int>();
    }
}