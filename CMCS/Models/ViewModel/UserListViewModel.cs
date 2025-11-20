using System;
using System.ComponentModel.DataAnnotations;

namespace CMCS.ViewModels
{
    public class UserListViewModel
    {
        [Display(Name = "First Name")]
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; }

        [Display(Name = "Email Address")]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Display(Name = "Phone Number")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Department")]
        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
        public string Department { get; set; }

        [Display(Name = "Hourly Rate")]
        [Range(0, 1000, ErrorMessage = "Hourly rate must be between 0 and 1000")]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Role")]
        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; }

        [Display(Name = "Created Date")]
        [DataType(DataType.Date)]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Lecturer ID")]
        public int? LecturerId { get; set; }

        // Computed property for full name
        public string FullName => $"{FirstName} {LastName}";

        public bool IsActive { get; set; }
        public int? CoordinatorId { get;  set; }

        public int? ManagerId { get; set; }
    }
}