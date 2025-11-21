using System;
using System.ComponentModel.DataAnnotations;

namespace gurujiRide.Models
{
    public class RideRequest
    {
        public int Id { get; set; }
    [Required]
    [StringLength(100)]
    public string? Pickup { get; set; }

    [Required]
    [StringLength(100)]
    public string? Dropoff { get; set; }

    [Required]
    [StringLength(100)]
    public string? Name { get; set; }

    [Required]
    [StringLength(200)]
    public string? Contact { get; set; }
        // Date (date part) - bound from <input type="date">
        public DateTime? Date { get; set; }
        // Time (time of day) - bound from <input type="time">
        public TimeSpan? Time { get; set; }
        // When the request was created (UTC)
        public DateTime CreatedAt { get; set; }
    }
}
