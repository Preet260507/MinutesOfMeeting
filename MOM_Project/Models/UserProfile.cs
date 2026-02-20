using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MOM_Project.Models
{
    public class UserProfile
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Department { get; set; }

        // Stores the text path in the database (e.g., "/images/my-pic.jpg")
        public string? ProfileImagePath { get; set; }

        // Handles the actual file upload from the HTML form (Not saved to DB directly)
        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePictureUpload { get; set; }
    }
}