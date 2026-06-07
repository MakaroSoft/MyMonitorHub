using System.ComponentModel.DataAnnotations;

namespace MyMonitorHub.Domain.Models
{
    public class UserModel
    {
        public int UserId { get; set; }

        // Password is optional on the profile form (blank = keep existing).
        // Validation lives in the controller so an empty string never trips a
        // data-annotation error that would surface as "required" in the UI.
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Email Address")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Home Phone Number")]
        [Phone]
        public string HomePhone { get; set; }

        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Work Phone Number")]
        [Phone]
        public string WorkPhone { get; set; }

        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Mobile Phone Number")]
        [Phone]
        public string CellPhone { get; set; }
    }
}