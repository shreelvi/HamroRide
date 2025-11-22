using Microsoft.EntityFrameworkCore;
using gurujiRide.Models;

namespace gurujiRide.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<RideRequest> RideRequests { get; set; } = default!;
    public DbSet<gurujiRide.Models.Feedback> Feedbacks { get; set; } = default!;
    public DbSet<gurujiRide.Models.OfferRide> OfferRides { get; set; } = default!;
}
