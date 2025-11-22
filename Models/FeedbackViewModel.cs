using System;

namespace gurujiRide.Models
{
    public class FeedbackViewModel
    {
        public Feedback NewFeedback { get; set; } = new();

        // After a successful POST we can show a simple success message
        public bool Submitted { get; set; }
    }
}
