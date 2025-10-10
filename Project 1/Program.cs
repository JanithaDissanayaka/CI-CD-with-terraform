using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project_1.Data.Services;
using Project_1.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Project_1.Hubs;
using Project_1.Services; // ✅ NEW: for BidExpirationService

var builder = WebApplication.CreateBuilder(args);

// ----------------- Database -----------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ----------------- Identity -----------------
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// ----------------- SignalR Service -----------------
builder.Services.AddSignalR(); // ✅ Enables SignalR real-time connections

// ----------------- Application Services -----------------
builder.Services.AddScoped<IListingsService, ListingsService>();
builder.Services.AddScoped<IBidsService, BidsService>();
builder.Services.AddScoped<ICommentsService, CommentsService>();

// ✅ Register the background service for automatic bid expiration
builder.Services.AddHostedService<BidExpirationService>();

// ----------------- Fake Email Sender (for testing) -----------------
builder.Services.AddSingleton<IEmailSender, NoEmailSender>();

// ----------------- MVC / Razor -----------------
builder.Services.AddControllersWithViews();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

// ----------------- Middleware -----------------
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ----------------- SignalR Hub Mapping -----------------
app.MapHub<BidHub>("/bidHub"); // ✅ SignalR endpoint for real-time bidding

// ----------------- Routes -----------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// ----------------- Run -----------------
app.Run();


// ----------------- Fake Email Sender Class -----------------
public class NoEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // 🚫 Skips sending real emails (used for local testing)
        return Task.CompletedTask;
    }
}

// ----------------- Hub Class -----------------
namespace Project_1.Hubs
{
    using Microsoft.AspNetCore.SignalR;
    using System.Threading.Tasks;

    public class BidHub : Hub
    {
        // 🔹 Called when a new highest bid is placed
        public async Task UpdateHighestBid(int listingId, string newHighestBidAmount)
        {
            await Clients.Group($"listing-{listingId}")
                .SendAsync("ReceiveNewHighestBid", newHighestBidAmount);
        }

        // 🔹 Called when a user views a specific listing
        public async Task JoinListingGroup(int listingId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"listing-{listingId}");
        }

        // 🔹 (Optional) Notify when auction ends
        public async Task NotifyAuctionEnded(int listingId, string winner, decimal finalPrice)
        {
            await Clients.Group($"listing-{listingId}")
                .SendAsync("AuctionEnded", new { listingId, winner, finalPrice });
        }
    }
}
