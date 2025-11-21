using System.Collections.Generic;

namespace gurujiRide.Models
{
    public class IndexViewModel
    {
        public RideRequest NewRequest { get; set; } = new();
        public List<RideRequest> RecentRequests { get; set; } = new();
        public int Skip { get; set; }
        public int Take { get; set; } = 11;
    }
}
