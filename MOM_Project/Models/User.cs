using System.ComponentModel.DataAnnotations;

namespace MOM_Project.Models
{
    public class UserModel
    {
        [Required(ErrorMessage = "Please enter your username.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Please enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}