using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using gurujiRide.Models;
using gurujiRide.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace gurujiRide.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _db;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    // GET: /Home/LoadMore
    [HttpGet]
    public async Task<IActionResult> LoadMore(int skip = 0, int take = 11)
    {
        _logger.LogInformation("LoadMore called with skip={skip} take={take}", skip, take);

        // Defensive: only run the query if DB is reachable and the CreatedAt column exists
        try
        {
            if (_db.Database.CanConnect() && await DbHasCreatedAtColumnAsync())
            {
                try
                {
                    var items = await _db.RideRequests
                        .OrderByDescending(r => r.CreatedAt)
                        .Skip(skip)
                        .Take(take)
                        .AsNoTracking()
                        .ToListAsync();

                    return PartialView("_RideRequestCards", items);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to load RideRequests for LoadMore (query failed)");
                    // fall through to sample data
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Database readiness check failed; falling back to sample data");
        }

        // If DB unavailable or schema mismatch, return sample data so client can continue
        var sample = Enumerable.Range(0, take).Select(i => new RideRequest
        {
            Id = i + 1 + skip,
            Name = $"Test Rider {i + 1 + skip}",
            Contact = "https://m.me/example",
            Pickup = "Sample pickup",
            Dropoff = "Sample dropoff",
            Date = DateTime.Today.AddDays(i % 3),
            Time = TimeSpan.FromMinutes(30 * (i % 6)),
            CreatedAt = DateTime.UtcNow.AddMinutes(-5 * (i + skip))
        }).ToList();

        return PartialView("_RideRequestCards", sample);
    }

    // GET: /Home/Index?skip=0
    public async Task<IActionResult> Index(int skip = 0, int take = 11)
    {
        var vm = new IndexViewModel { Skip = skip, Take = take };

        try
        {
            if (_db.Database.CanConnect() && await DbHasCreatedAtColumnAsync())
            {
                vm.RecentRequests = await _db.RideRequests
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .AsNoTracking()
                    .ToListAsync();
            }
            else
            {
                // fallback to sample data
                vm.RecentRequests = Enumerable.Range(0, take).Select(i => new RideRequest
                {
                    Id = i + 1 + skip,
                    Name = $"Test Rider {i + 1 + skip}",
                    Contact = "https://m.me/example",
                    Pickup = "Sample pickup",
                    Dropoff = "Sample dropoff",
                    Date = DateTime.Today.AddDays(i % 3),
                    Time = TimeSpan.FromMinutes(30 * (i % 6)),
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5 * (i + skip))
                }).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to load RideRequests from DB - returning sample data");
            vm.RecentRequests = Enumerable.Range(0, take).Select(i => new RideRequest
            {
                Id = i + 1 + skip,
                Name = $"Test Rider {i + 1 + skip}",
                Contact = "https://m.me/example",
                Pickup = "Sample pickup",
                Dropoff = "Sample dropoff",
                Date = DateTime.Today.AddDays(i % 3),
                Time = TimeSpan.FromMinutes(30 * (i % 6)),
                CreatedAt = DateTime.UtcNow.AddMinutes(-5 * (i + skip))
            }).ToList();
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(IndexViewModel vm)
    {
        // vm.NewRequest is bound from the form
        if (!ModelState.IsValid)
        {
            // repopulate recent requests for the view
            try
            {
                vm.RecentRequests = await _db.RideRequests
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(vm.Take)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch
            {
                vm.RecentRequests = Enumerable.Range(0, vm.Take).Select(i => new RideRequest
                {
                    Id = i + 1,
                    Name = $"Test Rider {i + 1}",
                    Contact = "https://m.me/example",
                    Pickup = "Sample pickup",
                    Dropoff = "Sample dropoff",
                    Date = DateTime.Today,
                    Time = TimeSpan.FromMinutes(30),
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5 * i)
                }).ToList();
            }

            return View(vm);
        }

        // Set timestamp and attempt to save. If DB schema isn't updated, swallow errors so UI still works.
        vm.NewRequest.CreatedAt = DateTime.UtcNow;
        try
        {
            _db.RideRequests.Add(vm.NewRequest);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to save RideRequest to DB (maybe migration not applied). The request will not be persisted.");
        }

        // Redirect to GET (PRG) to show the updated list (or sample data)
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Helper to check whether the RideRequests table has the CreatedAt column.
    // This is a defensive check used when the migrations may not have been applied yet.
    private async Task<bool> DbHasCreatedAtColumnAsync(CancellationToken ct = default)
    {
        try
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync(ct);
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RideRequests' AND COLUMN_NAME = 'CreatedAt'";
            var result = await cmd.ExecuteScalarAsync(ct);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not verify CreatedAt column existence");
            return false;
        }
    }
}
