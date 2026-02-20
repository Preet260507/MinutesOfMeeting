using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MOM_Project.Models
{
    [Table("MOM_MeetingType")]
    public class MeetingType
    {
        [Key]
        public int MeetingTypeID { get; set; }

        [Required(ErrorMessage = "Meeting Type Name is required.")]
        [Display(Name = "Meeting Type")]
        public string MeetingTypeName { get; set; }

        // NEW FIELD
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}