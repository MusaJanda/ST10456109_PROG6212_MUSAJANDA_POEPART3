using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CMCS.Models
{
    public class ProgrammeCoordinator
    {
        [Key]
        public int CoordinatorId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string? PhoneNumber { get; set; }
        public string Email { get; set; }

        public string? Department { get; set; }

        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser ApplicationUser { get; set; }
    }
}
