using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MOM_Project.Models
{
    [Table("MOM_MeetingVenue")]
    public class MeetingVenue
    {
        [Key]
        public int MeetingVenueID { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Venue Name")]
        public string MeetingVenueName { get; set; }

        public DateTime? Created { get; set; }
        public DateTime? Modified { get; set; }
    }
}