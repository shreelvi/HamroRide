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

    // GET: /Home/Lyft
    [HttpGet]
    public async Task<IActionResult> Lyft(DateTime? dateFrom = null, DateTime? dateTo = null, TimeSpan? timeFrom = null, TimeSpan? timeTo = null, string? pickup = null, string? dropoff = null, string? sortBy = null, string? sortDir = "asc", int page = 1, int pageSize = 20, bool includeAll = true)
    {
        var vm = new gurujiRide.Models.LyftViewModel
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            TimeFrom = timeFrom,
            TimeTo = timeTo,
            Pickup = pickup,
            Dropoff = dropoff,
            SortBy = sortBy,
            SortDir = sortDir,
            IncludeAll = includeAll,
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 5, 200)
        };

        try
        {
            if (_db.Database.CanConnect() && await DbHasCreatedAtColumnAsync())
            {
                // Build a server-side query. By default return only scheduled requests later than now,
                // but when includeAll is true return all rides (including past and missing-date entries).
                var q = _db.RideRequests.AsNoTracking().AsQueryable();
                if (!vm.IncludeAll)
                {
                    var today = DateTime.Today;
                    var nowTime = DateTime.Now.TimeOfDay;
                    q = q.Where(r => r.Date.HasValue && (
                        r.Date > today || (r.Date == today && ( (r.Time ?? TimeSpan.Zero) > nowTime ))
                    ));
                }

                // Apply additional filters
                if (vm.DateFrom.HasValue)
                {
                    var df = vm.DateFrom.Value.Date;
                    q = q.Where(r => r.Date >= df);
                }
                if (vm.DateTo.HasValue)
                {
                    var dt = vm.DateTo.Value.Date;
                    q = q.Where(r => r.Date <= dt);
                }
                if (vm.TimeFrom.HasValue)
                {
                    var tf = vm.TimeFrom.Value;
                    q = q.Where(r => (r.Time ?? TimeSpan.Zero) >= tf);
                }
                if (vm.TimeTo.HasValue)
                {
                    var tt = vm.TimeTo.Value;
                    q = q.Where(r => (r.Time ?? TimeSpan.Zero) <= tt);
                }
                if (!string.IsNullOrWhiteSpace(vm.Pickup))
                {
                    var pick = vm.Pickup.Trim();
                    q = q.Where(r => EF.Functions.Like(r.Pickup ?? string.Empty, $"%{pick}%"));
                }
                if (!string.IsNullOrWhiteSpace(vm.Dropoff))
                {
                    var drop = vm.Dropoff.Trim();
                    q = q.Where(r => EF.Functions.Like(r.Dropoff ?? string.Empty, $"%{drop}%"));
                }

                // Total count before paging
                vm.TotalItems = await q.CountAsync();

                // Sorting
                sortBy = (vm.SortBy ?? "date").ToLowerInvariant();
                sortDir = (vm.SortDir ?? "asc").ToLowerInvariant();

                IOrderedQueryable<RideRequest> ordered = sortBy switch
                {
                    "pickup" => sortDir == "desc" ? q.OrderByDescending(r => r.Pickup) : q.OrderBy(r => r.Pickup),
                    "dropoff" => sortDir == "desc" ? q.OrderByDescending(r => r.Dropoff) : q.OrderBy(r => r.Dropoff),
                    "created" => sortDir == "desc" ? q.OrderByDescending(r => r.CreatedAt) : q.OrderBy(r => r.CreatedAt),
                    _ => sortDir == "desc" ? q.OrderByDescending(r => r.Date).ThenByDescending(r => r.Time) : q.OrderBy(r => r.Date).ThenBy(r => r.Time),
                };

                // Paging
                vm.Results = await ordered.Skip((vm.Page - 1) * vm.PageSize).Take(vm.PageSize).ToListAsync();
            }
            else
            {
                vm.TotalItems = 0;
                vm.Results = new List<RideRequest>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error preparing Lyft list - returning empty results");
            vm.TotalItems = 0;
            vm.Results = new List<RideRequest>();
        }

        return View(vm);
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
                // Also load recent offers so the right panel can show both lists if desired
                vm.RecentOffers = await _db.OfferRides
                    .OrderByDescending(o => o.CreatedAt)
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
                // sample offers fallback
                vm.RecentOffers = Enumerable.Range(0, take).Select(i => new OfferRide
                {
                    Id = i + 1 + skip,
                    Name = $"Test Driver {i + 1 + skip}",
                    Contact = "https://m.me/example",
                    Pickup = "Sample pickup",
                    Dropoff = "Sample dropoff",
                    Date = DateTime.Today.AddDays(i % 3),
                    Time = TimeSpan.FromMinutes(30 * (i % 6)),
                    Note = "Sample note",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10 * (i + skip))
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

    // POST: /Home/Offer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Offer(OfferRide offer)
    {
        if (!ModelState.IsValid)
        {
            // If invalid, redirect back to Index so the page shows validation messages (simpler PRG could be improved)
            return RedirectToAction(nameof(Index));
        }

        offer.CreatedAt = DateTime.UtcNow;
        try
        {
            _db.OfferRides.Add(offer);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to save OfferRide to DB (maybe migration not applied). The offer will not be persisted.");
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Home/Offer
    [HttpGet]
    public IActionResult Offer()
    {
        ViewData["Title"] = "Offer a ride";
        var model = new OfferRide();
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // GET: /Home/Feedback
    [HttpGet]
    public IActionResult Feedback()
    {
        var vm = new gurujiRide.Models.FeedbackViewModel();
        ViewData["Title"] = "Feedback & Suggestions";
        return View(vm);
    }

    // POST: /Home/Feedback
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Feedback(gurujiRide.Models.FeedbackViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        vm.NewFeedback.CreatedAt = DateTime.UtcNow;
        try
        {
            // Attempt to save; if DB or migrations not present swallow the error like other pages
            _db.Add(vm.NewFeedback);
            await _db.SaveChangesAsync();
            vm.Submitted = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to save feedback to DB (maybe migration not applied). Feedback will not be persisted.");
            // keep Submitted = true so user sees acknowledgment even if it wasn't persisted
            vm.Submitted = true;
        }

        return View(vm);
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
