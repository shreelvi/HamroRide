using System;
using System.Collections.Generic;

namespace gurujiRide.Models
{
    public class LyftViewModel
    {
        // Filters
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public TimeSpan? TimeFrom { get; set; }
        public TimeSpan? TimeTo { get; set; }
        public string? Pickup { get; set; }
        public string? Dropoff { get; set; }

        // Sorting
        public string? SortBy { get; set; }
        public string? SortDir { get; set; } = "asc";

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }

    // Include past and missing-date rides (default: show all)
    public bool IncludeAll { get; set; } = true;

        // Results
        public List<RideRequest>? Results { get; set; }
    }
}
