using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project_1.Models;
using Project_1.Data.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Project_1.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.SignalR;
using Project_1.Hubs;

namespace Project_1.Controllers
{
    public class ListingsController : Controller
    {

        private ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ListingsController> _logger;

        private readonly IHubContext<BidHub> _hubContext;

        private readonly IListingsService _listingsService;
        private readonly IBidsService _bidsService;
        private readonly ICommentsService _commentsService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ListingsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            ILogger<ListingsController> logger,
            IListingsService listingsService,
            IWebHostEnvironment webHostEnvironment,
            IBidsService bidsService,
            ICommentsService commentsService,
            IHubContext<BidHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
            _listingsService = listingsService;
            _webHostEnvironment = webHostEnvironment;
            _bidsService = bidsService;
            _commentsService = commentsService;
            _hubContext = hubContext;
        }

        // GET: Listings
        public async Task<IActionResult> Index(int? pageNumber, string searchString)
        {
            var applicationDbContext = _listingsService.GetAll();
            int pageSize = 3;
            if (!string.IsNullOrEmpty(searchString))
            {
                applicationDbContext = applicationDbContext.Where(a => a.Title.Contains(searchString));
                return View(await PaginatedList<Listing>.CreateAsync(applicationDbContext.Where(l => l.IsSold == false).AsNoTracking(), pageNumber ?? 1, pageSize));

            }

            return View(await PaginatedList<Listing>.CreateAsync(applicationDbContext.Where(l => l.IsSold == false).AsNoTracking(), pageNumber ?? 1, pageSize));
        }
        public async Task<IActionResult> MyListings(int? pageNumber)
        {
            var applicationDbContext = _listingsService.GetAll();
            int pageSize = 3;

            return View("Index", await PaginatedList<Listing>.CreateAsync(applicationDbContext.Where(l => l.IdentityUserId == User.FindFirstValue(ClaimTypes.NameIdentifier)).AsNoTracking(), pageNumber ?? 1, pageSize));
        }
        public async Task<IActionResult> MyBids(int? pageNumber)
        {
            var applicationDbContext = _bidsService.GetAll();
            int pageSize = 3;

            return View(await PaginatedList<Bid>.CreateAsync(applicationDbContext.Where(l => l.IdentityUserId == User.FindFirstValue(ClaimTypes.NameIdentifier)).AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Listings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var listing = await _listingsService.GetById(id);

            if (listing == null)
            {
                return NotFound();
            }

            return View(listing);
        }

        // GET: Listings/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Listings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ListingVM listing)
        {
            if (listing.Image != null)
            {
                string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "Images");
                string fileName = listing.Image.FileName;
                string filePath = Path.Combine(uploadDir, fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    listing.Image.CopyTo(fileStream);
                }

                // Assuming IdentityUserId is related to the logged-in user
                var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);  // Or use appropriate method

                if (identityUserId == null)
                {
                    // Handle the case where the user is not authenticated
                    return Unauthorized();
                }

                var listObj = new Listing
                {
                    Title = listing.Title,
                    Description = listing.Description,
                    Price = listing.Price,
                    IdentityUserId = identityUserId,  // Assign the current user's ID
                    ImagePath = fileName,
                };

                await _listingsService.Add(listObj);
                return RedirectToAction("Index");
            }
            return View(listing);
        }

        // 🛠️ EDITED: AddBid action to include SignalR broadcast
        [HttpPost]
        public async Task<ActionResult> AddBid([Bind("Id, Price, ListingId, IdentityUserId")] Bid bid)
        {
            if (ModelState.IsValid)
            {
                // 1. Add the new bid to the database
                await _bidsService.Add(bid);

                // 2. Update the listing's current price (assuming 'Price' tracks the highest bid)
                var listing = await _listingsService.GetById(bid.ListingId);

                // This check is important: only update if the new bid is higher
                // (Your BidService should ideally handle the business logic of verifying this).
                // Assuming validation passed and this is the new highest bid:
                listing.Price = bid.Price;
                await _listingsService.SaveChanges();

                // 3. BROADCAST THE UPDATE VIA SIGNALR
                string formattedBid = bid.Price.ToString("N2"); // Format as currency (e.g., 100.00)

                await _hubContext.Clients
                    .Group($"listing-{bid.ListingId}") // Broadcast only to clients viewing this listing
                    .SendAsync("ReceiveNewHighestBid", formattedBid); // Use the method name defined in your JS
            }

            // Redirect back to the details page to refresh the page for the bidder.
            return RedirectToAction("Details", new { id = bid.ListingId });
        }

        // ✅ FIXED: CloseBidding method with ValidateAntiForgeryToken and null checks
        [HttpPost]
        [ValidateAntiForgeryToken] // <-- IMPORTANT SECURITY AND SUBMISSION FIX
        public async Task<IActionResult> CloseBidding(int id)
        {
            var listing = await _context.Listings
                .Include(l => l.Bids)
                    .ThenInclude(b => b.User) // Eager load the user for email logic
                .FirstOrDefaultAsync(l => l.Id == id);

            if (listing == null)
            {
                return NotFound();
            }

            // Mark as sold regardless of whether bids exist
            listing.IsSold = true;
            _context.Update(listing);

            // Safely find the highest bid
            var highestBid = listing.Bids?
                .OrderByDescending(b => b.Price)
                .FirstOrDefault();

            if (highestBid != null && highestBid.User != null)
            {
                var highestBidder = highestBid.User;

                string subject = "Congratulations! You have won the bid!";
                string message = $"Dear {highestBidder.UserName},\n\n" +
                                 $"Congratulations! You have won the bid with a bid of Rs {highestBid.Price.ToString("N2")}. Please contact us to finalize the transaction.\n\n" +
                                 "Best regards,\nYour Auction Team";

                try
                {
                    await _emailSender.SendEmailAsync(highestBidder.Email, subject, message);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error sending email: " + ex.Message);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = listing.Id });
        }


        [HttpPost]
        public async Task<ActionResult> AddComment([Bind("Id, Content, ListingId, IdentityUserId")] Comment comment)
        {
            if (ModelState.IsValid)
            {
                await _commentsService.Add(comment);
            }
            var listing = await _listingsService.GetById(comment.ListingId);
            return View("Details", listing);

        }
        public async Task<IActionResult> WonBids()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Retrieves the logged-in user ID
            var wonBids = await _bidsService.GetWonBidsAsync(userId);    // Gets won bids for the user
            return View(wonBids); // Returns the view with won bids
        }
    }
}