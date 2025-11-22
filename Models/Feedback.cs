using System;
using System.ComponentModel.DataAnnotations;

namespace gurujiRide.Models
{
    public class Feedback
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string? Email { get; set; }

        [Required]
        [StringLength(1000)]
        public string? Message { get; set; }

        [StringLength(100)]
        public string? Category { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
