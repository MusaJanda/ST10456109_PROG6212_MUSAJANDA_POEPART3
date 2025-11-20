using System.ComponentModel.DataAnnotations;

namespace CMCS.ViewModels
{
    public class EditUserViewModel
    {
        public int Id { get; set; }

        [Required]
        public string UserType { get; set; } // Lecturer, ProgrammeCoordinator, AcademicManager

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required]
        public string Department { get; set; }

        [Display(Name = "Created Date")]
        [DataType(DataType.Date)]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Hourly Rate")]
        [Range(0, 1000, ErrorMessage = "Hourly rate must be between 0 and 1000")]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Active Account")]
        public bool IsActive { get; set; } = true;
    }
}