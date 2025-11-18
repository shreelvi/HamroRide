using System;

namespace gurujiRide.Models
{
    public class RideRequest
    {
        public int Id { get; set; }
        public string? Pickup { get; set; }
        public string? Dropoff { get; set; }
        // Date (date part) - bound from <input type="date">
        public DateTime? Date { get; set; }
        // Time (time of day) - bound from <input type="time">
        public TimeSpan? Time { get; set; }
    }
}
