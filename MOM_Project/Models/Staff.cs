using System.ComponentModel.DataAnnotations;

namespace MOM_Project.Models
{
    public class Staff
    {
        public int StaffID { get; set; }

        [Required(ErrorMessage = "Please Enter Staff Name")]
        
        public string? StaffName { get; set; }

        [Display(Name = "Department")]
        public int? DepartmentID { get; set; }

        // Extra property just for displaying the name in the list
        public string? DepartmentName { get; set; } 

        [Phone(ErrorMessage = "Invalid Phone Number")]
        [Display(Name = "Mobile Number")]
        [Required(ErrorMessage = "Please Enter Mobile Number")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mobile Number must be exactly 10 digits.")]
        public string? MobileNo { get; set; }
        
        [EmailAddress]
        [Required(ErrorMessage = "Please Enter Email Address")]
        [Display(Name = "Email Address")]
        public string? EmailAddress { get; set; }

        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}